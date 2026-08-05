using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vnta.Hrm.Application.Common;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class DatabaseAttendanceBiometricDataRefreshService(
    ApplicationDbContext dbContext,
    ILogger<DatabaseAttendanceBiometricDataRefreshService> logger,
    AttendanceBiometricDataRefreshProgressTracker progressTracker)
    : IAttendanceBiometricDataRefreshService
{
    // Quy ước trạng thái nhân sự còn hiệu lực để đưa vào quá trình tổng hợp.
    private const int ActiveEmployeeStatus = 2;
    private const int OnLeaveEmployeeStatus = 3;
    // Giá trị mặc định khi tạo profile thiết bị cho nhân sự chưa có bản ghi tương ứng.
    private const string DefaultGroupCode = "1";
    private const string DefaultTimeZoneCode = "0000000100000000";
    private const string DefaultPrivilegeCode = "0";
    private const string DefaultVerifyMode = "-1";
    // Nhãn đánh dấu ảnh được đồng bộ từ user_pictures sang bio_photos.
    private const string UserPictureSyncType = "SYNC_FROM_USER_PICTURES";
    private const string NoSource = "khong_tim_thay_du_lieu";

    // Danh sách alias cột/bảng để service tự dò nhiều schema nguồn khác nhau.
    private static readonly string[] EmployeeIdColumns = ["employeeid", "employee_id"];
    private static readonly string[] EmployeeCodeColumns = ["badgenumber", "badge_number", "employeecode", "employee_code", "empcode", "emp_code", "workno", "work_no", "cardno", "card_no"];
    private static readonly string[] UserIdColumns = ["userid", "user_id", "pin", "pin2", "id"];
    private static readonly string[] PayloadColumns = ["templatedata", "template_data", "tmp", "template", "fingerprint", "finger_data", "content", "data", "blob"];
    private static readonly string[] FacePayloadColumns = ["photo", "photodata", "photo_data", "pic", "image", "imagedata", "image_data", "avatar", "content", "data", "picture", "blob"];
    private static readonly string[] FingerprintTables = ["templatev10", "templatev9", "templatev8", "template", "userfinger", "fptemplate"];
    private static readonly string[] FaceSummaryTables = ["bio_photos", "user_pictures", "face_templates"];
    private static readonly string[] FaceTables = ["biophoto", "userpic", "face"];
    private static readonly string[] FingerprintFidColumns = ["fid", "fingerid", "finger_id"];
    private static readonly string[] BioDataTypeColumns = ["biotype", "bio_type", "type"];
    private static readonly string[] CardNumberColumns = ["cardnumber", "card_number", "cardno", "card_no"];
    private static readonly string[] PasswordColumns = ["password", "pwd", "passcode", "pin", "pin_code"];
    private static readonly string[] PrivilegeColumns = ["privilegecode", "privilege_code", "privilege", "pri", "role", "userrole", "user_role", "isadmin", "is_admin", "admin"];
    private static readonly string[] AvatarColumns = ["avatar"];
    private static readonly string[] UpdatedAtColumns = ["updatedatutc", "updated_at_utc", "updatedat", "updated_at"];
    private static readonly string[] CreatedAtColumns = ["createdatutc", "created_at_utc", "createdat", "created_at"];

    public Task<AttendanceBiometricDataRefreshProgress> GetProgressAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(progressTracker.Snapshot());

    public Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        CancellationToken cancellationToken = default)
        => RefreshInternalAsync(null, cancellationToken);

    public Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
        => RefreshInternalAsync(employeeId, cancellationToken);

    // Luồng chính:
    // 1. Bảo đảm bảng đích tồn tại.
    // 2. Xác định tập nhân sự cần xử lý.
    // 3. Đồng bộ các bảng phụ cần thiết.
    // 4. Đọc dữ liệu sinh trắc từ nhiều nguồn rồi gom về theo EmployeeId.
    // 5. Ghi kết quả cuối vào biometric_data.
    private async Task<AttendanceBiometricDataRefreshResult> RefreshInternalAsync(
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var totalEmployees = 0;
        var processedEmployees = 0;

        try
        {
            // Tạo bảng đích nếu DB mới hoặc local/dev chưa có migration áp sẵn.
            await EnsureBiometricDataTableAsync(cancellationToken);
            await EnsureEmployeeAvatarColumnAsync(cancellationToken);

            // Tải nhân sự đầu vào rồi tách active/inactive để xử lý đúng nghiệp vụ.
            var employees = await LoadEmployeesAsync(employeeId, cancellationToken);
            var activeEmployees = employees
                .Where(static employee => IsWorkingStatus(employee.Status))
                .ToList();
            var inactiveEmployeeIds = employees
                .Where(static employee => !IsWorkingStatus(employee.Status))
                .Select(static employee => employee.Id)
                .ToHashSet();

            if (employeeId.HasValue && employees.Count == 0)
            {
                inactiveEmployeeIds.Add(employeeId.Value);
            }

            totalEmployees = activeEmployees.Count;
            progressTracker.Start(totalEmployees, "Đang chuẩn bị dữ liệu nguồn");

            var activeEmployeeIds = activeEmployees
                .Select(static employee => employee.Id)
                .ToArray();
            var scopedEmployeeIds = activeEmployeeIds
                .Concat(inactiveEmployeeIds)
                .Distinct()
                .ToArray();

            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Đồng bộ profile thiết bị để nguồn card/password/admin luôn sẵn sàng.
            var profileSyncResult = await SyncDeviceUserProfilesAsync(
                activeEmployees,
                inactiveEmployeeIds,
                cancellationToken);

            // Bơm ảnh từ user_pictures sang bio_photos nếu bio_photos đang thiếu.
            await SyncBioPhotosFromUserPicturesAsync(
                activeEmployeeIds.ToHashSet(),
                cancellationToken);

            await SyncEmployeeAvatarsAsync(
                activeEmployeeIds.ToHashSet(),
                cancellationToken);

            // Tải snapshot tổng hợp từ nhiều bảng nguồn.
            var summaryResult = await LoadSummariesAsync(
                activeEmployees,
                cancellationToken);
            var summaryByEmployeeId = summaryResult.Summaries;

            var biometricRows = await dbContext.BiometricData
                .Where(row => scopedEmployeeIds.Contains(row.EmployeeId))
                .ToListAsync(cancellationToken);

            // Dọn dữ liệu trùng nếu một nhân sự đang có nhiều hơn một dòng tổng hợp.
            var duplicateRows = biometricRows
                .GroupBy(row => row.EmployeeId)
                .SelectMany(group => group
                    .OrderByDescending(row => row.LastUpdated)
                    .ThenByDescending(row => row.Id)
                    .Skip(1))
                .ToList();

            if (duplicateRows.Count > 0)
            {
                dbContext.BiometricData.RemoveRange(duplicateRows);
                biometricRows = biometricRows.Except(duplicateRows).ToList();
            }

            var biometricByEmployeeId = biometricRows.ToDictionary(row => row.EmployeeId);

            // Xóa các dòng không còn hợp lệ do nhân sự đã ra khỏi tập active.
            var staleRows = biometricRows
                .Where(row => inactiveEmployeeIds.Contains(row.EmployeeId))
                .ToList();

            if (staleRows.Count > 0)
            {
                dbContext.BiometricData.RemoveRange(staleRows);
                foreach (var staleRow in staleRows)
                {
                    biometricByEmployeeId.Remove(staleRow.EmployeeId);
                }
            }

            var refreshedAtUtc = DateTime.UtcNow;
            var refreshedAtDatabase = NormalizeDatabaseTimestamp(refreshedAtUtc);
            var inserted = 0;
            var updated = 0;

            progressTracker.Update(0, totalEmployees, "Đang tổng hợp dữ liệu nhân viên");

            // Mỗi nhân sự còn lại sẽ có đúng một dòng trong biometric_data.
            foreach (var employee in activeEmployees)
            {
                summaryByEmployeeId.TryGetValue(employee.Id, out var summary);

                // Nếu chưa có dòng tổng hợp thì tạo mới.
                if (!biometricByEmployeeId.TryGetValue(employee.Id, out var biometricRow))
                {
                    dbContext.BiometricData.Add(new AttendanceBiometricDataRow
                    {
                        Id = Guid.CreateVersion7(),
                        EmployeeId = employee.Id,
                        CardNumber = summary?.CardNumber,
                        Password = summary?.Password,
                        IsAdmin = summary?.IsAdmin ?? false,
                        FpQty = summary?.FpQty ?? 0,
                        HasFaceData = summary?.HasFaceData ?? false,
                        LastUpdated = refreshedAtDatabase
                    });

                    inserted++;
                }
                else
                {
                    // Nếu đã có thì cập nhật toàn bộ giá trị tổng hợp mới nhất.
                    biometricRow.CardNumber = summary?.CardNumber;
                    biometricRow.Password = summary?.Password;
                    biometricRow.IsAdmin = summary?.IsAdmin ?? false;
                    biometricRow.FpQty = summary?.FpQty ?? 0;
                    biometricRow.HasFaceData = summary?.HasFaceData ?? false;
                    biometricRow.LastUpdated = refreshedAtDatabase;
                    updated++;
                }

                processedEmployees++;
                progressTracker.Update(processedEmployees, totalEmployees, "Đang tổng hợp dữ liệu nhân viên");

                // Nhả nhịp ngắn để UI có thể đọc snapshot tiến độ trong lúc tổng hợp danh sách lớn.
                await Task.Yield();
            }

            // Ghi DB một lần cuối để tránh I/O dư thừa trong vòng lặp lớn.
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            progressTracker.Complete(totalEmployees, "Đã hoàn tất");

            // Ghi log ngắn gọn để biết lần chạy này lấy dữ liệu từ nguồn nào.
            logger.LogInformation(
                "Biometric refresh completed. ScopeEmployeeId={ScopeEmployeeId}, ActiveEmployees={ActiveEmployees}, Inserted={Inserted}, Updated={Updated}, ProfileInserted={ProfileInserted}, ProfileUpdated={ProfileUpdated}, ProfileDeleted={ProfileDeleted}, FingerprintSource={FingerprintSource}, FaceSource={FaceSource}",
                employeeId,
                activeEmployees.Count,
                inserted,
                updated,
                profileSyncResult.ProfilesInserted,
                profileSyncResult.ProfilesUpdated,
                profileSyncResult.ProfilesDeleted,
                summaryResult.FingerprintSource,
                summaryResult.FaceSource);

            return new AttendanceBiometricDataRefreshResult(
                activeEmployees.Count,
                inserted,
                updated,
                profileSyncResult.ProfilesInserted,
                profileSyncResult.ProfilesUpdated,
                profileSyncResult.ProfilesDeleted,
                summaryByEmployeeId.Count(static pair => pair.Value.FpQty > 0),
                summaryByEmployeeId.Count(static pair => pair.Value.HasFaceData),
                refreshedAtUtc,
                summaryResult.FingerprintSource,
                summaryResult.FaceSource);
        }
        catch (OperationCanceledException)
        {
            progressTracker.Fail(totalEmployees, processedEmployees, "Đã hủy");
            throw;
        }
        catch (Exception)
        {
            progressTracker.Fail(totalEmployees, processedEmployees, "Có lỗi khi tổng hợp dữ liệu");
            throw;
        }
    }

    // Auto-create bảng biometric_data cho local/dev hoặc DB mới trước khi tổng hợp.
    private async Task EnsureBiometricDataTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists "biometric_data" (
                "Id" uuid not null,
                "EmployeeId" uuid not null,
                "FpQty" integer not null default 0,
                "HasFaceData" boolean not null default false,
                "LastUpdated" timestamp without time zone not null,
                "CardNumber" character varying(255),
                "IsAdmin" boolean not null default false,
                "Password" character varying(255),
                constraint "PK_biometric_data" primary key ("Id"),
                constraint "FK_biometric_data_employees_EmployeeId"
                    foreign key ("EmployeeId")
                    references "employees" ("Id")
                    on delete restrict
            );

            create index if not exists "IX_biometric_data_EmployeeId"
                on "biometric_data" ("EmployeeId");

            create index if not exists "IX_biometric_data_LastUpdated"
                on "biometric_data" ("LastUpdated");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    // Bảo đảm bảng employees có cột Avatar để lần tổng hợp có thể cập nhật ảnh đại diện an toàn.
    private async Task EnsureEmployeeAvatarColumnAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            alter table if exists "employees"
            add column if not exists "Avatar" text null;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    // Đọc nhân sự đầu vào, đồng thời dựng snapshot gọn để các bước dưới không phải kéo theo entity đầy đủ.
    private async Task<List<EmployeeRefreshSnapshot>> LoadEmployeesAsync(
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Employees.AsNoTracking();

        if (employeeId.HasValue)
        {
            query = query.Where(employee => employee.Id == employeeId.Value);
        }

        return await query
            .OrderBy(employee => employee.EmployeeCode)
            .Select(employee => new EmployeeRefreshSnapshot(
                employee.Id,
                employee.EmployeeCode,
                (employee.LastName + " " + employee.FirstName).Trim(),
                employee.Status))
            .ToListAsync(cancellationToken);
    }

    // Đồng bộ bảng device_user_profiles:
    // - tạo profile mặc định nếu nhân sự active chưa có,
    // - cập nhật tên/mã khi đã có,
    // - xóa profile của nhân sự inactive trong phạm vi đang xử lý.
    private async Task<ProfileSyncResult> SyncDeviceUserProfilesAsync(
        IReadOnlyCollection<EmployeeRefreshSnapshot> activeEmployees,
        IReadOnlySet<Guid> inactiveEmployeeIds,
        CancellationToken cancellationToken)
    {
        var scopedEmployeeIds = activeEmployees.Select(static employee => employee.Id)
            .Concat(inactiveEmployeeIds)
            .Distinct()
            .ToArray();

        if (scopedEmployeeIds.Length == 0 ||
            !await TableExistsAsync("device_user_profiles", cancellationToken))
        {
            return new ProfileSyncResult(0, 0, 0);
        }

        var profilesInserted = 0;
        var profilesUpdated = 0;
        var profilesDeleted = 0;
        var syncTimestamp = GetDatabaseUtcNow();

        var existingProfiles = await dbContext.DeviceUserProfiles
            .Where(profile => scopedEmployeeIds.Contains(profile.EmployeeId))
            .ToListAsync(cancellationToken);

        if (inactiveEmployeeIds.Count > 0)
        {
            var inactiveProfiles = existingProfiles
                .Where(profile => inactiveEmployeeIds.Contains(profile.EmployeeId))
                .ToList();

            if (inactiveProfiles.Count > 0)
            {
                dbContext.DeviceUserProfiles.RemoveRange(inactiveProfiles);
                profilesDeleted += inactiveProfiles.Count;
                existingProfiles = existingProfiles.Except(inactiveProfiles).ToList();
            }
        }

        foreach (var employee in activeEmployees)
        {
            var profiles = existingProfiles
                .Where(profile => profile.EmployeeId == employee.Id)
                .ToList();

            if (profiles.Count == 0)
            {
                dbContext.DeviceUserProfiles.Add(new AttendanceDeviceUserProfileRow
                {
                    Id = Guid.CreateVersion7(),
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.EmployeeCode,
                    DeviceSn = string.Empty,
                    FullName = employee.FullName,
                    Password = null,
                    CardNumber = null,
                    GroupCode = DefaultGroupCode,
                    TimeZoneCode = DefaultTimeZoneCode,
                    PrivilegeCode = DefaultPrivilegeCode,
                    VerifyMode = DefaultVerifyMode,
                    ViceCard = null,
                    CreatedAtUtc = syncTimestamp,
                    UpdatedAtUtc = syncTimestamp
                });

                profilesInserted++;
                continue;
            }

            foreach (var profile in profiles)
            {
                profile.EmployeeCode = employee.EmployeeCode;
                profile.FullName = employee.FullName;
                profile.UpdatedAtUtc = syncTimestamp;
                profilesUpdated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileSyncResult(profilesInserted, profilesUpdated, profilesDeleted);
    }

    // Bổ sung dữ liệu ảnh khuôn mặt sang bio_photos nếu nguồn user_pictures có ảnh mà bio_photos còn thiếu.
    // Mục đích là tăng khả năng tổng hợp khuôn mặt trong các hệ thống có schema chưa đồng nhất.
    private async Task SyncBioPhotosFromUserPicturesAsync(
        IReadOnlySet<Guid> activeEmployeeIds,
        CancellationToken cancellationToken)
    {
        if (activeEmployeeIds.Count == 0 ||
            !await TableExistsAsync("user_pictures", cancellationToken) ||
            !await TableExistsAsync("bio_photos", cancellationToken))
        {
            return;
        }

        var userPictures = await dbContext.UserPictures
            .AsNoTracking()
            .Where(picture =>
                activeEmployeeIds.Contains(picture.EmployeeId)
                && picture.Content != null
                && picture.Content.Trim() != string.Empty)
            .OrderByDescending(picture => picture.UpdatedAtUtc ?? picture.CreatedAtUtc)
            .ThenByDescending(picture => picture.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var userPictureByEmployeeId = userPictures
            .GroupBy(picture => picture.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        if (userPictureByEmployeeId.Count == 0)
        {
            return;
        }

        var bioPhotos = await dbContext.BioPhotos
            .Where(photo => activeEmployeeIds.Contains(photo.EmployeeId))
            .OrderByDescending(photo => photo.UpdatedAtUtc ?? photo.CreatedAtUtc)
            .ThenByDescending(photo => photo.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var bioPhotoByEmployeeId = bioPhotos
            .GroupBy(photo => photo.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group.First());
        var syncTimestamp = GetDatabaseUtcNow();

        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => activeEmployeeIds.Contains(employee.Id))
            .Select(employee => new { employee.Id, employee.EmployeeCode })
            .ToDictionaryAsync(employee => employee.Id, cancellationToken);

        foreach (var (currentEmployeeId, userPicture) in userPictureByEmployeeId)
        {
            if (bioPhotoByEmployeeId.TryGetValue(currentEmployeeId, out var bioPhoto)
                && HasTextPayload(bioPhoto.Content))
            {
                continue;
            }

            if (!employees.TryGetValue(currentEmployeeId, out var employee))
            {
                continue;
            }

            if (bioPhoto is null)
            {
                dbContext.BioPhotos.Add(new AttendanceBioPhotoRow
                {
                    Id = Guid.CreateVersion7(),
                    EmployeeId = currentEmployeeId,
                    DeviceSn = userPicture.DeviceSn,
                    FileName = string.IsNullOrWhiteSpace(userPicture.FileName)
                        ? $"{employee.EmployeeCode}.jpg"
                        : userPicture.FileName,
                    Type = UserPictureSyncType,
                    Size = userPicture.Size ?? userPicture.Content.Length,
                    Content = userPicture.Content,
                    CreatedAtUtc = syncTimestamp,
                    UpdatedAtUtc = syncTimestamp
                });

                continue;
            }

            bioPhoto.DeviceSn = string.IsNullOrWhiteSpace(bioPhoto.DeviceSn)
                ? userPicture.DeviceSn
                : bioPhoto.DeviceSn;
            bioPhoto.FileName = string.IsNullOrWhiteSpace(bioPhoto.FileName)
                ? userPicture.FileName
                : bioPhoto.FileName;
            bioPhoto.Type ??= UserPictureSyncType;
            bioPhoto.Size = userPicture.Size ?? userPicture.Content.Length;
            bioPhoto.Content = userPicture.Content;
            bioPhoto.UpdatedAtUtc = syncTimestamp;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Cập nhật employees.Avatar theo thứ tự ưu tiên:
    // 1. lấy từ bio_photos nếu có,
    // 2. nếu không có thì fallback sang user_pictures,
    // 3. nếu cả hai nguồn đều trống thì xóa Avatar để phản ánh đúng dữ liệu mới nhất.
    private async Task SyncEmployeeAvatarsAsync(
        IReadOnlySet<Guid> activeEmployeeIds,
        CancellationToken cancellationToken)
    {
        if (activeEmployeeIds.Count == 0)
        {
            return;
        }

        var employees = await dbContext.Employees
            .Where(employee => activeEmployeeIds.Contains(employee.Id))
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        var avatarByEmployeeId = new Dictionary<Guid, string?>(employees.Count);

        if (await TableExistsAsync("bio_photos", cancellationToken))
        {
            var bioPhotos = await dbContext.BioPhotos
                .AsNoTracking()
                .Where(photo =>
                    activeEmployeeIds.Contains(photo.EmployeeId)
                    && photo.Content != null
                    && photo.Content.Trim() != string.Empty)
                .OrderByDescending(photo => photo.UpdatedAtUtc ?? photo.CreatedAtUtc)
                .ThenByDescending(photo => photo.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var group in bioPhotos.GroupBy(photo => photo.EmployeeId))
            {
                var normalizedAvatar = group
                    .Select(photo => AvatarImageSourceHelper.NormalizeSource(photo.Content))
                    .FirstOrDefault(static avatar => !string.IsNullOrWhiteSpace(avatar));

                if (!string.IsNullOrWhiteSpace(normalizedAvatar))
                {
                    avatarByEmployeeId[group.Key] = normalizedAvatar;
                }
            }
        }

        if (await TableExistsAsync("user_pictures", cancellationToken))
        {
            var missingEmployeeIds = employees
                .Select(employee => employee.Id)
                .Where(employeeId => !avatarByEmployeeId.ContainsKey(employeeId))
                .ToArray();

            if (missingEmployeeIds.Length > 0)
            {
                var userPictures = await dbContext.UserPictures
                    .AsNoTracking()
                    .Where(picture =>
                        missingEmployeeIds.Contains(picture.EmployeeId)
                        && picture.Content != null
                        && picture.Content.Trim() != string.Empty)
                    .OrderByDescending(picture => picture.UpdatedAtUtc ?? picture.CreatedAtUtc)
                    .ThenByDescending(picture => picture.CreatedAtUtc)
                    .ToListAsync(cancellationToken);

                foreach (var group in userPictures.GroupBy(picture => picture.EmployeeId))
                {
                    var normalizedAvatar = group
                        .Select(picture => AvatarImageSourceHelper.NormalizeSource(picture.Content))
                        .FirstOrDefault(static avatar => !string.IsNullOrWhiteSpace(avatar));

                    if (!string.IsNullOrWhiteSpace(normalizedAvatar))
                    {
                        avatarByEmployeeId[group.Key] = normalizedAvatar;
                    }
                }
            }
        }

        foreach (var employee in employees)
        {
            employee.Avatar = avatarByEmployeeId.GetValueOrDefault(employee.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Ghép các mảnh dữ liệu sinh trắc từ nhiều nguồn về một snapshot theo EmployeeId.
    // Đây là nơi quyết định card/password/admin/fp_qty/has_face_data của từng nhân sự.
    private async Task<BiometricSummaryLoadResult> LoadSummariesAsync(
        IReadOnlyCollection<EmployeeRefreshSnapshot> activeEmployees,
        CancellationToken cancellationToken)
    {
        if (activeEmployees.Count == 0)
        {
            return new BiometricSummaryLoadResult([], NoSource, NoSource);
        }

        var employeeIds = activeEmployees
            .Select(static employee => employee.Id)
            .ToHashSet();
        var result = new Dictionary<Guid, BiometricSummarySnapshot>(employeeIds.Count);
        string fingerprintSource;
        string faceSource;

        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var profileSummaries = await LoadProfileSummariesAsync(connection, employeeIds, cancellationToken);
            var fingerprintLoadResult = await LoadFingerprintCountsAsync(connection, employeeIds, cancellationToken);
            var faceLoadResult = await LoadFaceEmployeeIdsAsync(connection, activeEmployees, employeeIds, cancellationToken);
            var fingerprintCounts = fingerprintLoadResult.Counts;
            var faceEmployeeIds = faceLoadResult.EmployeeIds;
            fingerprintSource = fingerprintLoadResult.Source;
            faceSource = faceLoadResult.Source;

            foreach (var employee in activeEmployees)
            {
                profileSummaries.TryGetValue(employee.Id, out var profileSummary);
                result[employee.Id] = new BiometricSummarySnapshot(
                    profileSummary?.CardNumber,
                    profileSummary?.Password,
                    profileSummary?.IsAdmin ?? false,
                    fingerprintCounts.GetValueOrDefault(employee.Id),
                    faceEmployeeIds.Contains(employee.Id));
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return new BiometricSummaryLoadResult(result, fingerprintSource, faceSource);
    }

    // Đọc thông tin profile ưu tiên từ device_user_profiles và chọn bản ghi "tốt nhất" theo UpdatedAt/CreatedAt.
    private static async Task<Dictionary<Guid, ProfileSummarySnapshot>> LoadProfileSummariesAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, "device_user_profiles", cancellationToken);
        if (resolvedTable is null || employeeIds.Count == 0)
        {
            return [];
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
        if (employeeIdColumn is null)
        {
            return [];
        }

        var cardNumberColumn = PickFirst(columns, CardNumberColumns);
        var passwordColumn = PickFirst(columns, PasswordColumns);
        var privilegeColumn = PickFirst(columns, PrivilegeColumns);
        var updatedAtColumn = PickFirst(columns, UpdatedAtColumns);
        var createdAtColumn = PickFirst(columns, CreatedAtColumns);
        var rowIdColumn = columns.TryGetValue("id", out var actualRowIdColumn)
            ? actualRowIdColumn
            : null;

        var sql = $"""
            select
                cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id,
                {BuildNullableTextProjection(cardNumberColumn, "card_number")} ,
                {BuildNullableTextProjection(passwordColumn, "password_value")} ,
                {BuildNullableTextProjection(privilegeColumn, "privilege_value")} ,
                {BuildNullableTimestampProjection(updatedAtColumn, "updated_at")} ,
                {BuildNullableTimestampProjection(createdAtColumn, "created_at")} ,
                {BuildNullableTextProjection(rowIdColumn, "row_id")}
            from {QuoteIdentifier(resolvedTable)}
            where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
            """;

        var rows = new List<ProfileSummaryProjection>();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var employeeIdText = reader.GetString(0)?.Trim();
            if (!Guid.TryParse(employeeIdText, out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            rows.Add(new ProfileSummaryProjection(
                employeeId,
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }

        return rows
            .GroupBy(static row => row.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group =>
                {
                    var orderedRows = group
                        .OrderByDescending(row => row.UpdatedAt ?? row.CreatedAt)
                        .ThenByDescending(row => row.CreatedAt)
                        .ThenByDescending(row => row.RowId)
                        .ToArray();

                    return new ProfileSummarySnapshot(
                        orderedRows
                            .Select(static row => NormalizeOptional(row.CardNumber))
                            .FirstOrDefault(static value => value is not null),
                        orderedRows
                            .Select(static row => NormalizeOptional(row.Password))
                            .FirstOrDefault(static value => value is not null),
                        orderedRows.Any(static row => ParseAdminValue(row.PrivilegeValue)));
                });
    }

    // Tải số lượng vân tay theo chiến lược nhiều lớp:
    // 1. fingerprint_templates
    // 2. biodata
    // 3. các bảng legacy như templatev10/template/userfinger...
    private static async Task<FingerprintLoadResult> LoadFingerprintCountsAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var summaryCounts = new Dictionary<Guid, int>();
        var matchedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var templateLoadResult = await TryLoadFingerprintTemplateCountsAsync(connection, employeeIds, cancellationToken);
        MergeCounts(summaryCounts, templateLoadResult.Counts);
        if (templateLoadResult.Counts.Count > 0)
        {
            matchedSources.Add(templateLoadResult.Source);
        }

        var bioDataLoadResult = await TryLoadFingerprintCountsFromBioDataAsync(connection, employeeIds, cancellationToken);
        MergeCounts(summaryCounts, bioDataLoadResult.Counts);
        if (bioDataLoadResult.Counts.Count > 0)
        {
            matchedSources.Add(bioDataLoadResult.Source);
        }

        if (summaryCounts.Count > 0)
        {
            return new FingerprintLoadResult(summaryCounts, JoinSources(matchedSources));
        }

        var legacyCounts = new Dictionary<Guid, int>();
        matchedSources.Clear();
        foreach (var tableName in FingerprintTables)
        {
            var resolvedTable = await ResolveTableNameAsync(connection, tableName, cancellationToken);
            if (resolvedTable is null)
            {
                continue;
            }

            var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
            var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
            var payloadColumn = PickFirst(columns, PayloadColumns);
            if (employeeIdColumn is null || payloadColumn is null)
            {
                continue;
            }

            var counts = await QueryCountsByEmployeeIdAsync(
                connection,
                employeeIds,
                $"""
                select cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id, count(*)::int as fp_qty
                from {QuoteIdentifier(resolvedTable)}
                where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
                  and {QuoteIdentifier(payloadColumn)} is not null
                  and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
                group by cast({QuoteIdentifier(employeeIdColumn)} as text)
                """,
                cancellationToken);

            MergeCounts(legacyCounts, counts);
            if (counts.Count > 0)
            {
                matchedSources.Add(resolvedTable);
            }
        }

        return new FingerprintLoadResult(legacyCounts, JoinSources(matchedSources));
    }

    // Tìm nhân sự có dữ liệu khuôn mặt:
    // - ưu tiên các bảng summary hiện đại,
    // - nếu không có thì fallback sang nhóm bảng legacy.
    private static async Task<FaceLoadResult> LoadFaceEmployeeIdsAsync(
        NpgsqlConnection connection,
        IReadOnlyCollection<EmployeeRefreshSnapshot> activeEmployees,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var summaryLoadResult = await TryLoadFaceEmployeeIdsFromSummaryTablesAsync(connection, employeeIds, cancellationToken);
        if (summaryLoadResult.EmployeeIds.Count > 0)
        {
            return summaryLoadResult;
        }

        return await LoadLegacyFaceEmployeeIdsAsync(connection, activeEmployees, cancellationToken);
    }

    // Quét các bảng chuẩn/đời mới để xác định nhân sự có payload khuôn mặt hợp lệ.
    private static async Task<FaceLoadResult> TryLoadFaceEmployeeIdsFromSummaryTablesAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<Guid>();
        var matchedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableName in FaceSummaryTables)
        {
            var resolvedTable = await ResolveTableNameAsync(connection, tableName, cancellationToken);
            if (resolvedTable is null)
            {
                continue;
            }

            var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
            var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
            var payloadColumn = PickFirst(columns, FacePayloadColumns.Concat(PayloadColumns));
            if (employeeIdColumn is null || payloadColumn is null)
            {
                continue;
            }

            var matched = await QueryEmployeeIdsAsync(
                connection,
                employeeIds,
                $"""
                select distinct cast({QuoteIdentifier(employeeIdColumn)} as text)
                from {QuoteIdentifier(resolvedTable)}
                where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
                  and {QuoteIdentifier(payloadColumn)} is not null
                  and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
                """,
                cancellationToken);

            foreach (var employeeId in matched)
            {
                result.Add(employeeId);
            }

            if (matched.Count > 0)
            {
                matchedSources.Add(resolvedTable);
            }
        }

        var bioDataFaceEmployeeIds = await TryLoadFaceEmployeeIdsFromBioDataAsync(connection, employeeIds, cancellationToken);
        foreach (var employeeId in bioDataFaceEmployeeIds)
        {
            result.Add(employeeId);
        }

        if (bioDataFaceEmployeeIds.Count > 0)
        {
            matchedSources.Add("biodata");
        }

        return new FaceLoadResult(result, JoinSources(matchedSources));
    }

    // Fallback cho các hệ thống cũ, nơi dữ liệu ảnh có thể nằm ở nhiều bảng và liên kết bằng employeeId/code/userId.
    private static async Task<FaceLoadResult> LoadLegacyFaceEmployeeIdsAsync(
        NpgsqlConnection connection,
        IReadOnlyCollection<EmployeeRefreshSnapshot> activeEmployees,
        CancellationToken cancellationToken)
    {
        if (activeEmployees.Count == 0)
        {
            return new FaceLoadResult([], NoSource);
        }

        var userInfoTable = await ResolveLegacyUserInfoMetadataAsync(connection, cancellationToken);
        var result = new HashSet<Guid>();
        var matchedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var logicalTableName in FaceTables)
        {
            var table = await ResolveLegacyPhotoTableMetadataAsync(connection, logicalTableName, cancellationToken);
            if (table is null)
            {
                continue;
            }

            var snapshots = await LoadLegacyPhotoSnapshotsAsync(
                connection,
                table,
                userInfoTable,
                activeEmployees,
                requireContent: true,
                cancellationToken);

            foreach (var employeeId in snapshots.Keys)
            {
                result.Add(employeeId);
            }

            if (snapshots.Count > 0)
            {
                matchedSources.Add(table.TableName);
            }
        }

        return new FaceLoadResult(result, JoinSources(matchedSources));
    }

    // Đếm số vân tay từ bảng fingerprint_templates.
    // Mỗi cặp EmployeeId + FID được tính 1 lần để tránh đếm trùng nhiều phiên bản cùng ngón tay.
    private static async Task<FingerprintLoadResult> TryLoadFingerprintTemplateCountsAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, "fingerprint_templates", cancellationToken);
        if (resolvedTable is null || employeeIds.Count == 0)
        {
            return new FingerprintLoadResult([], NoSource);
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
        var fidColumn = PickFirst(columns, FingerprintFidColumns);
        var payloadColumn = PickFirst(columns, PayloadColumns);
        var updatedAtColumn = PickFirst(columns, UpdatedAtColumns);
        var createdAtColumn = PickFirst(columns, CreatedAtColumns);
        if (employeeIdColumn is null || fidColumn is null || payloadColumn is null)
        {
            return new FingerprintLoadResult([], NoSource);
        }

        var sql = $"""
            select
                cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id,
                cast({QuoteIdentifier(fidColumn)} as text) as fid,
                {BuildNullableTimestampProjection(updatedAtColumn, "updated_at")} ,
                {BuildNullableTimestampProjection(createdAtColumn, "created_at")}
            from {QuoteIdentifier(resolvedTable)}
            where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
              and {QuoteIdentifier(payloadColumn)} is not null
              and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());

        var rows = new List<FingerprintCountProjection>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var employeeIdText = reader.GetString(0)?.Trim();
            if (!Guid.TryParse(employeeIdText, out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            var fid = reader.IsDBNull(1)
                ? null
                : reader.GetString(1)?.Trim();
            if (string.IsNullOrWhiteSpace(fid))
            {
                continue;
            }

            rows.Add(new FingerprintCountProjection(
                employeeId,
                fid,
                reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                reader.IsDBNull(3) ? null : reader.GetDateTime(3)));
        }

        var result = rows
            .GroupBy(row => new { row.EmployeeId, row.Fid })
            .Select(group => group
                .OrderByDescending(row => row.UpdatedAt)
                .ThenByDescending(row => row.CreatedAt)
                .First())
            .GroupBy(static row => row.EmployeeId)
            .ToDictionary(static group => group.Key, static group => group.Count());

        return new FingerprintLoadResult(result, result.Count > 0 ? resolvedTable : NoSource);
    }

    // Đếm số vân tay từ bảng biodata.
    // Tại đây loại trừ các bản ghi thuộc loại khuôn mặt và gom theo khóa đặc trưng của mẫu sinh trắc.
    private static async Task<FingerprintLoadResult> TryLoadFingerprintCountsFromBioDataAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, "biodata", cancellationToken);
        if (resolvedTable is null || employeeIds.Count == 0)
        {
            return new FingerprintLoadResult([], NoSource);
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
        var bioTypeColumn = PickFirst(columns, BioDataTypeColumns);
        var payloadColumn = PickFirst(columns, PayloadColumns);
        var bioNoColumn = columns.TryGetValue("biono", out var actualBioNoColumn) ? actualBioNoColumn : null;
        var bioIndexColumn = columns.TryGetValue("bioindex", out var actualBioIndexColumn) ? actualBioIndexColumn : null;
        if (employeeIdColumn is null || bioTypeColumn is null || payloadColumn is null)
        {
            return new FingerprintLoadResult([], NoSource);
        }

        var sql = $"""
            select
                cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id,
                cast({QuoteIdentifier(bioTypeColumn)} as text) as bio_type,
                {BuildNullableTextProjection(bioNoColumn, "bio_no")} ,
                {BuildNullableTextProjection(bioIndexColumn, "bio_index")}
            from {QuoteIdentifier(resolvedTable)}
            where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
              and {QuoteIdentifier(payloadColumn)} is not null
              and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());

        var rows = new List<BioDataFingerprintProjection>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var employeeIdText = reader.GetString(0)?.Trim();
            if (!Guid.TryParse(employeeIdText, out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            var bioType = reader.IsDBNull(1)
                ? null
                : reader.GetString(1)?.Trim();
            if (IsFaceBioType(bioType))
            {
                continue;
            }

            rows.Add(new BioDataFingerprintProjection(
                employeeId,
                bioType,
                reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim(),
                reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim()));
        }

        var counts = rows
            .GroupBy(static row => new
            {
                row.EmployeeId,
                BioType = NormalizeOptional(row.BioType) ?? string.Empty,
                BioNo = NormalizeOptional(row.BioNo) ?? string.Empty,
                BioIndex = NormalizeOptional(row.BioIndex) ?? string.Empty
            })
            .Select(static group => group.Key)
            .GroupBy(static row => row.EmployeeId)
            .ToDictionary(static group => group.Key, static group => group.Count());

        return new FingerprintLoadResult(counts, counts.Count > 0 ? resolvedTable : NoSource);
    }

    // Xác định nhân sự có dữ liệu khuôn mặt từ biodata dựa trên cột type/biotype.
    private static async Task<HashSet<Guid>> TryLoadFaceEmployeeIdsFromBioDataAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, "biodata", cancellationToken);
        if (resolvedTable is null || employeeIds.Count == 0)
        {
            return [];
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
        var typeColumn = PickFirst(columns, BioDataTypeColumns);
        var payloadColumn = PickFirst(columns, PayloadColumns);
        if (employeeIdColumn is null || typeColumn is null || payloadColumn is null)
        {
            return [];
        }

        var sql = $"""
            select
                cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id,
                cast({QuoteIdentifier(typeColumn)} as text) as bio_type
            from {QuoteIdentifier(resolvedTable)}
            where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
              and {QuoteIdentifier(payloadColumn)} is not null
              and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());

        var result = new HashSet<Guid>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var employeeIdText = reader.GetString(0)?.Trim();
            if (!Guid.TryParse(employeeIdText, out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            var bioType = reader.IsDBNull(1)
                ? null
                : reader.GetString(1)?.Trim();
            if (IsFaceBioType(bioType))
            {
                result.Add(employeeId);
            }
        }

        return result;
    }

    // Dò metadata cho một bảng ảnh legacy để biết có thể nối dữ liệu theo employeeId, employeeCode hay userId.
    private static async Task<LegacyPhotoTableMetadata?> ResolveLegacyPhotoTableMetadataAsync(
        NpgsqlConnection connection,
        string logicalTableName,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, logicalTableName, cancellationToken);
        if (resolvedTable is null)
        {
            return null;
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var contentColumn = PickFirst(columns, FacePayloadColumns.Concat(PayloadColumns));
        if (contentColumn is null)
        {
            return null;
        }

        return new LegacyPhotoTableMetadata(
            resolvedTable,
            contentColumn,
            columns.TryGetValue("id", out var actualRowIdColumn) ? actualRowIdColumn : null,
            PickFirst(columns, EmployeeIdColumns),
            PickFirst(columns, EmployeeCodeColumns),
            PickFirst(columns, UserIdColumns),
            PickFirst(columns, UpdatedAtColumns),
            PickFirst(columns, CreatedAtColumns));
    }

    // Dò bảng userinfo legacy để dùng như cầu nối giữa userId thiết bị và employeeCode.
    private static async Task<LegacyUserInfoMetadata?> ResolveLegacyUserInfoMetadataAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var resolvedTable = await ResolveTableNameAsync(connection, "userinfo", cancellationToken);
        if (resolvedTable is null)
        {
            return null;
        }

        var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
        var employeeCodeColumn = PickFirst(columns, EmployeeCodeColumns);
        var userIdColumn = PickFirst(columns, UserIdColumns);
        if (employeeCodeColumn is null || userIdColumn is null)
        {
            return null;
        }

        return new LegacyUserInfoMetadata(resolvedTable, employeeCodeColumn, userIdColumn);
    }

    // Tải snapshot ảnh legacy theo chiến lược nối linh hoạt:
    // - nối trực tiếp bằng employeeId nếu có,
    // - nếu không có thì nối bằng employeeCode,
    // - cuối cùng mới nối qua userinfo bằng userId.
    private static async Task<Dictionary<Guid, PhotoSyncSnapshot>> LoadLegacyPhotoSnapshotsAsync(
        NpgsqlConnection connection,
        LegacyPhotoTableMetadata table,
        LegacyUserInfoMetadata? userInfoTable,
        IReadOnlyCollection<EmployeeRefreshSnapshot> activeEmployees,
        bool requireContent,
        CancellationToken cancellationToken)
    {
        var employeesById = activeEmployees.ToDictionary(static employee => employee.Id);
        var employeesByCode = activeEmployees
            .Where(static employee => !string.IsNullOrWhiteSpace(employee.EmployeeCode))
            .GroupBy(static employee => employee.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        string sql;
        string parameterName;
        string[] parameterValues;
        var matchByEmployeeId = false;

        if (table.EmployeeIdColumn is not null)
        {
            matchByEmployeeId = true;
            parameterName = "employeeIds";
            parameterValues = activeEmployees.Select(static employee => employee.Id.ToString()).ToArray();
            sql = $"""
                select
                    cast({QuoteIdentifier(table.EmployeeIdColumn)} as text) as employee_match,
                    {BuildNullableTextProjection(table.RowIdColumn, "row_id")} ,
                    {BuildNullableTextProjection(table.UserIdColumn, "user_id")} ,
                    cast({QuoteIdentifier(table.ContentColumn)} as text) as content_value,
                    {BuildNullableTimestampProjection(table.UpdatedAtColumn, "updated_at")} ,
                    {BuildNullableTimestampProjection(table.CreatedAtColumn, "created_at")}
                from {QuoteIdentifier(table.TableName)}
                where cast({QuoteIdentifier(table.EmployeeIdColumn)} as text) = any(@employeeIds)
                """;
        }
        else if (table.EmployeeCodeColumn is not null)
        {
            parameterName = "employeeCodes";
            parameterValues = employeesByCode.Keys.ToArray();
            sql = $"""
                select
                    cast({QuoteIdentifier(table.EmployeeCodeColumn)} as text) as employee_match,
                    {BuildNullableTextProjection(table.RowIdColumn, "row_id")} ,
                    {BuildNullableTextProjection(table.UserIdColumn, "user_id")} ,
                    cast({QuoteIdentifier(table.ContentColumn)} as text) as content_value,
                    {BuildNullableTimestampProjection(table.UpdatedAtColumn, "updated_at")} ,
                    {BuildNullableTimestampProjection(table.CreatedAtColumn, "created_at")}
                from {QuoteIdentifier(table.TableName)}
                where cast({QuoteIdentifier(table.EmployeeCodeColumn)} as text) = any(@employeeCodes)
                """;
        }
        else if (table.UserIdColumn is not null && userInfoTable is not null)
        {
            parameterName = "employeeCodes";
            parameterValues = employeesByCode.Keys.ToArray();
            sql = $"""
                select
                    cast(u.{QuoteIdentifier(userInfoTable.EmployeeCodeColumn)} as text) as employee_match,
                    {BuildNullableTextProjection(table.RowIdColumn, "row_id", "p")} ,
                    cast(p.{QuoteIdentifier(table.UserIdColumn)} as text) as user_id,
                    cast(p.{QuoteIdentifier(table.ContentColumn)} as text) as content_value,
                    {BuildNullableTimestampProjection(table.UpdatedAtColumn, "updated_at", "p")} ,
                    {BuildNullableTimestampProjection(table.CreatedAtColumn, "created_at", "p")}
                from {QuoteIdentifier(table.TableName)} p
                inner join {QuoteIdentifier(userInfoTable.TableName)} u
                    on p.{QuoteIdentifier(table.UserIdColumn)} = u.{QuoteIdentifier(userInfoTable.UserIdColumn)}
                where cast(u.{QuoteIdentifier(userInfoTable.EmployeeCodeColumn)} as text) = any(@employeeCodes)
                """;
        }
        else
        {
            return [];
        }

        if (parameterValues.Length == 0)
        {
            return [];
        }

        var rows = new List<PhotoSyncSnapshot>();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(parameterName, parameterValues);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            EmployeeRefreshSnapshot? employee = null;
            if (matchByEmployeeId)
            {
                var employeeIdText = reader.GetString(0)?.Trim();
                if (!Guid.TryParse(employeeIdText, out var employeeId) || !employeesById.TryGetValue(employeeId, out employee))
                {
                    continue;
                }
            }
            else
            {
                var employeeCode = reader.GetString(0)?.Trim();
                if (string.IsNullOrWhiteSpace(employeeCode) || !employeesByCode.TryGetValue(employeeCode, out employee))
                {
                    continue;
                }
            }

            if (employee is null)
            {
                continue;
            }

            var content = reader.IsDBNull(3) ? null : reader.GetString(3);
            if (requireContent && string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var rowId = reader.IsDBNull(1) ? null : reader.GetString(1)?.Trim();
            var userId = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim();
            DateTime? updatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
            DateTime? createdAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5);

            rows.Add(new PhotoSyncSnapshot(
                employee.Id,
                employee.EmployeeCode.Trim(),
                rowId,
                userId,
                content,
                updatedAt,
                createdAt));
        }

        return rows
            .GroupBy(static row => row.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(row => row.UpdatedAt ?? row.CreatedAt)
                    .ThenByDescending(row => row.CreatedAt)
                    .ThenByDescending(row => row.RowId)
                    .First());
    }

    // Helper kiểm tra bảng có tồn tại thật trong DB nguồn/đích hay không.
    private async Task<bool> TableExistsAsync(
        string expectedTableName,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await ResolveTableNameAsync(connection, expectedTableName, cancellationToken) is not null;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Khi cùng một nhân sự được tìm thấy ở nhiều nguồn vân tay, giữ số lượng lớn nhất làm kết quả cuối.
    private static void MergeCounts(Dictionary<Guid, int> target, IReadOnlyDictionary<Guid, int> source)
    {
        foreach (var pair in source)
        {
            target[pair.Key] = target.TryGetValue(pair.Key, out var existing)
                ? Math.Max(existing, pair.Value)
                : pair.Value;
        }
    }

    // Helper query generic cho bài toán "đếm theo employeeId".
    private static async Task<Dictionary<Guid, int>> QueryCountsByEmployeeIdAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        string sql,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, int>();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            if (!Guid.TryParse(reader.GetString(0)?.Trim(), out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            result[employeeId] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
        }

        return result;
    }

    // Helper query generic cho bài toán "lấy tập employeeId thỏa điều kiện".
    private static async Task<HashSet<Guid>> QueryEmployeeIdsAsync(
        NpgsqlConnection connection,
        IReadOnlySet<Guid> employeeIds,
        string sql,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<Guid>();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static id => id.ToString()).ToArray());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            if (!Guid.TryParse(reader.GetString(0)?.Trim(), out var employeeId) || !employeeIds.Contains(employeeId))
            {
                continue;
            }

            result.Add(employeeId);
        }

        return result;
    }

    // Tìm tên bảng thực trong DB theo kiểu không phân biệt hoa thường.
    private static async Task<string?> ResolveTableNameAsync(
        NpgsqlConnection connection,
        string expectedTableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select table_name
            from information_schema.tables
            where table_schema not in ('pg_catalog', 'information_schema')
              and lower(table_name) = @tableName
            limit 1
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tableName", expectedTableName.ToLowerInvariant());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    // Đọc danh sách cột thực tế để các query dynamic phía trên tự thích nghi với nhiều schema.
    private static async Task<Dictionary<string, string>> GetColumnNamesAsync(
        NpgsqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select column_name
            from information_schema.columns
            where table_schema not in ('pg_catalog', 'information_schema')
              and lower(table_name) = @tableName
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tableName", tableName.ToLowerInvariant());

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var columnName = reader.GetString(0);
            result[columnName.ToLowerInvariant()] = columnName;
        }

        return result;
    }

    // Chọn tên cột đầu tiên khớp với tập alias đã biết.
    private static string? PickFirst(
        IReadOnlyDictionary<string, string> columns,
        IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (columns.TryGetValue(candidate.ToLowerInvariant(), out var actualColumnName))
            {
                return actualColumnName;
            }
        }

        return null;
    }

    // Sinh projection text an toàn cho query dynamic, kể cả khi cột không tồn tại.
    private static string BuildNullableTextProjection(
        string? columnName,
        string alias,
        string? tableAlias = null)
    {
        if (columnName is null)
        {
            return $"null::text as {alias}";
        }

        var prefix = tableAlias is null ? string.Empty : tableAlias + ".";
        return $"cast({prefix}{QuoteIdentifier(columnName)} as text) as {alias}";
    }

    // Sinh projection timestamp an toàn cho query dynamic, kể cả khi cột không tồn tại.
    private static string BuildNullableTimestampProjection(
        string? columnName,
        string alias,
        string? tableAlias = null)
    {
        if (columnName is null)
        {
            return $"null::timestamp as {alias}";
        }

        var prefix = tableAlias is null ? string.Empty : tableAlias + ".";
        return $"cast({prefix}{QuoteIdentifier(columnName)} as timestamp) as {alias}";
    }

    // Escape identifier để không bị lỗi khi tên bảng/cột có ký tự đặc biệt hoặc chữ hoa.
    private static string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    // Chuẩn hóa danh sách nguồn dữ liệu dùng cho log/toast/debug.
    private static string JoinSources(IEnumerable<string> sources)
    {
        var values = sources
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? NoSource : string.Join(", ", values);
    }

    // Đồng bộ kiểu thời gian về timestamp không timezone vì schema đích đang lưu theo dạng này.
    private static DateTime GetDatabaseUtcNow()
        => NormalizeDatabaseTimestamp(DateTime.UtcNow);

    private static DateTime NormalizeDatabaseTimestamp(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    // Chỉ tổng hợp cho nhân sự đang làm việc hoặc nghỉ phép hợp lệ.
    private static bool IsWorkingStatus(int status)
        => status is ActiveEmployeeStatus or OnLeaveEmployeeStatus;

    // Heuristic nhận diện các giá trị type đại diện cho dữ liệu khuôn mặt.
    private static bool IsFaceBioType(string? bioType)
    {
        if (string.IsNullOrWhiteSpace(bioType))
        {
            return false;
        }

        var normalized = bioType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "face" => true,
            "photo" => true,
            "avatar" => true,
            "7" => true,
            "8" => true,
            _ => normalized.Contains("face", StringComparison.Ordinal)
        };
    }

    // Chuẩn hóa nhiều kiểu biểu diễn quyền admin từ các nguồn cũ/mới khác nhau.
    private static bool ParseAdminValue(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = rawValue.Trim().ToLowerInvariant();
        return normalized switch
        {
            "1" => true,
            "true" => true,
            "t" => true,
            "yes" => true,
            "y" => true,
            "admin" => true,
            "administrator" => true,
            _ => int.TryParse(normalized, out var numericValue) && numericValue > 0
        };
    }

    // Cắt khoảng trắng và trả null nếu chuỗi rỗng để việc so sánh/gom nhóm ổn định hơn.
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Kiểm tra payload văn bản có dữ liệu thật hay chỉ là null/rỗng.
    private static bool HasTextPayload(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    // Các record nội bộ phía dưới chỉ dùng làm DTO tạm trong service,
    // giúp từng bước xử lý rõ nghĩa mà không phải mang theo entity đầy đủ.
    private sealed record EmployeeRefreshSnapshot(
        Guid Id,
        string EmployeeCode,
        string FullName,
        int Status);

    private sealed record ProfileSyncResult(
        int ProfilesInserted,
        int ProfilesUpdated,
        int ProfilesDeleted);

    private sealed record BiometricSummarySnapshot(
        string? CardNumber,
        string? Password,
        bool IsAdmin,
        int FpQty,
        bool HasFaceData);

    private sealed record BiometricSummaryLoadResult(
        Dictionary<Guid, BiometricSummarySnapshot> Summaries,
        string FingerprintSource,
        string FaceSource);

    private sealed record FingerprintLoadResult(
        Dictionary<Guid, int> Counts,
        string Source);

    private sealed record FaceLoadResult(
        HashSet<Guid> EmployeeIds,
        string Source);

    private sealed record ProfileSummarySnapshot(
        string? CardNumber,
        string? Password,
        bool IsAdmin);

    private sealed record ProfileSummaryProjection(
        Guid EmployeeId,
        string? CardNumber,
        string? Password,
        string? PrivilegeValue,
        DateTime? UpdatedAt,
        DateTime? CreatedAt,
        string? RowId);

    private sealed record FingerprintCountProjection(
        Guid EmployeeId,
        string Fid,
        DateTime? UpdatedAt,
        DateTime? CreatedAt);

    private sealed record BioDataFingerprintProjection(
        Guid EmployeeId,
        string? BioType,
        string? BioNo,
        string? BioIndex);

    private sealed record LegacyPhotoTableMetadata(
        string TableName,
        string ContentColumn,
        string? RowIdColumn,
        string? EmployeeIdColumn,
        string? EmployeeCodeColumn,
        string? UserIdColumn,
        string? UpdatedAtColumn,
        string? CreatedAtColumn);

    private sealed record LegacyUserInfoMetadata(
        string TableName,
        string EmployeeCodeColumn,
        string UserIdColumn);

    private sealed record PhotoSyncSnapshot(
        Guid EmployeeId,
        string EmployeeCode,
        string? RowId,
        string? UserId,
        string? Content,
        DateTime? UpdatedAt,
        DateTime? CreatedAt);
}
