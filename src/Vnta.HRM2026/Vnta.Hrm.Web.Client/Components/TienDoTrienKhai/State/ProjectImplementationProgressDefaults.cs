using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

/// <summary>Cung cấp dữ liệu khởi tạo cho kho tiến độ triển khai.</summary>
public static class ProjectImplementationProgressDefaults
{
    public static ProjectImplementationProgressSnapshot CreateSnapshot() =>
        ProjectImplementationProgressSessionState.CreateDefaultSnapshot();
}
