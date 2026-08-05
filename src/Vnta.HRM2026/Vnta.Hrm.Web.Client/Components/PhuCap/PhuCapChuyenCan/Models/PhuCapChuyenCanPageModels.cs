namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;

/// <summary>Giá trị lựa chọn tháng hiển thị trên thanh công cụ.</summary>
public sealed record MonthOption(int Value, string Text);

/// <summary>Giá trị lựa chọn kích thước trang hiển thị trên lưới.</summary>
public sealed record PageSizeOption(int Value, string Text);

/// <summary>Thông tin huy hiệu tổng hợp hiển thị trên lưới.</summary>
public sealed record AttendanceAllowanceSummaryBadge(string Key, string Label, string ShortLabel, int Count);
