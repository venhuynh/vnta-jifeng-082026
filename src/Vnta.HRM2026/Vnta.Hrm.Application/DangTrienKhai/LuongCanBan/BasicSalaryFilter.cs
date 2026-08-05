namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

public sealed record BasicSalaryFilter(
    string? SearchText,
    int Take = 2000);
