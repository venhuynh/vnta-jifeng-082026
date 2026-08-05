using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Dialogs;

public sealed class KhauTruThueTNCNEditModel
{
    public Guid PayrollDeductionSummaryRecordId { get; set; }
    public string EmployeeDisplay { get; set; } = string.Empty;
    public string PayrollPeriodDisplay { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Số tiền khấu trừ phải từ 0 đến 9.999.999.999.999.999,99.")]
    public decimal DeductionAmount { get; set; }

    public bool IsLocked { get; set; }
    public DateTime? OriginalUpdatedAtUtc { get; set; }
}
