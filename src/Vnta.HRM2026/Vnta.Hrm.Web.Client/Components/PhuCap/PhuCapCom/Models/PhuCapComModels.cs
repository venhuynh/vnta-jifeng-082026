namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Đại diện kiểu <c>MonthOption</c> phục vụ màn hình phụ cấp cơm.</summary>
public sealed record MonthOption(int Value, string Text);

/// <summary>Giá trị lựa chọn kích thước trang hiển thị trên lưới phụ cấp cơm.</summary>
public sealed record PageSizeOption(int Value, string Text);

/// <summary>Thực hiện xử lý cho luồng <c>MealAllowanceReloadSnapshot</c>.</summary>
internal readonly record struct MealAllowanceReloadSnapshot(
    int PayrollMonth,
    int PayrollYear,
    string? SearchText,
    int PageIndex,
    int PageSize);

/// <summary>Mô hình dữ liệu phục vụ điều chỉnh thủ công phụ cấp cơm.</summary>
public sealed class PhuCapComEditModel
{
    public Guid Id { get; set; }

    public string EmployeeDisplay { get; set; } = string.Empty;

    public int QualifiedMealDays { get; set; }

    public int Overtime1900Days { get; set; }

    public decimal MealAllowancePerQualifiedDay { get; set; }

    public decimal MealAllowanceAmount { get; private set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? OriginalUpdatedAtUtc { get; set; }

    public void RecalculateAmount() => MealAllowanceAmount = MealAllowancePolicy.CalculateAllowanceAmount(
        new MealAllowanceAmountInput(QualifiedMealDays, MealAllowancePerQualifiedDay));
}
