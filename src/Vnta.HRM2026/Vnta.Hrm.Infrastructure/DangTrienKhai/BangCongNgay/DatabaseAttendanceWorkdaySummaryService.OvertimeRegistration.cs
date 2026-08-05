namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed partial class DatabaseAttendanceWorkdaySummaryService
{
    private void EnsureDailyOvertimeRegistrationCheckIsSatisfied()
    {
        if(!workdaySummaryOptions.EnableDailyOvertimeRegistrationCheck)
        {
            return;
        }

        throw new InvalidOperationException(
            "Đã bật AttendanceWorkdaySummary:EnableDailyOvertimeRegistrationCheck nhưng nguồn đăng ký tăng ca hằng ngày chưa được tích hợp. Hãy hoàn thành bảng đăng ký tăng ca hằng ngày và nối source backend trước khi bật cờ này.");
    }
}
