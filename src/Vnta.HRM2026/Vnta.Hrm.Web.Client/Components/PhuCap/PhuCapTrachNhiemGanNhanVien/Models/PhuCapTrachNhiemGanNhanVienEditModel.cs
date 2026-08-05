using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Models;

public sealed class PhuCapTrachNhiemGanNhanVienEditModel
{
    public Guid? GradeId { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1.000 ký tự.")]
    public string? Note { get; set; }

    public static PhuCapTrachNhiemGanNhanVienEditModel From(PayrollResponsibilityAllowanceEmployeeAssignmentDto source) => new()
    {
        GradeId = source.GradeId,
        Note = source.Note
    };
}
