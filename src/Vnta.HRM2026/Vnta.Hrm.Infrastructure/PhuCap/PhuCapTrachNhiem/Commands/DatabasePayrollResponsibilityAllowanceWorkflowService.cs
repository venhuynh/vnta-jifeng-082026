using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem.Policies;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Điều phối workflow phụ cấp trách nhiệm trên cùng DbContext để các command ABC,
/// snapshot downstream và audit được lưu nhất quán trong một transaction.
/// </summary>
/// <summary>
/// Shared EF Core persistence operations for the responsibility-allowance feature.
/// This is deliberately not registered as an application service: focused read and
/// command services expose only their own capability contracts.
/// </summary>
public abstract partial class PayrollResponsibilityAllowancePersistenceOperations(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IBasicSalaryWorkdaySource basicSalaryWorkdaySource,
    ILogger logger)
{
    #region Hằng số và ngữ cảnh dùng chung

    /// <summary>Kỳ nhỏ nhất được module hỗ trợ; dữ liệu trước mốc này không hợp lệ.</summary>
    private static readonly ResponsibilityAllowancePeriod MinimumSupportedPeriod = new(2026, 6);
    private const int MaximumSupportedYear = 2100;
    private const int ResignedEmployeeStatus = 5;
    private const string PositionDefaultSourceKey = "position-default";
    private const string EmployeeAssignmentSourceKey = "employee-assignment";
    private const string SystemAuditUser = "system";
    private const string BatchLockAuditScopeMetadataKey = "auditScope";
    private static readonly ResponsibilityAllowanceSourceSelectionPolicy SourceSelectionPolicy = new();
    private static readonly ResponsibilityAllowanceAbcPolicy AbcPolicy = new();
    private static readonly ResponsibilityAllowanceAmountCalculator AmountCalculator = new();
    private static readonly ResponsibilityAllowanceWorkdayMetricsCalculator WorkdayMetricsCalculator = new();

    // Actor của request được ưu tiên; worker nền không có request mới dùng giá trị hệ thống.
    private string CurrentAuditUser => auditScope.Current?.Actor.DisplayName
        ?? auditScope.Current?.Actor.ActorId
        ?? SystemAuditUser;

    #endregion







    #region Tiện ích nội bộ cho workflow

    /// <summary>
    /// Dựng hoặc cập nhật snapshot ABC từ dữ liệu nguồn của một kỳ. Thứ tự ưu tiên
    /// là assignment theo nhân viên, sau đó mapping theo chức vụ; không có nguồn
    /// bậc hợp lệ thì không sinh/cập nhật dòng ABC. Theo mặc định chỉ làm mới
    /// snapshot nguồn; việc tính lại ABC và số tiền được bật tường minh cho luồng
    /// tính lại riêng. Hàm chỉ thay đổi tracker.
    /// </summary>
    private async Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshCoreAsync(
        int year,
        int month,
        Guid? employeeId,
        CancellationToken cancellationToken,
        bool recalculateAbc = false)
    {
        // Refresh chỉ xét nhân viên còn làm việc; nhân viên nghỉ việc không phát sinh snapshot mới.
        var activeEmployees = await LoadActiveEmployeesAsync(cancellationToken);
        if (employeeId.HasValue)
        {
            activeEmployees = activeEmployees
                .Where(employee => employee.Id == employeeId.Value)
                .ToList();
        }

        // Các dictionary giúp resolve nguồn theo nhân viên trong vòng lặp mà không truy vấn N+1.
        var grades = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(x => x.Year == year && x.Month == month)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var gradeSnapshots = grades.ToDictionary(
            pair => pair.Key,
            pair => new ResponsibilityAllowanceGradeSnapshot(
                pair.Value.Id,
                pair.Value.Code,
                pair.Value.Name,
                pair.Value.StandardResponsibilityAllowanceAmount,
                pair.Value.IsActive
                    ? ResponsibilityAllowanceConfigurationState.Active
                    : ResponsibilityAllowanceConfigurationState.Inactive));

        var mappings = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .AsNoTracking()
            .Where(x => x.Year == year && x.Month == month && x.IsActive)
            .ToDictionaryAsync(x => x.PositionId, cancellationToken);

        var assignments = await (
                from assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
                join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                    on assignment.PayrollAllowanceSummaryRecordId equals summary.Id
                where summary.PayrollYear == year && summary.PayrollMonth == month
                select new { summary.EmployeeId, Assignment = assignment })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Assignment, cancellationToken);

        var now = GetDatabaseNow();
        // ABC luôn gắn với allowance summary cùng nhân viên/cùng kỳ, nên tạo summary thiếu trước.
        await EnsureSummaryRowsAsync(
            year,
            month,
            activeEmployees.Select(x => x.Id).ToArray(),
            now,
            cancellationToken);

        var summariesQuery = dbContext.PayrollAllowanceSummaryRecords
            .Where(x => x.PayrollYear == year && x.PayrollMonth == month);
        if (employeeId.HasValue)
        {
            summariesQuery = summariesQuery.Where(x => x.EmployeeId == employeeId.Value);
        }

        var summariesByEmployeeId = await summariesQuery
            .ToDictionaryAsync(x => x.EmployeeId, cancellationToken);
        if (employeeId.HasValue && summariesByEmployeeId.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy dữ liệu tổng hợp phụ cấp của nhân viên ở kỳ đã chọn.");
        }

        var employeeIds = summariesByEmployeeId.Keys.ToArray();
        var employeesById = await LoadEmployeeSnapshotsByIdAsync(employeeIds, cancellationToken);
        var summaryIds = summariesByEmployeeId.Values.Select(x => x.Id).ToArray();
        var existingRows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(x => x.Year == year && x.Month == month && summaryIds.Contains(x.PayrollAllowanceSummaryRecordId))
            .ToDictionaryAsync(x => x.PayrollAllowanceSummaryRecordId, cancellationToken);

        var workdayAggregates = await LoadWorkdayAggregateAsync(year, month, employeeIds, cancellationToken);
        var standardWorkdays = await basicSalaryWorkdaySource.LoadStandardWorkingDaysAsync(
            year,
            month,
            employeeIds,
            cancellationToken);

        var inserted = 0;
        var updated = 0;
        var skippedLocked = 0;
        var skippedMissingSource = 0;
        var touchedRows = new List<PayrollResponsibilityAllowanceAbcRow>(summariesByEmployeeId.Count);

        foreach (var summary in summariesByEmployeeId.Values)
        {
            if (!employeesById.TryGetValue(summary.EmployeeId, out var employee))
            {
                throw new InvalidOperationException("Không tìm thấy nhân viên của dữ liệu tổng hợp phụ cấp.");
            }

            existingRows.TryGetValue(summary.Id, out var row);
            // Khóa ABC đóng băng cả dữ liệu nguồn snapshot; refresh chỉ ghi nhận để đồng bộ an toàn.
            if (row is not null && row.IsLocked)
            {
                skippedLocked++;
                touchedRows.Add(row);
                continue;
            }

            assignments.TryGetValue(employee.Id, out var assignment);
            PayrollResponsibilityAllowanceGradePositionRow? mapping = null;
            if (employee.PositionId.HasValue)
            {
                mappings.TryGetValue(employee.PositionId.Value, out mapping);
            }

            var sourceDecision = SourceSelectionPolicy.Select(
                new ResponsibilityAllowanceSourceSelectionInput(
                    assignment is null
                        ? null
                        : new ResponsibilityAllowanceAssignmentSnapshot(
                            assignment.GradeId,
                            assignment.IsAssignGradeFromPosition
                                ? ResponsibilityAllowanceAssignmentSource.PositionDefault
                                : ResponsibilityAllowanceAssignmentSource.EmployeeAssignment),
                    mapping is null
                        ? null
                        : new ResponsibilityAllowancePositionMappingSnapshot(
                            mapping.GradeId,
                            mapping.IsActive
                                ? ResponsibilityAllowanceConfigurationState.Active
                                : ResponsibilityAllowanceConfigurationState.Inactive),
                    gradeSnapshots));
            var selectedSource = new SelectedSourceSnapshot(
                sourceDecision.Source.ToStorageValue(),
                ResolveSourceLabel(assignment, sourceDecision),
                sourceDecision.Grade is null ? null : grades[sourceDecision.Grade.Id],
                sourceDecision.StandardAmount);
            // Không có bậc hợp lệ thì bỏ qua: không tạo dòng tiền 0 gây hiểu nhầm là được áp dụng.
            if (selectedSource.Grade is null)
            {
                skippedMissingSource++;
                continue;
            }

            // Lần đầu gặp summary này thì tạo snapshot; các lần sau cập nhật chính dòng đã có.
            if (row is null)
            {
                row = new PayrollResponsibilityAllowanceAbcRow
                {
                    Id = Guid.NewGuid(),
                    PayrollAllowanceSummaryRecordId = summary.Id,
                    EmployeeId = summary.EmployeeId,
                    Year = year,
                    Month = month,
                    CreatedAtUtc = now
                };
                dbContext.PayrollResponsibilityAllowanceAbcRows.Add(row);
                existingRows[summary.Id] = row;
                inserted++;
            }
            else
            {
                updated++;
            }

            row.EmployeeCode = employee.EmployeeCode;
            row.EmployeeName = employee.EmployeeName;
            row.DepartmentName = employee.DepartmentName;
            row.PositionId = employee.PositionId;
            row.PositionName = employee.PositionName;

            var actualWorkdays = workdayAggregates.TryGetValue(employee.Id, out var aggregate)
                ? aggregate.SalaryWorkdays
                : 0m;
            var standardDays = standardWorkdays.TryGetValue(employee.Id, out var salaryDays)
                ? salaryDays
                : 0m;
            row.GradeId = selectedSource.Grade?.Id;
            row.GradeCode = selectedSource.Grade?.Code;
            row.GradeName = selectedSource.Grade?.Name ?? string.Empty;
            row.ActualWorkDays = actualWorkdays;
            row.StandardWorkDays = standardDays;
            row.StandardResponsibilityAllowanceAmount = selectedSource.StandardResponsibilityAllowanceAmount;
            // Bảo vệ dữ liệu cũ: THS âm từng lưu trước đây được quy về 0 khi refresh.
            row.MonthlyPerformanceBonusAmount = row.MonthlyPerformanceBonusAmount < 0 ? 0m : row.MonthlyPerformanceBonusAmount;
            if (recalculateAbc)
            {
                row.AbcRating = ComputeAbcRating(
                    standardDays,
                    actualWorkdays,
                    aggregate?.HasUnexcusedAbsence ?? false);
                row.ActualResponsibilityAllowanceAmount = CalculateActualResponsibilityAllowanceAmount(
                    row.StandardResponsibilityAllowanceAmount,
                    row.StandardWorkDays,
                    row.ActualWorkDays,
                    row.AbcRating,
                    row.MonthlyPerformanceBonusAmount,
                    row.IsPerformanceBonusExcluded);
                row.CalculatedAtUtc = now;
                row.CalculatedBy = CurrentAuditUser;
            }
            row.UpdatedAtUtc = now;
            row.UpdatedBy = CurrentAuditUser;

            touchedRows.Add(row);
        }

        await ApplyDownstreamSnapshotsAsync(touchedRows, now, cancellationToken);

        return new RefreshPayrollResponsibilityAllowanceAbcResult(
            year,
            month,
            summariesByEmployeeId.Count,
            inserted,
            updated,
            skippedLocked,
            skippedMissingSource);
    }

    /// <summary>
    /// Thực thi optimistic concurrency cho một dòng. Client phải gửi đúng mốc
    /// cập nhật, nếu không thao tác bị từ chối để tránh ghi đè thay đổi mới hơn.
    /// </summary>
    private static void EnsureConcurrency(
        PayrollResponsibilityAllowanceAbcRow row,
        DateTime? originalUpdatedAtUtc)
    {
        if (!originalUpdatedAtUtc.HasValue
            || (row.UpdatedAtUtc ?? row.CreatedAtUtc) != originalUpdatedAtUtc.Value)
        {
            throw new ResponsibilityAllowanceConflictException(
                "Dữ liệu phụ cấp trách nhiệm đã thay đổi. Vui lòng tải lại trước khi thao tác.");
        }
    }

    /// <summary>
    /// Kiểm tra token concurrency cho toàn bộ tập dòng của thao tác hàng loạt.
    /// Tập EmployeeId và số token phải khớp chính xác với tập dòng đích.
    /// </summary>
    private static void EnsureBatchConcurrency(
        IReadOnlyCollection<PayrollResponsibilityAllowanceAbcRow> rows,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens)
    {
        // Gộp theo nhân viên để token trùng không làm nới lỏng điều kiện kiểm tra version.
        var expectedByEmployeeId = concurrencyTokens?
            .Where(token => token.EmployeeId != Guid.Empty)
            .GroupBy(token => token.EmployeeId)
            .ToDictionary(group => group.Key, group => group.Last().OriginalUpdatedAtUtc);

        if (expectedByEmployeeId is null || expectedByEmployeeId.Count != rows.Count)
        {
            throw new ResponsibilityAllowanceConflictException(
                "Dữ liệu phụ cấp trách nhiệm đã thay đổi hoặc chưa đủ mốc kiểm tra. Vui lòng tải lại trước khi thao tác.");
        }

        foreach (var row in rows)
        {
            if (!expectedByEmployeeId.TryGetValue(row.EmployeeId, out var originalUpdatedAtUtc))
            {
                throw new ResponsibilityAllowanceConflictException(
                    "Dữ liệu phụ cấp trách nhiệm đã thay đổi. Vui lòng tải lại trước khi thao tác.");
            }

            EnsureConcurrency(row, originalUpdatedAtUtc);
        }
    }

    /// <summary>Lấy dòng ABC có thể sửa; chặn sớm dòng không tồn tại hoặc đã khóa.</summary>
    private async Task<PayrollResponsibilityAllowanceAbcRow> GetEditableAbcRowAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        // Ràng buộc unique bảo đảm mỗi nhân viên chỉ có một dòng ABC trong một kỳ.
        var row = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng trách nhiệm của nhân viên ở kỳ đã chọn.");

        if (row.IsLocked)
        {
            throw new InvalidOperationException("Dòng trách nhiệm đã bị khóa, không thể cập nhật.");
        }

        return row;
    }

    /// <summary>
    /// Lõi lưu assignment: xác thực kỳ/nhân viên/bậc, bảo vệ kỳ đã khóa và snapshot
    /// hóa mức tiền bậc hoặc mức nhập tay. Hàm chưa tự commit transaction.
    /// </summary>
    private async Task<PayrollResponsibilityAllowanceEmployeeAssignmentRow> SaveEmployeeAssignmentCoreAsync(
        SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(request.Year, request.Month);

        // Assignment chỉ tồn tại cho nhân viên đang thuộc tập Summary của kỳ. Tập này có
        // thể chứa nhân viên đã nghỉ việc, vì Summary là snapshot nghiệp vụ của kỳ lương.
        var employee = await ResolveEmployeeSnapshotAsync(request.EmployeeId, cancellationToken);
        var summary = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .Where(
                x => x.EmployeeId == employee.Id
                    && x.PayrollYear == request.Year
                    && x.PayrollMonth == request.Month)
            .Select(x => new { x.Id, x.EmployeeId, x.PayrollYear, x.PayrollMonth })
            .SingleOrDefaultAsync(cancellationToken);
        if (summary is null)
        {
            throw new InvalidOperationException("Nhân viên không thuộc Phụ cấp tổng hợp của kỳ đã chọn.");
        }
        await EnsureAssignmentPeriodUnlockedAsync(employee.Id, request.Year, request.Month, cancellationToken);

        if (!request.GradeId.HasValue || request.GradeId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Phải chọn bậc trách nhiệm để tạo hoặc cập nhật assignment.");
        }

        var grade = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.GradeId.Value && x.Year == request.Year && x.Month == request.Month && x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy bậc trách nhiệm đang áp dụng được chọn.");

        var now = GetDatabaseNow();
        PayrollResponsibilityAllowanceEmployeeAssignmentRow? row;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            row = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);

            // Dòng Summary chưa có assignment vẫn phải xuất hiện trên grid. Grid dùng
            // Summary.Id làm key tạm thời; khi lưu lần đầu, nhận diện key này để tạo
            // assignment theo khóa tự nhiên nhân viên/kỳ mà không nới lỏng stale check.
            if (row is null)
            {
                if (request.Id.Value != summary.Id)
                {
                    throw new InvalidOperationException("Không tìm thấy gán trách nhiệm cần cập nhật. Hãy tải lại dữ liệu kỳ hiện tại.");
                }

                row = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                    .SingleOrDefaultAsync(x => x.PayrollAllowanceSummaryRecordId == summary.Id, cancellationToken);
            }

            if (row is not null && row.PayrollAllowanceSummaryRecordId != summary.Id)
            {
                throw new InvalidOperationException("Không thể chuyển gán trách nhiệm đang sửa sang kỳ khác. Hãy đóng popup và tải lại dữ liệu.");
            }
        }
        else
        {
            row = await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments
                .SingleOrDefaultAsync(x => x.PayrollAllowanceSummaryRecordId == summary.Id, cancellationToken);
        }

        if (row is null)
        {
            row = new PayrollResponsibilityAllowanceEmployeeAssignmentRow
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = now
            };
            dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Add(row);
        }

        row.PayrollAllowanceSummaryRecordId = summary.Id;
        row.GradeId = grade.Id;
        row.IsAssignGradeFromPosition = false;
        row.Note = NormalizeOptional(request.Note);
        row.UpdatedAtUtc = now;

        return row;
    }

    /// <summary>Không cho đổi assignment nếu nhân viên đã có dòng ABC khóa trong kỳ.</summary>
    private async Task EnsureAssignmentPeriodUnlockedAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var isLocked = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .AsNoTracking()
            .AnyAsync(
                x => x.EmployeeId == employeeId
                    && x.Year == year
                    && x.Month == month
                    && x.IsLocked,
                cancellationToken);

        if (isLocked)
        {
            throw new InvalidOperationException("Dòng trách nhiệm đã bị khóa, không thể cập nhật gán trách nhiệm hoặc điều chỉnh.");
        }
    }

    /// <summary>Ghép assignment persistence với snapshot nhân viên và bậc để trả DTO đầy đủ.</summary>
    private async Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> BuildEmployeeAssignmentDtoAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentRow row,
        CancellationToken cancellationToken)
    {
        var summary = await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == row.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy Summary được liên kết với gán trách nhiệm.");
        var employee = await ResolveEmployeeSnapshotAsync(summary.EmployeeId, cancellationToken);
        var grade = row.GradeId.HasValue
            ? await dbContext.PayrollResponsibilityAllowanceGrades
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == row.GradeId.Value, cancellationToken)
            : null;

        return new PayrollResponsibilityAllowanceEmployeeAssignmentDto(
            row.Id,
            summary.PayrollYear,
            summary.PayrollMonth,
            summary.EmployeeId,
            employee.EmployeeCode,
            employee.EmployeeName,
            employee.PositionId,
            employee.PositionName,
            row.GradeId,
            grade?.Code,
            grade?.Name ?? string.Empty,
            grade?.StandardResponsibilityAllowanceAmount ?? 0m,
            row.IsAssignGradeFromPosition,
            row.IsAssignGradeFromPosition ? PositionDefaultSourceKey : EmployeeAssignmentSourceKey,
            row.Note,
            row.UpdatedAtUtc ?? row.CreatedAtUtc);
    }

    /// <summary>Đọc bậc đúng kỳ cho ngữ cảnh popup; ID null/rỗng trả về không có nguồn.</summary>
    private async Task<PayrollResponsibilityAllowanceContextGradeDto?> BuildContextGradeAsync(
        Guid gradeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(x => x.Id == gradeId)
            .Select(x => new PayrollResponsibilityAllowanceContextGradeDto(
                "payroll_monthly_responsibility_allowance_grades",
                x.Id,
                x.Year,
                x.Month,
                x.Code,
                x.Name,
                x.StandardResponsibilityAllowanceAmount,
                x.DisplayOrder,
                x.IsActive,
                x.Note))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>Chuyển snapshot ABC persistence thành ngữ cảnh chỉ đọc cho popup.</summary>
    private static PayrollResponsibilityAllowanceCurrentAbcRecordContextDto BuildCurrentAbcContext(PayrollResponsibilityAllowanceAbcRow row)
    {
        return new PayrollResponsibilityAllowanceCurrentAbcRecordContextDto(
            "payroll_monthly_responsibility_allowance_abc",
            row.Id,
            row.EmployeeId,
            row.EmployeeCode,
            row.EmployeeName,
            row.PositionId,
            row.PositionName,
            row.GradeId,
            row.GradeCode,
            row.GradeName,
            row.Year,
            row.Month,
            row.ActualWorkDays,
            row.StandardWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded,
            row.StandardResponsibilityAllowanceAmount,
            row.ActualResponsibilityAllowanceAmount,
            row.IsLocked,
            row.CalculatedAtUtc,
            row.CalculatedBy,
            row.UpdatedAtUtc,
            row.UpdatedBy,
            row.LockedAtUtc,
            row.LockedBy,
            row.Note);
    }

    /// <summary>So sánh nguồn/hạng/tiền hiện có với kết quả xem trước, không ghi dữ liệu.</summary>
    private PayrollResponsibilityAllowanceUpdateImpactDto BuildUpdateImpact(
        PayrollResponsibilityAllowanceAbcRow? currentAbc,
        decimal selectedStandardAmount,
        decimal previewActualAmount)
    {
        if (currentAbc is null)
        {
            return new PayrollResponsibilityAllowanceUpdateImpactDto(
                "payroll_monthly_responsibility_allowance_abc",
                WillInsert: true,
                WillUpdate: false,
                SkippedBecauseLocked: false,
                AmountWouldChange: previewActualAmount > 0,
                Message: "Hệ thống sẽ tạo mới dòng bảng phụ Trách nhiệm cho kỳ này khi bạn lưu điều chỉnh.");
        }

        var amountWouldChange = currentAbc.StandardResponsibilityAllowanceAmount != selectedStandardAmount
            || currentAbc.ActualResponsibilityAllowanceAmount != previewActualAmount;

        return new PayrollResponsibilityAllowanceUpdateImpactDto(
            "payroll_monthly_responsibility_allowance_abc",
            WillInsert: false,
            WillUpdate: !currentAbc.IsLocked,
            SkippedBecauseLocked: currentAbc.IsLocked,
            AmountWouldChange: amountWouldChange,
            Message: currentAbc.IsLocked
                ? "Dòng hiện tại đang khóa nên thao tác refresh sẽ bỏ qua cho đến khi mở khóa."
                : "Sau khi lưu điều chỉnh, hệ thống sẽ làm mới lại dòng Trách nhiệm tháng hiện tại từ nguồn đã chọn.");
    }

    /// <summary>Tạo biểu thức diễn giải đúng nhánh áp dụng THS, không áp dụng THS và hạng D theo công chuẩn của kỳ.</summary>
    private static string BuildCalculationFormula(
        decimal standardAmount,
        decimal standardWorkDays,
        decimal actualWorkDays,
        string abcRating,
        decimal performanceBonusAmount,
        bool isPerformanceBonusExcluded)
    {
        var actual = CalculateActualResponsibilityAllowanceAmount(
            standardAmount,
            standardWorkDays,
            actualWorkDays,
            abcRating,
            performanceBonusAmount,
            isPerformanceBonusExcluded);

        if (isPerformanceBonusExcluded)
        {
            var missingWorkDays = Math.Max(standardWorkDays - actualWorkDays, 0m);
            return missingWorkDays <= 1m
                ? $"{standardAmount:0.##} = {actual:0.##}"
                : $"{standardAmount:0.##} / {standardWorkDays:0.##} x {actualWorkDays:0.##} = {actual:0.##}";
        }

        if (string.Equals(abcRating, "D", StringComparison.OrdinalIgnoreCase))
        {
            return $"70% x {standardAmount:0.##} x {performanceBonusAmount:0.####} / {standardWorkDays:0.##} x {actualWorkDays:0.##} = {actual:0.##}";
        }

        return $"{standardAmount:0.##} x {GetAbcMultiplier(abcRating):0.##} x {performanceBonusAmount:0.####} = {actual:0.##}";
    }

    private static string ResolveSourceLabel(
        PayrollResponsibilityAllowanceEmployeeAssignmentRow? assignment,
        ResponsibilityAllowanceSourceSelectionResult decision)
    {
        if (assignment is null)
        {
            return decision.Source == ResponsibilityAllowanceSelectedSource.PositionDefault
                ? "Mặc định theo chức vụ"
                : "Chưa có nguồn áp dụng";
        }

        if (assignment.IsAssignGradeFromPosition)
        {
            return "Mặc định theo chức vụ";
        }

        return decision.Grade is null
            ? "Điều chỉnh theo nhân viên"
            : "Gán theo nhân viên";
    }

    /// <summary>Phiên bản chọn nguồn cho DTO giải thích, bổ sung nhãn nghiệp vụ cho UI.</summary>
    private static PayrollResponsibilityAllowanceSelectedSourceContextDto ResolveSelectedSource(
        PayrollResponsibilityAllowanceEmployeeAssignmentContextDto? assignment,
        PayrollResponsibilityAllowanceContextGradeDto? manualGrade,
        PayrollResponsibilityAllowancePositionGradeMappingContextDto? positionMapping,
        PayrollResponsibilityAllowanceContextGradeDto? positionDefaultGrade)
    {
        var grades = new[] { manualGrade, positionDefaultGrade }
            .Where(x => x is not null)
            .Cast<PayrollResponsibilityAllowanceContextGradeDto>()
            .DistinctBy(x => x.Id)
            .ToDictionary(
                x => x.Id,
                x => new ResponsibilityAllowanceGradeSnapshot(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.StandardResponsibilityAllowanceAmount,
                    x.IsActive
                        ? ResponsibilityAllowanceConfigurationState.Active
                        : ResponsibilityAllowanceConfigurationState.Inactive));
        var decision = SourceSelectionPolicy.Select(
            new ResponsibilityAllowanceSourceSelectionInput(
                assignment is null
                    ? null
                    : new ResponsibilityAllowanceAssignmentSnapshot(
                        assignment.GradeId,
                        string.Equals(assignment.AssignmentSource, PositionDefaultSourceKey, StringComparison.Ordinal)
                            ? ResponsibilityAllowanceAssignmentSource.PositionDefault
                            : ResponsibilityAllowanceAssignmentSource.EmployeeAssignment),
                positionMapping is null
                    ? null
                    : new ResponsibilityAllowancePositionMappingSnapshot(
                        positionMapping.GradeId,
                        positionMapping.IsActive
                            ? ResponsibilityAllowanceConfigurationState.Active
                            : ResponsibilityAllowanceConfigurationState.Inactive),
                grades));
        var selectedGrade = decision.Grade is null
            ? null
            : new[] { manualGrade, positionDefaultGrade }
                .Where(x => x is not null && x.Id == decision.Grade.Id)
                .Cast<PayrollResponsibilityAllowanceContextGradeDto>()
                .FirstOrDefault();
        // Giữ nguyên source key đã snapshot cho DTO; policy chỉ quyết định nhánh nghiệp vụ.
        var sourceKey = assignment?.AssignmentSource ?? decision.Source.ToStorageValue();
        var sourceLabel = assignment is null
            ? decision.Source == ResponsibilityAllowanceSelectedSource.PositionDefault
                ? "Mặc định theo chức vụ"
                : "Chưa có nguồn áp dụng"
            : decision.Source == ResponsibilityAllowanceSelectedSource.PositionDefault
                ? "Mặc định theo chức vụ"
                : decision.Grade is null ? "Điều chỉnh theo nhân viên" : "Gán theo nhân viên";
        var amount = assignment is not null && decision.Grade is not null
            ? assignment.StandardResponsibilityAllowanceAmount
            : decision.StandardAmount;

        return new PayrollResponsibilityAllowanceSelectedSourceContextDto(
            sourceKey,
            sourceLabel,
            selectedGrade?.Id,
            selectedGrade?.Code,
            selectedGrade?.Name ?? (assignment is not null && decision.Grade is null ? "Không hưởng" : string.Empty),
            amount);
    }

    /// <summary>
    /// Tổng hợp công ABC từ chấm công ngày thường: chỉ trạng thái đủ điều kiện mới
    /// là công hành chính; tổng phút muộn/sớm được quy đổi thành ngày trừ cuối kỳ.
    /// </summary>
    private async Task<Dictionary<Guid, WorkdayAggregateSnapshot>> LoadWorkdayAggregateAsync(
        int year,
        int month,
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        var monthStart = new DateOnly(year, month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        var rows = await (
                from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
                where employeeIds.Contains(summary.EmployeeId)
                      && summary.WorkDate >= monthStart
                      && summary.WorkDate < monthEndExclusive
                      && summary.DayType == AttendanceWorkCalendarDayTypes.Regular
                join status in dbContext.AttendanceStatusCodes.AsNoTracking()
                    on summary.CodeKetQuaTinhCongId equals status.Id into statusGroup
                from status in statusGroup.DefaultIfEmpty()
                select new
                {
                    summary.EmployeeId,
                    StatusCode = status == null ? null : status.Code,
                    IsProductivityAllowanceEligible = status != null && status.CongHanhChinh,
                    summary.LateMinutes,
                    summary.EarlyLeaveMinutes
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var metrics = WorkdayMetricsCalculator.Calculate(
                        new ResponsibilityAllowanceWorkdayMetricsInput(
                            group.Select(x => new ResponsibilityAllowanceWorkdayInput(
                                x.IsProductivityAllowanceEligible
                                    ? ResponsibilityAllowanceWorkdayEligibility.Eligible
                                    : ResponsibilityAllowanceWorkdayEligibility.NotEligible,
                                x.LateMinutes,
                                x.EarlyLeaveMinutes,
                                new ResponsibilityAllowanceAttendanceCode(x.StatusCode))
                            ).ToArray()));

                    return new WorkdayAggregateSnapshot(
                        metrics.AdministrativeWorkdays,
                        metrics.LateEarlyDeductionDays,
                        metrics.AbcWorkdays,
                        metrics.UnexcusedAbsenceState == ResponsibilityAllowanceUnexcusedAbsenceState.Present,
                        metrics.EligibleAttendanceCodes.Select(x => x.Value).ToArray());
                });
    }

    /// <summary>Đọc snapshot nhân viên còn làm việc để refresh toàn kỳ.</summary>
    private async Task<List<EmployeeSnapshot>> LoadActiveEmployeesAsync(CancellationToken cancellationToken)
    {
        return await (
                from employee in dbContext.Employees.AsNoTracking()
                where !employee.IsDeleted && employee.Status != ResignedEmployeeStatus
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                orderby employee.EmployeeCode, employee.LastName, employee.FirstName
                select new EmployeeSnapshot(
                    employee.Id,
                    employee.EmployeeCode,
                    BuildEmployeeName(employee.LastName, employee.FirstName),
                    department == null ? null : BuildDepartmentName(department),
                    position == null ? (Guid?)null : position.Id,
                    position == null ? string.Empty : position.Code,
                    position == null ? string.Empty : position.Name))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Đọc snapshot nhân viên theo tập ID để điền thông tin hiển thị cho ABC.</summary>
    private async Task<Dictionary<Guid, EmployeeSnapshot>> LoadEmployeeSnapshotsByIdAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await (
                from employee in dbContext.Employees.AsNoTracking()
                where employeeIds.Contains(employee.Id)
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                select new EmployeeSnapshot(
                    employee.Id,
                    employee.EmployeeCode,
                    BuildEmployeeName(employee.LastName, employee.FirstName),
                    department == null ? null : BuildDepartmentName(department),
                    position == null ? (Guid?)null : position.Id,
                    position == null ? string.Empty : position.Code,
                    position == null ? string.Empty : position.Name))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    /// <summary>Tra cứu nhân viên còn làm việc; nhân viên nghỉ/đã xóa không được gán phụ cấp.</summary>
    private async Task<EmployeeSnapshot> ResolveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await (
                from employee in dbContext.Employees.AsNoTracking()
                where employee.Id == employeeId && !employee.IsDeleted && employee.Status != ResignedEmployeeStatus
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                select new EmployeeSnapshot(
                    employee.Id,
                    employee.EmployeeCode,
                    BuildEmployeeName(employee.LastName, employee.FirstName),
                    department == null ? null : BuildDepartmentName(department),
                    position == null ? (Guid?)null : position.Id,
                    position == null ? string.Empty : position.Code,
                    position == null ? string.Empty : position.Name))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên đang làm việc.");
    }

    /// <summary>Đọc snapshot nhân viên theo ID mà không áp điều kiện trạng thái làm việc.</summary>
    private async Task<EmployeeSnapshot> ResolveEmployeeSnapshotAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await (
                from employee in dbContext.Employees.AsNoTracking()
                where employee.Id == employeeId
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                select new EmployeeSnapshot(
                    employee.Id,
                    employee.EmployeeCode,
                    BuildEmployeeName(employee.LastName, employee.FirstName),
                    department == null ? null : BuildDepartmentName(department),
                    position == null ? (Guid?)null : position.Id,
                    position == null ? string.Empty : position.Code,
                    position == null ? string.Empty : position.Name))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên của Phụ cấp tổng hợp.");
    }

    /// <summary>Bảo đảm mỗi nhân viên có summary kỳ lương, tạo summary tiền 0 nếu còn thiếu.</summary>
    private async Task<Dictionary<Guid, PayrollAllowanceSummaryRecordRow>> EnsureSummaryRowsAsync(
        int year,
        int month,
        IReadOnlyCollection<Guid> employeeIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        var summariesByEmployeeId = await dbContext.PayrollAllowanceSummaryRecords
            .Where(x => x.PayrollYear == year && x.PayrollMonth == month && employeeIds.Contains(x.EmployeeId))
            .ToDictionaryAsync(x => x.EmployeeId, cancellationToken);

        foreach (var employeeId in employeeIds)
        {
            if (summariesByEmployeeId.ContainsKey(employeeId))
            {
                continue;
            }

            var summary = new PayrollAllowanceSummaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                PayrollYear = checked((short)year),
                PayrollMonth = checked((short)month),
                ResponsibilityAllowanceAmount = 0m,
                ResponsibilityOtherAllowanceAmount = 0m,
                SeniorityAllowanceAmount = 0m,
                AttendanceAllowanceAmount = 0m,
                MealAllowanceAmount = 0m,
                HazardAllowanceAmount = 0m,
                OtherAllowanceAmount = 0m,
                LeaveHolidayAllowanceAmount = 0m,
                IsLocked = false,
                CreatedAtUtc = now,
                CreatedBy = CurrentAuditUser
            };
            dbContext.PayrollAllowanceSummaryRecords.Add(summary);
            summariesByEmployeeId.Add(employeeId, summary);
        }

        return summariesByEmployeeId;
    }

    /// <summary>
    /// Đồng bộ tiền trách nhiệm từ ABC sang summary, kiểm tra liên kết nhân viên/kỳ
    /// và không sửa summary đã khóa.
    /// </summary>
    private async Task ApplyDownstreamSnapshotsAsync(
        IReadOnlyCollection<PayrollResponsibilityAllowanceAbcRow> abcRows,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (abcRows.Count == 0)
        {
            return;
        }

        var summaryIds = abcRows
            .Select(x => x.PayrollAllowanceSummaryRecordId)
            .Distinct()
            .ToArray();
        if (summaryIds.Any(x => x == Guid.Empty))
        {
            throw new InvalidOperationException("Dòng ABC phụ cấp trách nhiệm chưa được gắn Summary của kỳ lương.");
        }

        // Ưu tiên summary đang tracked để thấy cả summary mới tạo trong cùng transaction.
        var summariesById = dbContext.ChangeTracker
            .Entries<PayrollAllowanceSummaryRecordRow>()
            .Where(x => x.State != EntityState.Deleted && summaryIds.Contains(x.Entity.Id))
            .Select(x => x.Entity)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.Single());
        var missingSummaryIds = summaryIds.Where(x => !summariesById.ContainsKey(x)).ToArray();
        if (missingSummaryIds.Length > 0)
        {
            var persistedSummaries = await dbContext.PayrollAllowanceSummaryRecords
                .Where(x => missingSummaryIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            foreach (var summary in persistedSummaries)
            {
                summariesById.Add(summary.Id, summary);
            }
        }

        foreach (var abcRow in abcRows)
        {
            if (!summariesById.TryGetValue(abcRow.PayrollAllowanceSummaryRecordId, out var summary))
            {
                throw new InvalidOperationException("Không tìm thấy Summary được liên kết với dòng ABC phụ cấp trách nhiệm.");
            }

            if (summary.EmployeeId != abcRow.EmployeeId
                || summary.PayrollYear != abcRow.Year
                || summary.PayrollMonth != abcRow.Month)
            {
                throw new InvalidOperationException("Liên kết ABC và Summary không cùng nhân viên hoặc kỳ lương.");
            }

            // Khóa summary độc lập với khóa ABC; summary khóa không nhận đồng bộ tự động.
            if (!summary.IsLocked)
            {
                summary.ResponsibilityAllowanceAmount = abcRow.ActualResponsibilityAllowanceAmount;
                summary.UpdatedAtUtc = now;
                summary.UpdatedBy = CurrentAuditUser;
            }
        }
    }

    /// <summary>Ủy quyền công thức xếp loại ABC dùng chung.</summary>
    private static string ComputeAbcRating(
        decimal standardWorkDays,
        decimal actualWorkDays,
        bool hasUnexcusedAbsence = false) =>
        AbcPolicy.Evaluate(
            new ResponsibilityAllowanceAbcInput(
                standardWorkDays,
                actualWorkDays,
                hasUnexcusedAbsence
                    ? ResponsibilityAllowanceUnexcusedAbsenceState.Present
                    : ResponsibilityAllowanceUnexcusedAbsenceState.NotPresent))
            .Rating
            .ToStorageValue();

    /// <summary>Ủy quyền lấy hệ số ABC dùng chung.</summary>
    private static decimal GetAbcMultiplier(string abcRating) =>
        ResponsibilityAllowanceAbcPolicy.GetMultiplier(
            ResponsibilityAllowancePolicyStorageValues.ToAbcRating(abcRating));

    /// <summary>Ủy quyền công thức tiền thực tế dùng chung.</summary>
    private static decimal CalculateActualResponsibilityAllowanceAmount(
        decimal standardAmount,
        decimal standardWorkDays,
        decimal actualWorkDays,
        string abcRating,
        decimal monthlyPerformanceBonusAmount,
        bool isPerformanceBonusExcluded)
        => AmountCalculator.Calculate(
            new ResponsibilityAllowanceAmountInput(
                standardAmount,
                standardWorkDays,
                actualWorkDays,
                ResponsibilityAllowancePolicyStorageValues.ToAbcRating(abcRating),
                monthlyPerformanceBonusAmount,
                isPerformanceBonusExcluded
                    ? ResponsibilityAllowancePerformanceBonusApplication.Excluded
                    : ResponsibilityAllowancePerformanceBonusApplication.Applied))
            .ActualAmount;

    /// <summary>Ghép họ tên, bỏ thành phần trống và khoảng trắng thừa.</summary>
    private static string BuildEmployeeName(string? lastName, string? firstName)
    {
        return string.Join(
            " ",
            new[] { lastName, firstName }
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x!.Trim()));
    }

    /// <summary>Chọn tên đơn vị theo ưu tiên nhóm, tổ, rồi phòng/xưởng.</summary>
    private static string BuildDepartmentName(AttendanceDepartmentRow department)
    {
        return NormalizeOptional(department.GroupName)
            ?? NormalizeOptional(department.TeamName)
            ?? NormalizeOptional(department.DepartmentOrWorkshopName)
            ?? string.Empty;
    }

    /// <summary>Chuẩn hóa trường bắt buộc và báo lỗi có tên trường khi kết quả rỗng.</summary>
    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value);
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new InvalidOperationException($"{fieldName} là bắt buộc.")
            : normalized;
    }

    /// <summary>Trim trường tùy chọn và chuyển chuỗi trắng thành null để lưu nhất quán.</summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Trả thời điểm nghiệp vụ UTC+7 dạng Unspecified theo quy ước cột database.</summary>
    private static DateTime GetDatabaseNow() =>
        PostgreSqlTimestamp.ToTimestampWithoutTimeZone(DateTime.UtcNow.AddHours(7));

    /// <summary>Kiểm tra tháng hợp lệ và kỳ nằm trong phạm vi module được mở.</summary>
    private static void ValidatePeriod(int year, int month)
    {
        if (year < MinimumSupportedPeriod.Year || year > MaximumSupportedYear)
        {
            throw new InvalidOperationException(
                $"Năm dữ liệu phải nằm trong khoảng {MinimumSupportedPeriod.Year} đến {MaximumSupportedYear}.");
        }

        if (month is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        }

        if (year == MinimumSupportedPeriod.Year && month < MinimumSupportedPeriod.Month)
        {
            throw new InvalidOperationException(
                $"Mốc dữ liệu của màn phụ cấp trách nhiệm bắt đầu từ {MinimumSupportedPeriod.ToDisplayText()}.");
        }
    }

    /// <summary>Ánh xạ entity ABC đã materialize sang DTO trả về cho client.</summary>
    private static PayrollResponsibilityAllowanceAbcItemDto MapAbcDto(PayrollResponsibilityAllowanceAbcRow row)
    {
        return new PayrollResponsibilityAllowanceAbcItemDto(
            row.Id,
            row.EmployeeId,
            row.EmployeeCode,
            row.EmployeeName,
            row.DepartmentName,
            row.PositionId,
            row.PositionName,
            row.GradeId,
            row.GradeCode,
            row.GradeName,
            row.Year,
            row.Month,
            row.ActualWorkDays,
            row.StandardWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded,
            row.StandardResponsibilityAllowanceAmount,
            row.ActualResponsibilityAllowanceAmount,
            row.IsLocked,
            row.CalculatedAtUtc,
            row.CalculatedBy,
            row.UpdatedAtUtc,
            row.UpdatedBy,
            row.LockedAtUtc,
            row.LockedBy,
            row.Note,
            row.CreatedAtUtc);
    }

    /// <summary>Tạo expression EF-translatable để chiếu ABC sang DTO ngay trên database.</summary>
    private static System.Linq.Expressions.Expression<Func<PayrollResponsibilityAllowanceAbcRow, PayrollResponsibilityAllowanceAbcItemDto>> MapAbcDtoExpression()
    {
        return row => new PayrollResponsibilityAllowanceAbcItemDto(
            row.Id,
            row.EmployeeId,
            row.EmployeeCode,
            row.EmployeeName,
            row.DepartmentName,
            row.PositionId,
            row.PositionName,
            row.GradeId,
            row.GradeCode,
            row.GradeName,
            row.Year,
            row.Month,
            row.ActualWorkDays,
            row.StandardWorkDays,
            row.AbcRating,
            row.MonthlyPerformanceBonusAmount,
            row.IsPerformanceBonusExcluded,
            row.StandardResponsibilityAllowanceAmount,
            row.ActualResponsibilityAllowanceAmount,
            row.IsLocked,
            row.CalculatedAtUtc,
            row.CalculatedBy,
            row.UpdatedAtUtc,
            row.UpdatedBy,
            row.LockedAtUtc,
            row.LockedBy,
            row.Note,
            row.CreatedAtUtc);
    }

    #endregion

    #region Kiểu dữ liệu nội bộ

    private sealed record WorkdayAggregateSnapshot(
        decimal AdministrativeWorkdays,
        decimal LateEarlyDeductionDays,
        decimal SalaryWorkdays,
        bool HasUnexcusedAbsence,
        IReadOnlyList<string> StatusCodes);

    private sealed record SelectedSourceSnapshot(
        string SourceKey,
        string SourceLabel,
        PayrollResponsibilityAllowanceGradeRow? Grade,
        decimal StandardResponsibilityAllowanceAmount);

    private sealed record EmployeeSnapshot(
        Guid Id,
        string EmployeeCode,
        string EmployeeName,
        string? DepartmentName,
        Guid? PositionId,
        string PositionCode,
        string PositionName);

    private readonly record struct ResponsibilityAllowancePeriod(int Year, int Month)
    {
        public ResponsibilityAllowancePeriod GetPreviousPeriod()
        {
            return Month == 1
                ? new ResponsibilityAllowancePeriod(Year - 1, 12)
                : new ResponsibilityAllowancePeriod(Year, Month - 1);
        }

        public string ToDisplayText() => $"{Month:00}/{Year}";
    }

    #endregion
}
