using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Màn hình theo dõi tiến độ triển khai với dữ liệu chỉ tồn tại trong phiên UI.</summary>
public partial class TienDoTrienKhai
{
    private ProjectImplementationProgressSessionState SessionState { get; } = new();

    private TienDoTrienKhaiGrid? GridSection { get; set; }

    private bool IsEditPopupVisible { get; set; }

    private bool IsSavingEdit { get; set; }

    private ProjectImplementationProgressEditModel EditModel { get; set; } = new();

    private EditContext EditContext { get; set; } = new(new ProjectImplementationProgressEditModel());
}
