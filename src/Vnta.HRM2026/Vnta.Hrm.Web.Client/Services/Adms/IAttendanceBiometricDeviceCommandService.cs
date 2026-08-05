using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;

namespace Vnta.Hrm.Web.Client.Services.Adms;

public interface IAttendanceBiometricDeviceCommandService
{
    Task<AttendanceBiometricDeviceCommandCreateResult> CreatePullCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default);

    Task<AttendanceBiometricDeviceCommandCreateResult> CreatePushCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default);

    Task<AttendanceBiometricDeviceCommandCreateResult> CreateDeleteCommandsAsync(
        IReadOnlyList<AttendanceBiometricDataRecord> employees,
        IReadOnlyList<AttendanceDeviceRecord> devices,
        CancellationToken cancellationToken = default);
}
