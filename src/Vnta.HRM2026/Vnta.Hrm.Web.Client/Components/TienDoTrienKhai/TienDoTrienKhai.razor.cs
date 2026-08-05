using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Màn hình lộ trình triển khai dự án với dữ liệu quản lý trực tiếp trong UI.</summary>
public partial class TienDoTrienKhai
{
    private ProjectImplementationProgressSessionState SessionState { get; } = new();
}
