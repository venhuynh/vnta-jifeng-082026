using DevExpress.Blazor.Localization;
using System.Resources;

namespace Vnta.Hrm.Web.Client.Services.Ui;

/// <summary>
/// Standardizes DevExpress UI strings in Vietnamese for the controls used by HRM.
/// The v26.1 Vietnamese satellite resources will extend this map with every remaining ID.
/// </summary>
public sealed class VietnameseDxLocalizationService : DxLocalizationService
{
    private static readonly ResourceManager DevExpressResources = new(
        "Vnta.Hrm.Web.Client.Resources.DevExpress.LocalizationRes",
        typeof(VietnameseDxLocalizationService).Assembly);

    private static readonly IReadOnlyDictionary<string, string> Translations = new Dictionary<string, string>
    {
        [ToKey(DxBlazorStringId.Yes)] = "Có",
        [ToKey(DxBlazorStringId.No)] = "Không",
        [ToKey(DxBlazorStringId.Loading_Panel_Text)] = "Đang tải...",
        [ToKey(DxBlazorStringId.SearchBox_NullText)] = "Tìm kiếm...",
        [ToKey(DxBlazorStringId.Grid_Loading)] = "Đang tải...",
        [ToKey(DxBlazorStringId.Grid_EmptyDataRow)] = "Không có dữ liệu để hiển thị.",
        [ToKey(DxBlazorStringId.Grid_PageSizeSelector_Caption)] = "Số dòng mỗi trang:",
        [ToKey(DxBlazorStringId.Grid_PageSizeSelector_AllRowsItem)] = "Tất cả",
        [ToKey(DxBlazorStringId.Grid_Pager_Summary_PageLabel)] = "Trang",
        [ToKey(DxBlazorStringId.Pager_PageOfLabel)] = "trên",
        [ToKey(DxBlazorStringId.Grid_Pager_Summary_ItemFormat)] = "({0} bản ghi)",
        [ToKey(DxBlazorStringId.Grid_Pager_Summary_ItemsFormat)] = "({0} bản ghi)",
        [ToKey(DxBlazorStringId.A11y_Pager_BottomNavigation)] = "Phân trang phía dưới",
        [ToKey(DxBlazorStringId.A11y_Pager_TopNavigation)] = "Phân trang phía trên",
        [ToKey(DxBlazorStringId.A11y_Pager_CurrentPage)] = "Trang {0} trên {1}",
        [ToKey(DxBlazorStringId.A11y_Pager_FirstPage)] = "Trang đầu",
        [ToKey(DxBlazorStringId.A11y_Pager_LastPage)] = "Trang cuối",
        [ToKey(DxBlazorStringId.A11y_Pager_NavigateToPage)] = "Chuyển đến trang {0}",
        [ToKey(DxBlazorStringId.A11y_Pager_NextPage)] = "Trang sau",
        [ToKey(DxBlazorStringId.A11y_Pager_PreviousPage)] = "Trang trước",
        ["DxBlazorStringId.Grid_SearchBoxNullText"] = "Tìm kiếm...",
        ["DxBlazorStringId.Grid_ClearFilterButton"] = "Xóa bộ lọc",
        ["DxBlazorStringId.Grid_FilterMenu_ApplyButton"] = "Áp dụng",
        ["DxBlazorStringId.Grid_FilterMenu_CancelButton"] = "Hủy",
        ["DxBlazorStringId.Grid_FilterMenu_ClearButton"] = "Xóa",
        ["DxBlazorStringId.Grid_FilterMenu_SearchBoxNullText"] = "Tìm kiếm...",
        ["DxBlazorStringId.Grid_FilterMenu_SelectAll"] = "Chọn tất cả",
        ["DxBlazorStringId.Grid_FilterMenu_BlanksItem"] = "(Trống)",
        ["DxBlazorStringId.Grid_FilterMenu_ValuesHeaderText"] = "Giá trị",
        ["DxBlazorStringId.Grid_GroupPanel"] = "Kéo tiêu đề cột vào đây để nhóm dữ liệu",
        ["DxBlazorStringId.Grid_Editing_NewButton"] = "Thêm mới",
        ["DxBlazorStringId.Grid_Editing_EditButton"] = "Sửa",
        ["DxBlazorStringId.Grid_Editing_DeleteButton"] = "Xóa",
        ["DxBlazorStringId.Grid_Editing_SaveButton"] = "Lưu",
        ["DxBlazorStringId.Grid_Editing_CancelButton"] = "Hủy",
        ["DxBlazorStringId.Grid_Editing_DeleteConfirmationText"] = "Bạn có chắc chắn muốn xóa bản ghi này?",
        ["DxBlazorStringId.Grid_Editing_PopupEditFormHeaderText"] = "Chỉnh sửa dữ liệu",
        ["DxBlazorStringId.Grid_CommandItem_SelectAll"] = "Chọn tất cả",
        ["DxBlazorStringId.Grid_CommandItem_DeselectAll"] = "Bỏ chọn tất cả",
        ["DxBlazorStringId.Grid_Summary_Sum"] = "Tổng",
        ["DxBlazorStringId.Grid_Summary_Average"] = "Trung bình",
        ["DxBlazorStringId.Grid_Summary_Count"] = "Số lượng",
        ["DxBlazorStringId.Grid_Summary_Min"] = "Nhỏ nhất",
        ["DxBlazorStringId.Grid_Summary_Max"] = "Lớn nhất",
        ["DxBlazorStringId.FilterBuilder_Add_Condition"] = "Thêm điều kiện",
        ["DxBlazorStringId.FilterBuilder_Add_Group"] = "Thêm nhóm",
        ["DxBlazorStringId.FilterBuilder_Group_And"] = "Và",
        ["DxBlazorStringId.FilterBuilder_Group_Or"] = "Hoặc",
        ["DxBlazorStringId.FilterBuilder_ClauseType_Contains"] = "Chứa",
        ["DxBlazorStringId.FilterBuilder_ClauseType_DoesNotContain"] = "Không chứa",
        ["DxBlazorStringId.FilterBuilder_ClauseType_Equals"] = "Bằng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_DoesNotEqual"] = "Không bằng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_BeginsWith"] = "Bắt đầu bằng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_EndsWith"] = "Kết thúc bằng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_IsNull"] = "Là rỗng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_IsNotNull"] = "Không rỗng",
        ["DxBlazorStringId.FilterBuilder_ClauseType_IsToday"] = "Hôm nay",
        ["DxBlazorStringId.FilterBuilder_ClauseType_IsYesterday"] = "Hôm qua",
        ["DxBlazorStringId.FilterBuilder_ClauseType_IsTomorrow"] = "Ngày mai"
    };

    protected override string GetString(string key) =>
        Translations.TryGetValue(key, out var translation)
            ? translation
            : DevExpressResources.GetString(key) ?? base.GetString(key);

    private static string ToKey(DxBlazorStringId stringId) =>
        $"{nameof(DxBlazorStringId)}.{stringId}";
}
