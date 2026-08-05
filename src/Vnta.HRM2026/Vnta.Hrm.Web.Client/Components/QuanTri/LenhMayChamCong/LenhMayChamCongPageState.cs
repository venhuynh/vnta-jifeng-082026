using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.LenhMayChamCong;

public sealed class LenhMayChamCongPageState
{
    public string? DeviceSn { get; set; }

    public string? Status { get; set; }

    public string? SearchTerm { get; set; }

    public int? SelectedCommandId { get; set; }

    public DateTime? CommitFrom { get; set; }

    public DateTime? CommitTo { get; set; }

    public AdmsDeviceCommandFilter ToFilter()
    {
        return new(
            Normalize(DeviceSn),
            Normalize(Status),
            Normalize(SearchTerm),
            CommitFrom,
            CommitTo);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
