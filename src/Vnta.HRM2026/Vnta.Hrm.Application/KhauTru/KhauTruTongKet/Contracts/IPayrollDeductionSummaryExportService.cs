namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

/// <summary>Exports the server-authorized deduction-summary snapshot.</summary>
public interface IPayrollDeductionSummaryExportService
{
    Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        PayrollDeductionSummaryExportFormat format,
        CancellationToken cancellationToken = default);
}
