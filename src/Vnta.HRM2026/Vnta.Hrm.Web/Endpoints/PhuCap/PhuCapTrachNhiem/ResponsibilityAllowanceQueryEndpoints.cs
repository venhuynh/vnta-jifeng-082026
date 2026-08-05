using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;

internal static class ResponsibilityAllowanceQueryEndpoints
{
    internal static Task<IResult> GetGradeConfigAsync(
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationReadService service,
        CancellationToken cancellationToken) =>
        ResponsibilityAllowanceEndpointExecution.QueryAsync(
            token => service.GetGradeConfigAsync(year, month, token), cancellationToken);

    internal static Task<IResult> SearchEmployeeAssignmentsAsync(
        [FromBody] PayrollResponsibilityAllowanceEmployeeAssignmentQuery? query,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService service,
        CancellationToken cancellationToken) =>
        query is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu điều kiện tìm kiếm danh sách gán cấp bậc nhân viên."))
            : ResponsibilityAllowanceEndpointExecution.QueryAsync(
                token => service.SearchEmployeeAssignmentsAsync(query, token), cancellationToken);

    internal static Task<IResult> GetMonthlyAbcAsync(
        int year,
        int month,
        bool? isLocked,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcQueryService service,
        CancellationToken cancellationToken) =>
        ResponsibilityAllowanceEndpointExecution.QueryAsync(
            token => service.GetAbcAsync(new PayrollResponsibilityAllowanceAbcFilter(year, month, isLocked), token), cancellationToken);

    internal static Task<IResult> SearchMonthlyAbcAsync(
        [FromBody] PayrollResponsibilityAllowanceAbcQuery? query,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcQueryService service,
        CancellationToken cancellationToken) =>
        query is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Yêu cầu tìm kiếm phụ cấp trách nhiệm không hợp lệ."))
            : ResponsibilityAllowanceEndpointExecution.QueryAsync(
                token => service.SearchAbcAsync(query, token), cancellationToken);

    internal static Task<IResult> GetMonthlyAbcUpdateContextAsync(
        Guid employeeId,
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcQueryService service,
        CancellationToken cancellationToken) =>
        ResponsibilityAllowanceEndpointExecution.QueryAsync(
            token => service.GetUpdateContextAsync(employeeId, year, month, token), cancellationToken);
}
