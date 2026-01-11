namespace AI_Bible_App.Maui.Services;

/// <summary>
/// MAUI implementation of IDialogService using Shell.Current.
/// Handles null checks internally so ViewModels don't need to.
/// </summary>
public class DialogService : IDialogService
{
    private Page? CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page 
        ?? Shell.Current?.CurrentPage;

    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = CurrentPage;
        if (page != null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var page = CurrentPage;
        if (page != null)
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    public async Task<string?> ShowActionSheetAsync(string title, string? cancel, string? destruction, params string[] buttons)
    {
        var page = CurrentPage;
        if (page != null)
        {
            return await page.DisplayActionSheet(title, cancel, destruction, buttons);
        }
        return null;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string? initialValue = null, int maxLength = -1, string accept = "OK", string cancel = "Cancel", Keyboard? keyboard = null)
    {
        var page = CurrentPage;
        if (page != null)
        {
            return await page.DisplayPromptAsync(title, message, accept, cancel, initialValue: initialValue, maxLength: maxLength, keyboard: keyboard ?? Keyboard.Default);
        }
        return null;
    }
}
