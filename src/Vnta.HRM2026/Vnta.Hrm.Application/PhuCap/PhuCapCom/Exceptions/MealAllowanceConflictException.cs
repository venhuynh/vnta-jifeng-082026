namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Exceptions;

public sealed class MealAllowanceConflictException(string message) : InvalidOperationException(message);
