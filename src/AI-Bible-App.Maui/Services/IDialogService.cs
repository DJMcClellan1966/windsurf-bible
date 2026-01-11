namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Service for displaying dialogs, alerts, and action sheets.
/// Abstracts away Shell.Current.CurrentPage calls for cleaner ViewModels.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an alert dialog with a single OK button
    /// </summary>
    Task ShowAlertAsync(string title, string message, string cancel = "OK");

    /// <summary>
    /// Shows a confirmation dialog with accept/cancel buttons
    /// </summary>
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");

    /// <summary>
    /// Shows an action sheet with multiple options
    /// </summary>
    Task<string?> ShowActionSheetAsync(string title, string? cancel, string? destruction, params string[] buttons);

    /// <summary>
    /// Shows a prompt dialog for text input
    /// </summary>
    Task<string?> ShowPromptAsync(string title, string message, string? initialValue = null, int maxLength = -1, string accept = "OK", string cancel = "Cancel", Keyboard? keyboard = null);
}
