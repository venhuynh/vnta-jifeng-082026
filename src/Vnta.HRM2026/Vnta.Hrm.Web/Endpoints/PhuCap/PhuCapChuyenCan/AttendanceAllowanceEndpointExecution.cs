using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;

/// <summary>
/// Maps attendance-allowance command failures to the feature's HTTP contract.
/// </summary>
internal static class AttendanceAllowanceEndpointExecution
{
    public static IResult MapCommandException(AttendanceAllowanceCommandException exception) =>
        exception.Failure switch
        {
            AttendanceAllowanceCommandFailure.NotFound => Results.NotFound(new { message = exception.Message }),
            AttendanceAllowanceCommandFailure.Locked or AttendanceAllowanceCommandFailure.Concurrency => Results.Conflict(new { message = exception.Message }),
            _ => Results.BadRequest(new { message = exception.Message })
        };
}
