using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Màn hình lộ trình triển khai dự án với dữ liệu quản lý trực tiếp trong UI.</summary>
public partial class TienDoTrienKhai
{
    [Inject] private IProjectImplementationProgressStore ProgressStore { get; set; } = default!;

    private ProjectImplementationProgressSessionState SessionState { get; } = new();

    private string? LoadError { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            SessionState.Apply(await ProgressStore.LoadAsync());
        }
        catch
        {
            LoadError = "Không thể tải dữ liệu tiến độ đã lưu. Hệ thống đang hiển thị dữ liệu mặc định.";
        }
    }
}
