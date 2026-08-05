using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

/// <summary>Draft độc lập dùng để thêm hoặc cập nhật một hạng mục triển khai tại UI.</summary>
public sealed class ProjectImplementationProgressEditModel : IValidatableObject
{
    public Guid Id { get; init; }

    public bool IsNew { get; init; }

    [Required(ErrorMessage = "Vui lòng nhập mã hạng mục.")]
    [StringLength(32, ErrorMessage = "Mã hạng mục không được vượt quá 32 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hạng mục triển khai.")]
    [StringLength(250, ErrorMessage = "Hạng mục triển khai không được vượt quá 250 ký tự.")]
    public string WorkItem { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập phân hệ.")]
    [StringLength(120, ErrorMessage = "Phân hệ không được vượt quá 120 ký tự.")]
    public string Module { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập người hoặc nhóm phụ trách.")]
    [StringLength(120, ErrorMessage = "Thông tin phụ trách không được vượt quá 120 ký tự.")]
    public string Owner { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime DueDate { get; set; }

    [Range(0, 100, ErrorMessage = "Tiến độ phải nằm trong khoảng từ 0 đến 100.")]
    public int ProgressPercent { get; set; }

    public ProjectImplementationProgressStatus Status { get; set; } = ProjectImplementationProgressStatus.NotStarted;

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1.000 ký tự.")]
    public string Note { get; set; } = string.Empty;

    public static ProjectImplementationProgressEditModel FromItem(ProjectImplementationProgressItem item) => new()
    {
        Id = item.Id,
        IsNew = false,
        Code = item.Code,
        WorkItem = item.WorkItem,
        Module = item.Module,
        Owner = item.Owner,
        StartDate = item.StartDate,
        DueDate = item.DueDate,
        ProgressPercent = item.ProgressPercent,
        Status = item.Status,
        Note = item.Note
    };

    public ProjectImplementationProgressItem ToItem() => new()
    {
        Id = Id,
        Code = Code.Trim(),
        WorkItem = WorkItem.Trim(),
        Module = Module.Trim(),
        Owner = Owner.Trim(),
        StartDate = StartDate.Date,
        DueDate = DueDate.Date,
        ProgressPercent = ProgressPercent,
        Status = Status,
        Note = Note.Trim()
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(DueDate.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "Hạn hoàn thành phải bằng hoặc sau ngày bắt đầu.",
                [nameof(DueDate), nameof(StartDate)]);
        }
    }
}
