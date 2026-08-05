namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;

/// <summary>Quản lý phiên bản yêu cầu và token hủy của luồng tải dữ liệu.</summary>
internal sealed class EmployeeAssignmentReloadLifecycleState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }
    public int RequestedVersion;
    public int ProcessedVersion;
}
