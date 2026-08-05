using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai;

/// <summary>Xử lý thêm, sửa và đặt lại dữ liệu chỉ trong phiên UI hiện tại.</summary>
public partial class TienDoTrienKhai
{
    private Task OpenCreatePopupAsync()
    {
        if(!CanOperate)
        {
            return Task.CompletedTask;
        }

        EditModel = SessionState.CreateNewEditModel();
        EditContext = new EditContext(EditModel);
        IsEditPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenEditPopupAsync(ProjectImplementationProgressItem item)
    {
        if(!CanOperate)
        {
            return Task.CompletedTask;
        }

        EditModel = ProjectImplementationProgressEditModel.FromItem(item);
        EditContext = new EditContext(EditModel);
        IsEditPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnEditPopupVisibleChanged(bool visible)
    {
        if(!visible)
        {
            CloseEditPopup();
        }

        return Task.CompletedTask;
    }

    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        CloseEditPopupCore();
    }

    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit || !EditContext.Validate())
        {
            return;
        }

        IsSavingEdit = true;
        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            if(SessionState.Save(EditModel))
            {
                CloseEditPopupCore();
            }
        }
        finally
        {
            IsSavingEdit = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task ResetItemsAsync()
    {
        if(IsSavingEdit)
        {
            return Task.CompletedTask;
        }

        SessionState.Reset();
        CloseEditPopupCore();
        return Task.CompletedTask;
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new ProjectImplementationProgressEditModel();
        EditContext = new EditContext(EditModel);
    }
}
