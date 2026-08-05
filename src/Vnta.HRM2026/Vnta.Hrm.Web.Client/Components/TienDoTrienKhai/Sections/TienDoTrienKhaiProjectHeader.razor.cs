using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Sections;

/// <summary>Trình bày tiêu đề và các chỉ số tổng quan của lộ trình triển khai.</summary>
public partial class TienDoTrienKhaiProjectHeader
{
    [Parameter, EditorRequired] public int PhaseCount { get; set; }

    [Parameter, EditorRequired] public int TotalDurationWeeks { get; set; }
}
