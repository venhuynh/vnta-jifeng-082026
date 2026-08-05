# Repository workflow

- Khi hoàn tất một công việc được người dùng yêu cầu, phải tạo một Git commit chứa đúng các thay đổi thuộc công việc đó.
- Không đưa các thay đổi có sẵn của người dùng hoặc thay đổi ngoài phạm vi vào commit.
- Trước khi commit, chạy kiểm tra/build/test phù hợp và ghi nhận kết quả trong báo cáo bàn giao.
- Không tự push hoặc tạo pull request nếu người dùng chưa yêu cầu.

## Hoàn tất đầu việc

Sau mỗi đầu việc có thay đổi mã nguồn hoặc cấu hình:

1. Chạy build hoặc bộ kiểm tra phù hợp với phạm vi thay đổi trước khi báo hoàn tất.
2. Chỉ khi kiểm tra thành công, tạo một commit Git độc lập với thông điệp mô tả rõ đầu việc vừa hoàn thành.
3. Không gộp các thay đổi không liên quan vào commit; nếu workspace đã có thay đổi ngoài phạm vi, giữ nguyên và báo lại.

Với thay đổi thuộc `src/Vnta.HRM2026/Vnta.Hrm.Web.Client`, lệnh kiểm tra mặc định là:

```powershell
dotnet build src\Vnta.HRM2026\Vnta.Hrm.Web.Client\Vnta.Hrm.Web.Client.csproj --no-restore
```
