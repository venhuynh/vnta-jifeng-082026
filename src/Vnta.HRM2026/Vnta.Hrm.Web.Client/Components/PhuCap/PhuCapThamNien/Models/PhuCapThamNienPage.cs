namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Giá trị lựa chọn kích thước trang hiển thị trên lưới.</summary>
public sealed record PageSizeOption(int Value, string Text);

/// <summary>
/// Kết quả trang lưới phụ cấp thâm niên đã được provider map sang view model UI.
/// </summary>
public sealed record PhuCapThamNienPage(
    IReadOnlyList<PhuCapThamNienRecord> Rows,
    int TotalCount,
    decimal TotalAllowanceAmount);
