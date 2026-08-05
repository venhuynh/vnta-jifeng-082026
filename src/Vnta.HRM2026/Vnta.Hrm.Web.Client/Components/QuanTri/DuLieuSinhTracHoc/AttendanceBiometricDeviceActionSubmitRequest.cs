using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDeviceActionSubmitRequest(
    AttendanceBiometricDeviceActionType ActionType,
    IReadOnlyList<AttendanceBiometricDataRecord> Employees,
    IReadOnlyList<AttendanceDeviceRecord> Devices);
