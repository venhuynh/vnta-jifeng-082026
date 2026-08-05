using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;

namespace Vnta.Hrm.Web.Client.Services.Adms;

public sealed class AttendanceBiometricDeviceCommandService(
    IAdmsDeviceCommandService deviceCommandService,
    IAttendanceBiometricDeviceQueueService attendanceBiometricDeviceQueueService)
    : IAttendanceBiometricDeviceCommandService
{
    private const int PullCommandBatchSize = 8;

    public async Task<AttendanceBiometricDeviceCommandCreateResult> CreatePullCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmployeeCodes = employees
            .Select(static employee => NormalizeEmployeeCode(employee.EmployeeCode))
            .Where(static employeeCode => !string.IsNullOrWhiteSpace(employeeCode))
            .Select(static employeeCode => employeeCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedEmployeeCodes.Length == 0)
        {
            throw new InvalidOperationException("Các nhân viên được chọn chưa có mã chấm công hợp lệ để tạo lệnh tải.");
        }

        var normalizedDeviceSerials = devices
            .Select(static device => NormalizeDeviceSerial(device.SerialNumber))
            .Where(static serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .Select(static serialNumber => serialNumber!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedDeviceSerials.Length == 0)
        {
            throw new InvalidOperationException("Chưa có máy chấm công hợp lệ để tạo lệnh tải.");
        }

        var commitTime = DateTime.Now;
        var requests = BuildPullCommandRequests(normalizedEmployeeCodes, normalizedDeviceSerials, commitTime);

        foreach (var batch in requests.Chunk(PullCommandBatchSize))
        {
            foreach (var request in batch)
            {
                await deviceCommandService.CreateAsync(request, cancellationToken);
            }
        }

        return new AttendanceBiometricDeviceCommandCreateResult(
            requests.Count,
            normalizedEmployeeCodes.Length,
            normalizedDeviceSerials.Length);
    }

    public async Task<AttendanceBiometricDeviceCommandCreateResult> CreatePushCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default)
    {
        var employeeIds = employees
            .Select(static employee => employee.EmployeeId)
            .Where(static employeeId => employeeId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (employeeIds.Length == 0)
        {
            throw new InvalidOperationException("Các nhân viên được chọn chưa có định danh hợp lệ để tạo lệnh cập nhật.");
        }

        var normalizedDeviceSerials = devices
            .Select(static device => NormalizeDeviceSerial(device.SerialNumber))
            .Where(static serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .Select(static serialNumber => serialNumber!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedDeviceSerials.Length == 0)
        {
            throw new InvalidOperationException("Chưa có máy chấm công hợp lệ để tạo lệnh cập nhật.");
        }

        var result = await attendanceBiometricDeviceQueueService.CreatePushCommandsAsync(
            new AttendanceBiometricDeviceCommandBatchRequest(employeeIds, normalizedDeviceSerials),
            cancellationToken);

        return new AttendanceBiometricDeviceCommandCreateResult(
            result.CommandsCreated,
            result.MatchedEmployees,
            result.DeviceSerialNumbers.Count);
    }

    public async Task<AttendanceBiometricDeviceCommandCreateResult> CreateDeleteCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default)
    {
        var employeeIds = employees
            .Select(static employee => employee.EmployeeId)
            .Where(static employeeId => employeeId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (employeeIds.Length == 0)
        {
            throw new InvalidOperationException("Các nhân viên được chọn chưa có định danh hợp lệ để tạo lệnh xóa.");
        }

        var normalizedDeviceSerials = devices
            .Select(static device => NormalizeDeviceSerial(device.SerialNumber))
            .Where(static serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .Select(static serialNumber => serialNumber!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedDeviceSerials.Length == 0)
        {
            throw new InvalidOperationException("Chưa có máy chấm công hợp lệ để tạo lệnh xóa.");
        }

        var result = await attendanceBiometricDeviceQueueService.CreateDeleteCommandsAsync(
            new AttendanceBiometricDeviceCommandBatchRequest(employeeIds, normalizedDeviceSerials),
            cancellationToken);

        return new AttendanceBiometricDeviceCommandCreateResult(
            result.CommandsCreated,
            result.MatchedEmployees,
            result.DeviceSerialNumbers.Count);
    }

    private static List<UpsertAdmsDeviceCommandRequest> BuildPullCommandRequests(
        IReadOnlyList<string> employeeCodes,
        IReadOnlyList<string> deviceSerialNumbers,
        DateTime commitTime)
    {
        var requests = new List<UpsertAdmsDeviceCommandRequest>();

        foreach (var deviceSerialNumber in deviceSerialNumbers)
        {
            foreach (var employeeCode in employeeCodes)
            {
                requests.Add(new UpsertAdmsDeviceCommandRequest(
                    deviceSerialNumber,
                    $"DATA QUERY USERINFO PIN={employeeCode}",
                    commitTime,
                    "Query User Info"));

                for (var fid = 0; fid < 10; fid += 1)
                {
                    requests.Add(new UpsertAdmsDeviceCommandRequest(
                        deviceSerialNumber,
                        $"DATA QUERY FINGERTMP PIN={employeeCode}\tFID={fid}",
                        commitTime,
                        $"Query Fingerprint Pin={employeeCode} Fid={fid}"));
                }

                for (var type = 0; type < 10; type += 1)
                {
                    foreach (var content in BuildQueryBioDataCommandContents(employeeCode, type))
                    {
                        requests.Add(new UpsertAdmsDeviceCommandRequest(
                            deviceSerialNumber,
                            content,
                            commitTime,
                            $"Query Biodata Pin={employeeCode} Type={type}"));
                    }
                }
            }
        }

        return requests;
    }

    private static IEnumerable<string> BuildQueryBioDataCommandContents(string employeeCode, int type)
    {
        if (type is 1 or 7)
        {
            for (var no = 0; no < 10; no += 1)
            {
                yield return $"DATA QUERY BIODATA Pin={employeeCode}\tType={type}\tNo={no}";
            }

            yield break;
        }

        if (type == 2)
        {
            yield return $"DATA QUERY BIODATA Pin={employeeCode}\tType={type}\tNo=0";
            yield break;
        }

        if (type is 4 or 8)
        {
            for (var no = 0; no < 2; no += 1)
            {
                yield return $"DATA QUERY BIODATA Pin={employeeCode}\tType={type}\tNo={no}";
            }

            yield break;
        }

        yield return $"DATA QUERY BIODATA Pin={employeeCode}\tType={type}";
    }

    private static string? NormalizeEmployeeCode(string? employeeCode)
    {
        return string.IsNullOrWhiteSpace(employeeCode)
            ? null
            : employeeCode.Trim();
    }

    private static string? NormalizeDeviceSerial(string? serialNumber)
    {
        return string.IsNullOrWhiteSpace(serialNumber)
            ? null
            : serialNumber.Trim().ToUpperInvariant();
    }
}
