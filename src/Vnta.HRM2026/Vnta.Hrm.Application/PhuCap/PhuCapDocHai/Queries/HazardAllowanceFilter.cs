namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Điều kiện đọc snapshot Phụ cấp độc hại. Cùng một contract được dùng cho danh sách cũ,
/// phân trang, summary badge và export để các tập kết quả không bị lệch nhau.
/// </summary>
/// <param name="PayrollMonth">Tháng kỳ lương cần đọc, từ 1 đến 12.</param>
/// <param name="PayrollYear">Năm kỳ lương thuộc cửa sổ backend hỗ trợ.</param>
/// <param name="LockState">Bộ lọc khóa tương thích với consumer cũ.</param>
/// <param name="SearchText">Từ khóa tìm theo dữ liệu được phép hiển thị; null nghĩa là không lọc text.</param>
/// <param name="Take">Số dòng tối đa của trang; Infrastructure clamp giá trị này để bảo vệ database.</param>
/// <param name="Skip">Số dòng bỏ qua trước trang hiện tại, dùng cho offset pagination.</param>
/// <param name="IncludeTotalCount">Cho phép caller bỏ Count khi total không cần thiết.</param>
/// <param name="SummaryBucket">Nhóm badge áp tại server, độc lập với bộ lọc khóa cũ.</param>
public sealed record HazardAllowanceFilter(
    int PayrollMonth,
    int PayrollYear,
    HazardAllowanceLockState LockState,
    string? SearchText,
    int Take = 1000,
    int Skip = 0,
    bool IncludeTotalCount = true,
    HazardAllowanceSummaryBucket SummaryBucket = HazardAllowanceSummaryBucket.All);
