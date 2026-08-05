using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;

namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;

/// <summary>Single source of truth for command payload validation.</summary>
public sealed class MealAllowanceRequestValidator : IMealAllowanceRequestValidator
{
    public MealAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear) =>
        payrollMonth is >= 1 and <= 12 && payrollYear is >= 2000 and <= 2100
            ? Valid()
            : Invalid("Kỳ lương không hợp lệ.");

    public MealAllowanceValidationResult Validate(RefreshMealAllowanceRequest request) =>
        ValidatePeriod(request.TargetPayrollMonth, request.TargetPayrollYear);

    public MealAllowanceValidationResult Validate(UpdateMealAllowanceManualValuesRequest request)
    {
        if(request.Id == Guid.Empty)
            return Invalid("Dòng phụ cấp cơm cần điều chỉnh không hợp lệ.");
        if(request.QualifiedMealDays < 0)
            return Invalid("Số ngày đủ điều kiện không được âm.");
        return request.Note?.Length > 1000
            ? Invalid("Ghi chú phụ cấp cơm không được vượt quá 1.000 ký tự.")
            : Valid();
    }

    public MealAllowanceValidationResult Validate(SetMealAllowanceLockStateBatchRequest request)
    {
        var periodResult = ValidatePeriod(request.PayrollMonth, request.PayrollYear);
        if(!periodResult.IsValid)
            return periodResult;

        var ids = request.RecordIds;
        var scopeIsValid = request.Scope switch
        {
            MealAllowanceLockActionScope.SelectedRows => ids is { Count: > 0 } && ids.All(id => id != Guid.Empty),
            MealAllowanceLockActionScope.WholePeriod => ids is null || ids.Count == 0,
            _ => false
        };

        return scopeIsValid
            ? Valid()
            : Invalid("Dữ liệu khóa phụ cấp cơm không hợp lệ.");
    }

    private static MealAllowanceValidationResult Valid() => new(null);

    private static MealAllowanceValidationResult Invalid(string message) => new(message);
}
