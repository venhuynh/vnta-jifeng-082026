using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Đại diện kiểu <c>PhuCapComStatus</c> phục vụ màn hình phụ cấp cơm.</summary>
public partial class PhuCapComStatus
{
    [Parameter] public bool IsLocked { get; set; }
}
