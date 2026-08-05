using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.LenhMayChamCong;

public sealed class LenhMayChamCongEditModel
{
    public int? Id { get; set; }

    public string DeviceSn { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime? CommitTime { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsNew => !Id.HasValue;

    public UpsertAdmsDeviceCommandRequest ToRequest()
    {
        return new(
            DeviceSn,
            Content,
            CommitTime,
            Description);
    }

    public static LenhMayChamCongEditModel Create()
    {
        return new()
        {
            CommitTime = DateTime.Now
        };
    }

    public static LenhMayChamCongEditModel FromDetail(AdmsDeviceCommandDetailDto detail)
    {
        return new()
        {
            Id = detail.Id,
            DeviceSn = detail.DeviceSn,
            Content = detail.Content,
            CommitTime = detail.CommitTime,
            Description = detail.Description
        };
    }

    public static LenhMayChamCongEditModel FromSummary(AdmsDeviceCommandSummaryDto summary)
    {
        return new()
        {
            Id = summary.Id,
            DeviceSn = summary.DeviceSn,
            Content = summary.Content,
            CommitTime = summary.CommitTime,
            Description = summary.Description
        };
    }
}
