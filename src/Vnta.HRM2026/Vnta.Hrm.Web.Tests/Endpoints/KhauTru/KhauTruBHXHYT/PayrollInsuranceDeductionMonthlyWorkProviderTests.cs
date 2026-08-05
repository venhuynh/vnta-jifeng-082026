using Vnta.Hrm.Application.ChamCong.BangCongThang;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruBHXHYT;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollInsuranceDeductionMonthlyWorkProviderTests
{
    [Fact]
    public async Task Load_employee_monthly_work_requires_payroll_access_and_derives_the_full_payroll_month()
    {
        var insuranceRecordId = Guid.NewGuid();
        var summaryRecordId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var payrollService = new FakePayrollInsuranceDeductionService(
        [
            CreateInsuranceRecord(insuranceRecordId, summaryRecordId, employeeId, 7, 2026)
        ]);
        var attendanceService = new FakeMonthlyWorkService(employeeId);
        var authorizer = new CapturingPayrollAuthorizer();
        var provider = CreateProvider(payrollService, attendanceService, authorizer);

        var result = await provider.LoadEmployeeMonthlyWorkAsync(
            insuranceRecordId,
            summaryRecordId,
            employeeId,
            2026,
            7);

        Assert.True(authorizer.WasDemanded);
        Assert.NotNull(result);
        Assert.Equal(employeeId, result.Id);
        Assert.Equal(new DateOnly(2026, 7, 1), attendanceService.ReceivedFilter!.FromDate);
        Assert.Equal(new DateOnly(2026, 7, 31), attendanceService.ReceivedFilter.ToDate);
        Assert.Equal(employeeId, attendanceService.ReceivedFilter.EmployeeId);
        Assert.Equal(1, attendanceService.ReceivedFilter.Take);
    }

    [Fact]
    public async Task Load_employee_monthly_work_does_not_query_attendance_when_the_payroll_row_is_out_of_scope()
    {
        var payrollService = new FakePayrollInsuranceDeductionService([]);
        var attendanceService = new FakeMonthlyWorkService(Guid.NewGuid());
        var authorizer = new CapturingPayrollAuthorizer();
        var provider = CreateProvider(payrollService, attendanceService, authorizer);

        var result = await provider.LoadEmployeeMonthlyWorkAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            7);

        Assert.True(authorizer.WasDemanded);
        Assert.Null(result);
        Assert.Null(attendanceService.ReceivedFilter);
    }

    private static PayrollInsuranceDeductionDataProvider CreateProvider(
        FakePayrollInsuranceDeductionService payrollService,
        IAttendanceMonthlyWorkSummaryGridReadService attendanceService,
        IPayrollAdministrationAuthorizer authorizer) =>
        new(
            payrollService,
            payrollService,
            payrollService,
            payrollService,
            payrollService,
            null!,
            authorizer,
            new MonthlyWorkSummaryDataProvider(attendanceService));

    private static PayrollInsuranceDeductionListItemDto CreateInsuranceRecord(
        Guid insuranceRecordId,
        Guid summaryRecordId,
        Guid employeeId,
        int payrollMonth,
        int payrollYear) =>
        new(
            insuranceRecordId,
            summaryRecordId,
            employeeId,
            "NV001",
            "Nhân viên kiểm thử",
            "Phòng kiểm thử",
            "Kiểm thử viên",
            payrollMonth,
            payrollYear,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            true,
            0,
            null,
            false,
            DateTime.UnixEpoch,
            null);

    private sealed class CapturingPayrollAuthorizer : IPayrollAdministrationAuthorizer
    {
        public bool WasDemanded { get; private set; }

        public Task DemandAsync(CancellationToken cancellationToken = default)
        {
            WasDemanded = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMonthlyWorkService(Guid employeeId)
        : IAttendanceMonthlyWorkSummaryGridReadService
    {
        public AttendanceMonthlyWorkSummaryGridFilter? ReceivedFilter { get; private set; }

        public Task<AttendanceMonthlyWorkSummaryGridPageDto> SearchAsync(
            AttendanceMonthlyWorkSummaryGridFilter filter,
            CancellationToken cancellationToken = default)
        {
            ReceivedFilter = filter;
            var dayCell = new AttendanceMonthlyWorkSummaryDayCellDto(
                Guid.NewGuid(),
                filter.FromDate,
                "Ngày thường",
                null,
                "HC",
                "Hành chính",
                "#336699",
                "08:00",
                "17:00",
                0,
                0,
                "FULL_WORK",
                false,
                0,
                0,
                0,
                0,
                DateTime.UnixEpoch,
                DateTime.UnixEpoch,
                null);
            var row = new AttendanceMonthlyWorkSummaryGridRowDto(
                employeeId,
                1,
                "NV001",
                "Nhân viên kiểm thử",
                "Phòng kiểm thử",
                "Kiểm thử viên",
                [dayCell]);

            return Task.FromResult(new AttendanceMonthlyWorkSummaryGridPageDto([row], 1));
        }
    }

    private sealed class FakePayrollInsuranceDeductionService(
        IReadOnlyList<PayrollInsuranceDeductionListItemDto> rows)
        : IPayrollInsuranceDeductionReadService,
          IPayrollInsuranceDeductionRefreshService,
          IPayrollInsuranceDeductionPreviousMonthSyncService,
          IPayrollInsuranceDeductionManualAdjustmentService,
          IPayrollInsuranceDeductionLockService,
          IPayrollInsuranceDeductionLegacyWriteService
    {
        public Task<PayrollInsuranceDeductionPageDto> SearchAsync(
            PayrollInsuranceDeductionFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PayrollInsuranceDeductionPageDto(rows, rows.Count));

        public Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
            RefreshPayrollInsuranceDeductionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(
            SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(
            UpdatePayrollInsuranceDeductionManualValuesRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(
            SetPayrollInsuranceDeductionLockStateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
            SetPayrollInsuranceDeductionBatchLockStateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> ValidateAsync(
            UpsertPayrollInsuranceDeductionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollInsuranceDeductionListItemDto> SaveAsync(
            UpsertPayrollInsuranceDeductionRequest request,
            bool isNew,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
