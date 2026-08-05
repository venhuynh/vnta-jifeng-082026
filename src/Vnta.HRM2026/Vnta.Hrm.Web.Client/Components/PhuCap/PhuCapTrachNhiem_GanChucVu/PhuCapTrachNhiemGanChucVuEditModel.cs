using System.ComponentModel.DataAnnotations;
namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem_GanChucVu;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemGanChucVuEditModel</c> phục vụ màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
public sealed class PhuCapTrachNhiemGanChucVuEditModel
{
    #region Editor Fields

    [Required(ErrorMessage = "Chức vụ không được để trống.")]
    /// <summary>Giá trị <c>PositionId</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    public Guid? PositionId { get; set; }

    [Required(ErrorMessage = "Cấp bậc không được để trống.")]
    /// <summary>Giá trị <c>GradeId</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    public Guid? GradeId { get; set; }

    /// <summary>Giá trị <c>IsActive</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    public bool IsActive { get; set; } = true;

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Giá trị <c>OriginalUpdatedAtUtc</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    public DateTime? OriginalUpdatedAtUtc { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>Thực hiện xử lý cho luồng <c>CreateDefault</c>.</summary>
    public static PhuCapTrachNhiemGanChucVuEditModel CreateDefault() => new();

    /// <summary>Thực hiện xử lý cho luồng <c>CreateFrom</c>.</summary>
    public static PhuCapTrachNhiemGanChucVuEditModel CreateFrom(PayrollResponsibilityAllowanceGradePositionDto mapping) =>
        new()
        {
            PositionId = mapping.PositionId,
            GradeId = mapping.GradeId,
            IsActive = mapping.IsActive,
            Note = mapping.Note ?? string.Empty,
            OriginalUpdatedAtUtc = mapping.UpdatedAtUtc
        };

    #endregion
}
