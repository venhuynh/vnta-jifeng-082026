using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private static string GetGradeLabel(PayrollResponsibilityAllowanceEmployeeAssignmentDto record) =>
        string.IsNullOrWhiteSpace(record.GradeCode) ? record.GradeName : $"{record.GradeCode} - {record.GradeName}";

    private string GetGradeLabelCssClass(Guid? gradeId)
    {
        var grade = gradeId is { } id ? Grades.FirstOrDefault(item => item.Id == id) : null;
        return string.Join(' ', "responsibility-grade", grade?.IsActive == true ? "responsibility-grade-active" : "responsibility-grade-inactive");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : $"{value.ToString("N0", DisplayCulture)} đ";

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if (normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            normalizedMonth = MinimumSupportedMonth;
        }

        return (normalizedMonth, normalizedYear);
    }

    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }
}
