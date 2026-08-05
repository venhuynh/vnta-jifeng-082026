using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.PostgresTypes;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangKyPheDuyet.DangKyTangCa;

public sealed class DatabaseOvertimeRegistrationService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope)
    : IOvertimeRegistrationService
{
    private const int MaxSearchResultLimit = 2000;
    private const string DefaultApprovedBy = "Giám đốc sản xuất";
    private const string DefaultActor = "authenticated-user";

    public async Task<IReadOnlyList<OvertimeRegistrationListItemDto>> SearchAsync(
        OvertimeRegistrationFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureTablesAsync(cancellationToken);

        var normalizedTake = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);
        var normalizedSearchText = NormalizeOptional(filter.SearchText);
        var requestQuery = dbContext.AttendanceOvertimeRegistrationRequests.AsNoTracking();

        if (filter.WorkDate.HasValue)
        {
            requestQuery = requestQuery.Where(row => row.WorkDate == filter.WorkDate.Value);
        }

        if (filter.DayType.HasValue)
        {
            requestQuery = requestQuery.Where(row => row.DayType == filter.DayType.Value);
        }

        if (filter.Status.HasValue)
        {
            requestQuery = requestQuery.Where(row => row.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            requestQuery = requestQuery.Where(row =>
                EF.Functions.ILike(row.WorkshopName, searchPattern)
                || EF.Functions.ILike(row.RequestedBy, searchPattern)
                || EF.Functions.ILike(row.ApprovedBy, searchPattern)
                || EF.Functions.ILike(row.Note, searchPattern)
                || dbContext.AttendanceOvertimeRegistrationDetails.Any(detail =>
                    detail.RequestId == row.Id
                    && (EF.Functions.ILike(detail.EmployeeCode, searchPattern)
                        || EF.Functions.ILike(detail.EmployeeName, searchPattern)
                        || EF.Functions.ILike(detail.TeamName, searchPattern)
                        || EF.Functions.ILike(detail.PositionName, searchPattern))));
        }

        var requestRows = await requestQuery
            .OrderByDescending(row => row.WorkDate)
            .ThenBy(row => row.WorkshopName)
            .ThenByDescending(row => row.LastActionAtUtc)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);

        if (requestRows.Count == 0)
        {
            return [];
        }

        var requestIds = requestRows.Select(row => row.Id).ToArray();
        var detailRows = await dbContext.AttendanceOvertimeRegistrationDetails
            .AsNoTracking()
            .Where(detail => requestIds.Contains(detail.RequestId))
            .OrderBy(detail => detail.TeamName)
            .ThenBy(detail => detail.EmployeeCode)
            .ToListAsync(cancellationToken);

        var detailsByRequestId = detailRows
            .GroupBy(detail => detail.RequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AttendanceOvertimeRegistrationDetailRow>)group.ToArray());

        return requestRows
            .Select(row => MapToListItemDto(
                row,
                detailsByRequestId.GetValueOrDefault(row.Id) ?? []))
            .ToArray();
    }

    public async Task<OvertimeRegistrationDraftDto> CreateDraftAsync(
        CreateOvertimeRegistrationDraftRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureTablesAsync(cancellationToken);
        ValidateDayType(request.DayType);

        var workshopContext = await LoadWorkshopContextForActorAsync(actorContext, cancellationToken);
        var existingRequestExists = await dbContext.AttendanceOvertimeRegistrationRequests
            .AsNoTracking()
            .AnyAsync(
                row => row.WorkshopCode == workshopContext.WorkshopCode
                       && row.WorkDate == request.WorkDate,
                cancellationToken);

        if (existingRequestExists)
        {
            throw new InvalidOperationException(
                $"Xưởng {workshopContext.WorkshopName} đã có phiếu đăng ký tăng ca cho ngày {request.WorkDate:dd/MM/yyyy}.");
        }

        var employeeAssignments = workshopContext.Employees
            .Values
            .OrderBy(employee => employee.TeamName)
            .ThenBy(employee => employee.EmployeeCode)
            .Select(employee => MapToEmployeeAssignmentDto(
                employee,
                request.DayType,
                OvertimeEmployeeAssignmentType.None))
            .ToArray();

        return new OvertimeRegistrationDraftDto(
            Guid.NewGuid(),
            request.WorkDate,
            request.DayType,
            workshopContext.WorkshopCode,
            workshopContext.WorkshopName,
            workshopContext.RequestedBy,
            DefaultApprovedBy,
            OvertimeRegistrationStatus.Draft,
            string.Empty,
            employeeAssignments);
    }

    public async Task<OvertimeRegistrationListItemDto> SaveAsync(
        UpsertOvertimeRegistrationRequest request,
        bool submitAfterSave,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureTablesAsync(cancellationToken);
        ValidateDayType(request.DayType);

        var workshopContext = await LoadWorkshopContextForActorAsync(actorContext, cancellationToken);
        var requestId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var normalizedNote = NormalizeOptional(request.Note) ?? string.Empty;
        var requestedAssignmentsByEmployeeId = BuildRequestedAssignmentMap(request.EmployeeAssignments);
        var detailSnapshots = BuildDetailSnapshots(
            workshopContext,
            request.DayType,
            requestedAssignmentsByEmployeeId);
        ValidateDetailSnapshots(detailSnapshots, request.DayType);

        var duplicateRequestExists = await dbContext.AttendanceOvertimeRegistrationRequests
            .AnyAsync(
                row => row.WorkshopCode == workshopContext.WorkshopCode
                       && row.WorkDate == request.WorkDate
                       && row.Id != requestId,
                cancellationToken);

        if (duplicateRequestExists)
        {
            throw new InvalidOperationException(
                $"Xưởng {workshopContext.WorkshopName} đã có phiếu đăng ký tăng ca cho ngày {request.WorkDate:dd/MM/yyyy}.");
        }

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var targetStatus = submitAfterSave
            ? OvertimeRegistrationStatus.PendingApproval
            : OvertimeRegistrationStatus.Draft;
        var requestRow = await dbContext.AttendanceOvertimeRegistrationRequests
            .SingleOrDefaultAsync(row => row.Id == requestId, cancellationToken);

        var isNew = requestRow is null;
        var previousStatus = requestRow?.Status;

        if (requestRow is null)
        {
            requestRow = new AttendanceOvertimeRegistrationRequestRow
            {
                Id = requestId,
                WorkDate = request.WorkDate,
                DayType = request.DayType,
                WorkshopCode = workshopContext.WorkshopCode,
                WorkshopName = workshopContext.WorkshopName,
                RequestedByEmployeeId = workshopContext.RequestedByEmployeeId,
                RequestedBy = workshopContext.RequestedBy,
                ApprovedByEmployeeId = null,
                ApprovedBy = DefaultApprovedBy,
                Status = targetStatus,
                Note = normalizedNote,
                LastActionAtUtc = now,
                SubmittedAtUtc = submitAfterSave ? now : null,
                ApprovedAtUtc = null,
                CreatedAtUtc = now,
                CreatedBy = NormalizeActor(actorContext.Actor),
                UpdatedAtUtc = null,
                UpdatedBy = null
            };
            dbContext.AttendanceOvertimeRegistrationRequests.Add(requestRow);
        }
        else
        {
            EnsureRequestIsEditable(requestRow);

            if (!actorContext.CanManageWorkshopRegistrations
                && requestRow.RequestedByEmployeeId != actorContext.EmployeeId)
            {
                throw new InvalidOperationException("Báº¡n chá»‰ Ä‘Æ°á»£c chá»‰nh sá»­a phiáº¿u Ä‘Äƒng kÃ½ tÄƒng ca do chÃ­nh mÃ¬nh táº¡o.");
            }

            if (!string.Equals(requestRow.WorkshopCode, workshopContext.WorkshopCode, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Bạn chỉ được chỉnh sửa phiếu thuộc xưởng của mình.");
            }

            requestRow.WorkDate = request.WorkDate;
            requestRow.DayType = request.DayType;
            requestRow.WorkshopName = workshopContext.WorkshopName;
            requestRow.RequestedByEmployeeId = workshopContext.RequestedByEmployeeId;
            requestRow.RequestedBy = workshopContext.RequestedBy;
            requestRow.ApprovedBy = DefaultApprovedBy;
            requestRow.Status = targetStatus;
            requestRow.Note = normalizedNote;
            requestRow.LastActionAtUtc = now;
            requestRow.SubmittedAtUtc = submitAfterSave ? now : null;
            requestRow.ApprovedAtUtc = null;
            requestRow.ApprovedByEmployeeId = null;
            requestRow.UpdatedAtUtc = now;
            requestRow.UpdatedBy = NormalizeActor(actorContext.Actor);
        }

        var existingDetails = await dbContext.AttendanceOvertimeRegistrationDetails
            .Where(detail => detail.RequestId == requestId)
            .ToListAsync(cancellationToken);

        if (existingDetails.Count > 0)
        {
            dbContext.AttendanceOvertimeRegistrationDetails.RemoveRange(existingDetails);
        }

        var detailRows = detailSnapshots
            .Select(snapshot => new AttendanceOvertimeRegistrationDetailRow
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                EmployeeId = snapshot.EmployeeId,
                EmployeeCode = snapshot.EmployeeCode,
                EmployeeName = snapshot.EmployeeName,
                PositionName = snapshot.PositionName,
                TeamCode = snapshot.TeamCode,
                TeamName = snapshot.TeamName,
                AssignmentType = snapshot.AssignmentType,
                CreatedAtUtc = now,
                UpdatedAtUtc = null
            })
            .ToArray();

        dbContext.AttendanceOvertimeRegistrationDetails.AddRange(detailRows);
        dbContext.AttendanceOvertimeRegistrationHistories.Add(
            BuildHistoryRow(
                requestId,
                previousStatus,
                targetStatus,
                isNew
                    ? (submitAfterSave ? "create-and-submit" : "create-draft")
                    : (submitAfterSave ? "save-and-submit" : "save-draft"),
                actorContext,
                now,
                null));

        RefineAuditActionIfActive(ResolveSaveAuditAction(isNew, requestRow.Status));
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToListItemDto(requestRow, detailRows);
    }

    public async Task ChangeStatusAsync(
        ChangeOvertimeRegistrationStatusRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureTablesAsync(cancellationToken);

        var normalizedIds = request.Ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return;
        }

        if (!actorContext.CanManageWorkshopRegistrations)
        {
            throw new InvalidOperationException("Báº¡n khÃ´ng cÃ³ quyá»n phÃª duyá»‡t hoáº·c Ä‘á»•i tráº¡ng thÃ¡i phiáº¿u Ä‘Äƒng kÃ½ tÄƒng ca.");
        }

        var requestRows = await dbContext.AttendanceOvertimeRegistrationRequests
            .Where(row => normalizedIds.Contains(row.Id))
            .ToListAsync(cancellationToken);

        if (requestRows.Count != normalizedIds.Length)
        {
            throw new InvalidOperationException("Một hoặc nhiều phiếu đăng ký tăng ca không còn tồn tại.");
        }

        var now = ToDatabaseTimestamp(DateTime.UtcNow);

        foreach (var requestRow in requestRows)
        {
            ValidateStatusTransition(requestRow.Status, request.TargetStatus);
            var previousStatus = requestRow.Status;

            requestRow.Status = request.TargetStatus;
            requestRow.LastActionAtUtc = now;
            requestRow.UpdatedAtUtc = now;
            requestRow.UpdatedBy = NormalizeActor(actorContext.Actor);

            switch (request.TargetStatus)
            {
                case OvertimeRegistrationStatus.PendingApproval:
                    requestRow.SubmittedAtUtc = now;
                    requestRow.ApprovedAtUtc = null;
                    requestRow.ApprovedByEmployeeId = null;
                    break;

                case OvertimeRegistrationStatus.Approved:
                    requestRow.ApprovedAtUtc = now;
                    requestRow.ApprovedByEmployeeId = actorContext.EmployeeId;
                    requestRow.ApprovedBy = DefaultApprovedBy;
                    break;

                case OvertimeRegistrationStatus.Returned:
                case OvertimeRegistrationStatus.Rejected:
                    requestRow.ApprovedAtUtc = null;
                    requestRow.ApprovedByEmployeeId = null;
                    break;
            }

            dbContext.AttendanceOvertimeRegistrationHistories.Add(
                BuildHistoryRow(
                    requestRow.Id,
                    previousStatus,
                    request.TargetStatus,
                    ResolveStatusActionName(request.TargetStatus),
                    actorContext,
                    now,
                    null));
        }

        if (request.TargetStatus == OvertimeRegistrationStatus.Approved)
        {
            await ApplyApprovedRequestsToAttendanceWorkdaySummariesAsync(
                requestRows,
                now,
                cancellationToken);
        }

        RefineAuditActionIfActive(ResolveStatusAuditAction(request.TargetStatus));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyApprovedRequestsToAttendanceWorkdaySummariesAsync(
        IReadOnlyList<AttendanceOvertimeRegistrationRequestRow> requestRows,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var requestIds = requestRows
            .Select(row => row.Id)
            .Distinct()
            .ToArray();

        var requestById = requestRows.ToDictionary(row => row.Id);
        var detailRows = await dbContext.AttendanceOvertimeRegistrationDetails
            .AsNoTracking()
            .Where(detail =>
                requestIds.Contains(detail.RequestId)
                && detail.AssignmentType != OvertimeEmployeeAssignmentType.None)
            .ToListAsync(cancellationToken);

        if (detailRows.Count == 0)
        {
            return;
        }

        var employeeIds = detailRows
            .Select(detail => detail.EmployeeId)
            .Distinct()
            .ToArray();
        var workDates = requestRows
            .Select(row => row.WorkDate)
            .Distinct()
            .ToArray();

        var summaryRows = await dbContext.AttendanceWorkdaySummaries
            .Where(summary =>
                employeeIds.Contains(summary.EmployeeId)
                && workDates.Contains(summary.WorkDate))
            .ToListAsync(cancellationToken);

        if (summaryRows.Count == 0)
        {
            return;
        }

        var approvedAssignmentsByKey = detailRows.ToDictionary(
            detail => (
                detail.EmployeeId,
                requestById[detail.RequestId].WorkDate),
            detail => (
                requestById[detail.RequestId].DayType,
                detail.AssignmentType));

        foreach (var summaryRow in summaryRows)
        {
            if (!approvedAssignmentsByKey.TryGetValue(
                    (summaryRow.EmployeeId, summaryRow.WorkDate),
                    out var approvedAssignment))
            {
                continue;
            }

            ApplyApprovedRegistrationToSummary(
                summaryRow,
                approvedAssignment.DayType,
                approvedAssignment.AssignmentType);
            summaryRow.UpdatedAtUtc = updatedAtUtc;
        }
    }

    private static void ApplyApprovedRegistrationToSummary(
        AttendanceWorkdaySummaryRow row,
        AttendanceWorkCalendarDayType dayType,
        OvertimeEmployeeAssignmentType assignmentType)
    {
        var isRegistered = assignmentType != OvertimeEmployeeAssignmentType.None;
        var overtimeMinutes = GetApprovedOvertimeMinutes(dayType, assignmentType);

        row.IsRegisterForOT = isRegistered;
        row.OvertimeMinutes = isRegistered ? overtimeMinutes : 0;
        row.OvertimeMinutes15 = 0;
        row.OvertimeMinutes20 = 0;
        row.OvertimeMinutes30 = 0;
        row.CheckInForOT15 = null;

        if (!isRegistered || overtimeMinutes <= 0)
        {
            return;
        }

        switch (dayType)
        {
            case AttendanceWorkCalendarDayType.DayOff:
                row.OvertimeMinutes20 = overtimeMinutes;
                break;

            case AttendanceWorkCalendarDayType.Holiday:
                row.OvertimeMinutes30 = overtimeMinutes;
                break;

            default:
                row.OvertimeMinutes15 = overtimeMinutes;
                break;
        }
    }

    private async Task<WorkshopContext> LoadWorkshopContextForActorAsync(
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (actorContext.EmployeeId is null || actorContext.EmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Tài khoản hiện tại chưa được liên kết với nhân viên để xác định xưởng đăng ký tăng ca.");
        }

        var actorContextRow = await (
                from employee in dbContext.Employees.AsNoTracking()
                where employee.Id == actorContext.EmployeeId.Value && !employee.IsDeleted
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                select new { employee, department })
            .SingleOrDefaultAsync(cancellationToken);

        if (actorContextRow?.department is null)
        {
            throw new InvalidOperationException("Không thể xác định xưởng của tài khoản hiện tại.");
        }

        var workshopName = NormalizeOptional(actorContextRow.department.DepartmentOrWorkshopName);
        if (string.IsNullOrWhiteSpace(workshopName))
        {
            throw new InvalidOperationException("Nhân viên hiện tại chưa có thông tin xưởng để lập phiếu tăng ca.");
        }

        var requestedBy = BuildEmployeeDisplayName(
            actorContextRow.employee.LastName,
            actorContextRow.employee.FirstName,
            actorContext.Actor);
        var workshopCode = BuildWorkshopCode(
            actorContextRow.department.CenterName,
            workshopName);

        var workshopEmployees = await (
                from employee in dbContext.Employees.AsNoTracking()
                where !employee.IsDeleted
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                where department != null
                      && department.CenterName == actorContextRow.department.CenterName
                      && department.DepartmentOrWorkshopName == actorContextRow.department.DepartmentOrWorkshopName
                orderby department.TeamName, department.GroupName, employee.EmployeeCode
                select new WorkshopEmployeeSnapshot(
                    employee.Id,
                    employee.EmployeeCode,
                    BuildEmployeeDisplayName(employee.LastName, employee.FirstName, employee.EmployeeCode),
                    NormalizeOptional(position == null ? null : position.Name) ?? "Nhân viên",
                    BuildTeamCode(department.Code, department.TeamName, department.GroupName, department.DepartmentOrWorkshopName),
                    BuildTeamName(department.TeamName, department.GroupName, department.DepartmentOrWorkshopName)))
            .ToListAsync(cancellationToken);

        if (workshopEmployees.Count == 0)
        {
            throw new InvalidOperationException($"Xưởng {workshopName} chưa có danh sách nhân viên để lập phiếu tăng ca.");
        }

        return new WorkshopContext(
            workshopCode,
            workshopName,
            actorContext.EmployeeId,
            requestedBy,
            workshopEmployees.ToDictionary(employee => employee.EmployeeId));
    }

    private static Dictionary<Guid, OvertimeEmployeeAssignmentType> BuildRequestedAssignmentMap(
        IReadOnlyList<UpsertOvertimeRegistrationEmployeeAssignmentRequest> employeeAssignments)
    {
        var result = new Dictionary<Guid, OvertimeEmployeeAssignmentType>();

        foreach (var employeeAssignment in employeeAssignments)
        {
            if (employeeAssignment.EmployeeId == Guid.Empty)
            {
                continue;
            }

            if (!result.TryAdd(employeeAssignment.EmployeeId, employeeAssignment.AssignmentType))
            {
                throw new InvalidOperationException("Danh sách nhân viên đăng ký tăng ca đang có dòng trùng lặp.");
            }
        }

        return result;
    }

    private static IReadOnlyList<DetailSnapshot> BuildDetailSnapshots(
        WorkshopContext workshopContext,
        AttendanceWorkCalendarDayType dayType,
        IReadOnlyDictionary<Guid, OvertimeEmployeeAssignmentType> requestedAssignmentsByEmployeeId)
    {
        var invalidEmployeeIds = requestedAssignmentsByEmployeeId.Keys
            .Where(employeeId => !workshopContext.Employees.ContainsKey(employeeId))
            .ToArray();

        if (invalidEmployeeIds.Length > 0)
        {
            throw new InvalidOperationException("Phiếu tăng ca đang chứa nhân viên nằm ngoài xưởng hiện tại.");
        }

        return workshopContext.Employees
            .Values
            .OrderBy(employee => employee.TeamName)
            .ThenBy(employee => employee.EmployeeCode)
            .Select(employee =>
            {
                var assignmentType = requestedAssignmentsByEmployeeId.TryGetValue(employee.EmployeeId, out var requestedAssignmentType)
                    ? NormalizeAssignmentTypeForDayType(requestedAssignmentType, dayType)
                    : OvertimeEmployeeAssignmentType.None;

                return new DetailSnapshot(
                    employee.EmployeeId,
                    employee.EmployeeCode,
                    employee.EmployeeName,
                    employee.PositionName,
                    employee.TeamCode,
                    employee.TeamName,
                    assignmentType);
            })
            .ToArray();
    }

    private static void ValidateDetailSnapshots(
        IReadOnlyList<DetailSnapshot> detailSnapshots,
        AttendanceWorkCalendarDayType dayType)
    {
        if (detailSnapshots.Count == 0)
        {
            throw new InvalidOperationException("Phiếu đăng ký tăng ca chưa có danh sách nhân viên trong xưởng.");
        }

        if (detailSnapshots.All(detail => detail.AssignmentType == OvertimeEmployeeAssignmentType.None))
        {
            throw new InvalidOperationException("Hãy chọn ít nhất một nhân viên tham gia tăng ca.");
        }

        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType))
        {
            if (detailSnapshots.Any(detail =>
                    detail.AssignmentType is OvertimeEmployeeAssignmentType.Until1900 or OvertimeEmployeeAssignmentType.Until2100))
            {
                throw new InvalidOperationException("Ngày nghỉ/ngày lễ chỉ cho phép chọn tham gia hoặc không tham gia tăng ca.");
            }

            return;
        }

        if (detailSnapshots.Any(detail => detail.AssignmentType == OvertimeEmployeeAssignmentType.SpecialDayRegistered))
        {
            throw new InvalidOperationException("Ngày thường chỉ cho phép chọn mức tăng ca đến 19:00 hoặc 21:00.");
        }
    }

    private static void EnsureRequestIsEditable(AttendanceOvertimeRegistrationRequestRow requestRow)
    {
        if (requestRow.Status is OvertimeRegistrationStatus.Draft or OvertimeRegistrationStatus.Returned)
        {
            return;
        }

        throw new InvalidOperationException("Chỉ phiếu nháp hoặc trả lại chỉnh sửa mới được cập nhật.");
    }

    private static void ValidateStatusTransition(
        OvertimeRegistrationStatus currentStatus,
        OvertimeRegistrationStatus targetStatus)
    {
        var isValid = targetStatus switch
        {
            OvertimeRegistrationStatus.PendingApproval => currentStatus is OvertimeRegistrationStatus.Draft or OvertimeRegistrationStatus.Returned,
            OvertimeRegistrationStatus.Approved => currentStatus == OvertimeRegistrationStatus.PendingApproval,
            OvertimeRegistrationStatus.Returned => currentStatus == OvertimeRegistrationStatus.PendingApproval,
            OvertimeRegistrationStatus.Rejected => currentStatus == OvertimeRegistrationStatus.PendingApproval,
            _ => false
        };

        if (!isValid)
        {
            throw new InvalidOperationException("Trạng thái phiếu đăng ký tăng ca không hợp lệ cho thao tác này.");
        }
    }

    private static void ValidateDayType(AttendanceWorkCalendarDayType dayType)
    {
        if (AttendanceWorkCalendarDayTypes.All.Contains(dayType))
        {
            return;
        }

        throw new InvalidOperationException("Loại ngày của phiếu đăng ký tăng ca không hợp lệ.");
    }

    private static OvertimeRegistrationListItemDto MapToListItemDto(
        AttendanceOvertimeRegistrationRequestRow requestRow,
        IReadOnlyList<AttendanceOvertimeRegistrationDetailRow> detailRows) =>
        new(
            requestRow.Id,
            requestRow.WorkDate,
            requestRow.DayType,
            requestRow.WorkshopCode,
            requestRow.WorkshopName,
            requestRow.RequestedBy,
            requestRow.ApprovedBy,
            requestRow.Status,
            requestRow.Note,
            requestRow.LastActionAtUtc,
            requestRow.SubmittedAtUtc,
            requestRow.ApprovedAtUtc,
            detailRows
                .Select(detail => MapToEmployeeAssignmentDto(detail, requestRow.DayType))
                .ToArray());

    private static OvertimeRegistrationEmployeeAssignmentDto MapToEmployeeAssignmentDto(
        AttendanceOvertimeRegistrationDetailRow detailRow,
        AttendanceWorkCalendarDayType dayType) =>
        new(
            detailRow.EmployeeId,
            detailRow.EmployeeCode,
            detailRow.EmployeeName,
            detailRow.PositionName,
            detailRow.TeamCode,
            detailRow.TeamName,
            detailRow.AssignmentType,
            BuildRegistrationHint(dayType, detailRow.AssignmentType));

    private static OvertimeRegistrationEmployeeAssignmentDto MapToEmployeeAssignmentDto(
        WorkshopEmployeeSnapshot employee,
        AttendanceWorkCalendarDayType dayType,
        OvertimeEmployeeAssignmentType assignmentType) =>
        new(
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.EmployeeName,
            employee.PositionName,
            employee.TeamCode,
            employee.TeamName,
            assignmentType,
            BuildRegistrationHint(dayType, assignmentType));

    private static AttendanceOvertimeRegistrationHistoryRow BuildHistoryRow(
        Guid requestId,
        OvertimeRegistrationStatus? fromStatus,
        OvertimeRegistrationStatus toStatus,
        string actionName,
        OvertimeRegistrationActorContext actorContext,
        DateTime performedAtUtc,
        string? note) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActionName = actionName,
            Note = NormalizeOptional(note),
            PerformedByEmployeeId = actorContext.EmployeeId,
            PerformedBy = NormalizeActor(actorContext.Actor),
            PerformedAtUtc = performedAtUtc
        };

    private static string ResolveStatusActionName(OvertimeRegistrationStatus status) => status switch
    {
        OvertimeRegistrationStatus.PendingApproval => "submit",
        OvertimeRegistrationStatus.Approved => "approve",
        OvertimeRegistrationStatus.Returned => "return",
        OvertimeRegistrationStatus.Rejected => "reject",
        _ => "update-status"
    };

    private void RefineAuditActionIfActive(string action)
    {
        if (auditScope.Current is not null)
        {
            auditScope.RefineAction(action);
        }
    }

    private static string ResolveSaveAuditAction(
        bool isNew,
        OvertimeRegistrationStatus persistedStatus) =>
        persistedStatus == OvertimeRegistrationStatus.PendingApproval
            ? AuditActions.OvertimeRegistration.Submitted
            : isNew
                ? AuditActions.OvertimeRegistration.DraftCreated
                : AuditActions.OvertimeRegistration.Updated;

    private static string ResolveStatusAuditAction(OvertimeRegistrationStatus targetStatus) => targetStatus switch
    {
        OvertimeRegistrationStatus.PendingApproval => AuditActions.OvertimeRegistration.Submitted,
        OvertimeRegistrationStatus.Approved => AuditActions.OvertimeRegistration.Approved,
        OvertimeRegistrationStatus.Returned => AuditActions.OvertimeRegistration.Returned,
        OvertimeRegistrationStatus.Rejected => AuditActions.OvertimeRegistration.Rejected,
        _ => throw new InvalidOperationException("The overtime registration target status is not auditable.")
    };

    private static OvertimeEmployeeAssignmentType NormalizeAssignmentTypeForDayType(
        OvertimeEmployeeAssignmentType assignmentType,
        AttendanceWorkCalendarDayType dayType)
    {
        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType))
        {
            return assignmentType == OvertimeEmployeeAssignmentType.None
                ? OvertimeEmployeeAssignmentType.None
                : OvertimeEmployeeAssignmentType.SpecialDayRegistered;
        }

        return assignmentType switch
        {
            OvertimeEmployeeAssignmentType.Until1900 => OvertimeEmployeeAssignmentType.Until1900,
            OvertimeEmployeeAssignmentType.Until2100 => OvertimeEmployeeAssignmentType.Until2100,
            _ => OvertimeEmployeeAssignmentType.None
        };
    }

    private static int GetApprovedOvertimeMinutes(
        AttendanceWorkCalendarDayType dayType,
        OvertimeEmployeeAssignmentType assignmentType)
    {
        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType))
        {
            return 0;
        }

        return assignmentType switch
        {
            OvertimeEmployeeAssignmentType.Until1900 => 120,
            OvertimeEmployeeAssignmentType.Until2100 => 240,
            _ => 0
        };
    }

    private static string BuildRegistrationHint(
        AttendanceWorkCalendarDayType dayType,
        OvertimeEmployeeAssignmentType assignmentType)
    {
        if (AttendanceWorkCalendarDayTypes.IsSpecialDay(dayType))
        {
            return assignmentType == OvertimeEmployeeAssignmentType.None
                ? "Không tham gia"
                : "Có tham gia";
        }

        return assignmentType switch
        {
            OvertimeEmployeeAssignmentType.Until1900 => "Đăng ký đến 19:00",
            OvertimeEmployeeAssignmentType.Until2100 => "Đăng ký đến 21:00",
            _ => "Không đăng ký"
        };
    }

    private static string BuildEmployeeDisplayName(
        string? lastName,
        string? firstName,
        string fallback)
    {
        var parts = new[]
        {
            NormalizeOptional(lastName),
            NormalizeOptional(firstName)
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        var value = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string BuildWorkshopCode(
        string? centerName,
        string? workshopName) =>
        string.Join(
            "::",
            new[]
            {
                NormalizeKeyPart(centerName),
                NormalizeKeyPart(workshopName)
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string BuildTeamCode(
        string? departmentCode,
        string? teamName,
        string? groupName,
        string? workshopName)
    {
        var resolvedTeamName = BuildTeamName(teamName, groupName, workshopName);
        return string.Join(
            "::",
            new[]
            {
                NormalizeKeyPart(departmentCode),
                NormalizeKeyPart(resolvedTeamName)
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildTeamName(
        string? teamName,
        string? groupName,
        string? workshopName) =>
        NormalizeOptional(groupName)
        ?? NormalizeOptional(teamName)
        ?? NormalizeOptional(workshopName)
        ?? "Tổ chưa xác định";

    private static string NormalizeActor(string? actor) =>
        NormalizeOptional(actor) ?? DefaultActor;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeKeyPart(string? value) =>
        NormalizeOptional(value)?
            .Replace('\t', ' ')
            .Replace("  ", " ", StringComparison.Ordinal)
            .ToUpperInvariant();

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private async Task EnsureTablesAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.attendance_overtime_registration_requests
            (
                "Id" uuid NOT NULL,
                "WorkDate" date NOT NULL,
                "DayType" smallint NOT NULL,
                "WorkshopCode" character varying(128) NOT NULL,
                "WorkshopName" character varying(255) NOT NULL,
                "RequestedByEmployeeId" uuid NULL,
                "RequestedBy" character varying(255) NOT NULL,
                "ApprovedByEmployeeId" uuid NULL,
                "ApprovedBy" character varying(255) NOT NULL,
                "Status" smallint NOT NULL,
                "Note" text NOT NULL DEFAULT '',
                "LastActionAtUtc" timestamp without time zone NOT NULL,
                "SubmittedAtUtc" timestamp without time zone NULL,
                "ApprovedAtUtc" timestamp without time zone NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "CreatedBy" character varying(128) NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                "UpdatedBy" character varying(128) NULL,
                CONSTRAINT "PK_attendance_overtime_registration_requests" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_attendance_overtime_registration_requests_requested_by"
                    FOREIGN KEY ("RequestedByEmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT,
                CONSTRAINT "FK_attendance_overtime_registration_requests_approved_by"
                    FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_attendance_overtime_registration_requests_WorkshopCode_WorkDate"
                ON public.attendance_overtime_registration_requests ("WorkshopCode", "WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_requests_WorkDate"
                ON public.attendance_overtime_registration_requests ("WorkDate");

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_requests_Status"
                ON public.attendance_overtime_registration_requests ("Status");
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.attendance_overtime_registration_details
            (
                "Id" uuid NOT NULL,
                "RequestId" uuid NOT NULL,
                "EmployeeId" uuid NOT NULL,
                "EmployeeCode" character varying(64) NOT NULL,
                "EmployeeName" character varying(255) NOT NULL,
                "PositionName" character varying(255) NOT NULL,
                "TeamCode" character varying(128) NOT NULL,
                "TeamName" character varying(255) NOT NULL,
                "AssignmentType" smallint NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_attendance_overtime_registration_details" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_attendance_overtime_registration_details_requests"
                    FOREIGN KEY ("RequestId") REFERENCES public.attendance_overtime_registration_requests ("Id")
                    ON DELETE CASCADE,
                CONSTRAINT "FK_attendance_overtime_registration_details_employees"
                    FOREIGN KEY ("EmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_details_RequestId"
                ON public.attendance_overtime_registration_details ("RequestId");

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_details_EmployeeId"
                ON public.attendance_overtime_registration_details ("EmployeeId");

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_attendance_overtime_registration_details_RequestId_EmployeeId"
                ON public.attendance_overtime_registration_details ("RequestId", "EmployeeId");
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS public.attendance_overtime_registration_histories
            (
                "Id" uuid NOT NULL,
                "RequestId" uuid NOT NULL,
                "FromStatus" smallint NULL,
                "ToStatus" smallint NOT NULL,
                "ActionName" character varying(64) NOT NULL,
                "Note" text NULL,
                "PerformedByEmployeeId" uuid NULL,
                "PerformedBy" character varying(128) NOT NULL,
                "PerformedAtUtc" timestamp without time zone NOT NULL,
                CONSTRAINT "PK_attendance_overtime_registration_histories" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_attendance_overtime_registration_histories_requests"
                    FOREIGN KEY ("RequestId") REFERENCES public.attendance_overtime_registration_requests ("Id")
                    ON DELETE CASCADE,
                CONSTRAINT "FK_attendance_overtime_registration_histories_employees"
                    FOREIGN KEY ("PerformedByEmployeeId") REFERENCES public.employees ("Id")
                    ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_histories_RequestId"
                ON public.attendance_overtime_registration_histories ("RequestId");

            CREATE INDEX IF NOT EXISTS "IX_attendance_overtime_registration_histories_PerformedAtUtc"
                ON public.attendance_overtime_registration_histories ("PerformedAtUtc");
            """,
            cancellationToken);
    }

    private sealed record WorkshopContext(
        string WorkshopCode,
        string WorkshopName,
        Guid? RequestedByEmployeeId,
        string RequestedBy,
        IReadOnlyDictionary<Guid, WorkshopEmployeeSnapshot> Employees);

    private sealed record WorkshopEmployeeSnapshot(
        Guid EmployeeId,
        string EmployeeCode,
        string EmployeeName,
        string PositionName,
        string TeamCode,
        string TeamName);

    private sealed record DetailSnapshot(
        Guid EmployeeId,
        string EmployeeCode,
        string EmployeeName,
        string PositionName,
        string TeamCode,
        string TeamName,
        OvertimeEmployeeAssignmentType AssignmentType);
}
