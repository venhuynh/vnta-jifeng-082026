namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;

/// <summary>Quản lý lựa chọn của lưới độc lập với bộ lọc và trạng thái tải.</summary>
internal sealed class EmployeeAssignmentSelectionState
{
    public IReadOnlyList<object> Items { get; set; } = [];

    public void Clear() => Items = [];
}
