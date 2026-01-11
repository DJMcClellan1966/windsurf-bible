using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

#pragma warning disable MVVMTK0045

namespace AI_Bible_App.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ITrainingDataExporter _exporter;
    private readonly IDialogService _dialogService;
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int totalSessions;

    [ObservableProperty]
    private int totalMessages;

    [ObservableProperty]
    private int ratedMessages;

    [ObservableProperty]
    private int positiveRatings;

    [ObservableProperty]
    private int negativeRatings;

    [ObservableProperty]
    private int messagesWithFeedback;

    // User Profile Properties
    [ObservableProperty]
    private string currentUserName = "Guest";

    [ObservableProperty]
    private string currentUserEmoji = "👤";

    [ObservableProperty]
    private DateTime currentUserSince = DateTime.UtcNow;

    [ObservableProperty]
    private bool isModerationEnabled = true;

    [ObservableProperty]
    private bool hasPinEnabled;

    [ObservableProperty]
    private string selectedTheme = "System";

    [ObservableProperty]
    private string selectedFontSize = "Medium";

    public List<string> ThemeOptions { get; } = new() { "System", "Light", "Dark" };
    public List<string> FontSizeOptions { get; } = new() { "Small", "Medium", "Large", "Extra Large" };

    public SettingsViewModel(ITrainingDataExporter exporter, IDialogService dialogService, IUserService userService, INavigationService navigationService)
    {
        _exporter = exporter;
        _dialogService = dialogService;
        _userService = userService;
        _navigationService = navigationService;
        Title = "Settings";
        
        // Subscribe to user changes
        _userService.CurrentUserChanged += OnCurrentUserChanged;
        LoadUserProfile();
    }

    private void OnCurrentUserChanged(object? sender, Core.Models.AppUser? user)
    {
        LoadUserProfile();
    }

    private void LoadUserProfile()
    {
        var user = _userService.CurrentUser;
        if (user != null)
        {
            CurrentUserName = user.Name;
            CurrentUserEmoji = user.AvatarEmoji ?? "👤";
            CurrentUserSince = user.CreatedAt;
            IsModerationEnabled = user.Settings.EnableContentModeration;
            HasPinEnabled = user.HasPin;
            SelectedTheme = user.Settings.ThemePreference;
            SelectedFontSize = user.Settings.FontSizePreference;
        }
        else
        {
            CurrentUserName = "Guest";
            CurrentUserEmoji = "👤";
            CurrentUserSince = DateTime.UtcNow;
            IsModerationEnabled = true;
            HasPinEnabled = false;
            SelectedTheme = "System";
            SelectedFontSize = "Medium";
        }
    }

    partial void OnIsModerationEnabledChanged(bool value)
    {
        // Save the setting when toggled
        _ = SaveModerationSettingAsync(value);
    }

    partial void OnSelectedThemeChanged(string value)
    {
        // Save and apply theme when changed
        _ = SaveAndApplyThemeAsync(value);
    }

    partial void OnSelectedFontSizeChanged(string value)
    {
        // Save font size preference when changed
        _ = SaveFontSizeAsync(value);
    }

    private async Task SaveAndApplyThemeAsync(string theme)
    {
        try
        {
            await _userService.UpdateCurrentUserAsync(user =>
            {
                user.Settings.ThemePreference = theme;
            });

            // Apply theme immediately
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = theme switch
                    {
                        "Light" => AppTheme.Light,
                        "Dark" => AppTheme.Dark,
                        _ => AppTheme.Unspecified
                    };
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Failed to save theme setting: {ex.Message}");
        }
    }

    private async Task SaveFontSizeAsync(string fontSize)
    {
        try
        {
            await _userService.UpdateCurrentUserAsync(user =>
            {
                user.Settings.FontSizePreference = fontSize;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Failed to save font size setting: {ex.Message}");
        }
    }

    private async Task SaveModerationSettingAsync(bool enabled)
    {
        try
        {
            await _userService.UpdateCurrentUserAsync(user =>
            {
                user.Settings.EnableContentModeration = enabled;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Failed to save moderation setting: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SetOrChangePinAsync()
    {
        var user = _userService.CurrentUser;
        if (user == null) return;

        if (user.HasPin)
        {
            // Verify current PIN first
            var currentPin = await _dialogService.ShowPromptAsync(
                "Verify Current PIN",
                "Enter your current PIN:",
                maxLength: 6,
                keyboard: Keyboard.Numeric);
            
            if (string.IsNullOrEmpty(currentPin)) return;
            
            var isValid = await _userService.VerifyPinAsync(user.Id, currentPin);
            if (!isValid)
            {
                await _dialogService.ShowAlertAsync("Incorrect PIN", "The PIN you entered is incorrect.");
                return;
            }
        }

        // Get new PIN
        var newPin = await _dialogService.ShowPromptAsync(
            "Set New PIN",
            "Enter a 4-6 digit PIN to protect your profile:",
            maxLength: 6,
            keyboard: Keyboard.Numeric);
        
        if (string.IsNullOrEmpty(newPin)) return;
        
        if (newPin.Length < 4)
        {
            await _dialogService.ShowAlertAsync("PIN Too Short", "PIN must be at least 4 digits.");
            return;
        }

        // Confirm PIN
        var confirmPin = await _dialogService.ShowPromptAsync(
            "Confirm PIN",
            "Enter your PIN again to confirm:",
            maxLength: 6,
            keyboard: Keyboard.Numeric);
        
        if (newPin != confirmPin)
        {
            await _dialogService.ShowAlertAsync("PINs Don't Match", "The PINs you entered don't match. Please try again.");
            return;
        }

        try
        {
            await _userService.SetUserPinAsync(user.Id, newPin);
            HasPinEnabled = true;
            await _dialogService.ShowAlertAsync("PIN Set", "Your PIN has been set successfully. You'll need to enter it when switching to your profile.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to set PIN: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RemovePinAsync()
    {
        var user = _userService.CurrentUser;
        if (user == null || !user.HasPin) return;

        // Verify current PIN
        var currentPin = await _dialogService.ShowPromptAsync(
            "Verify PIN",
            "Enter your current PIN to remove protection:",
            maxLength: 6,
            keyboard: Keyboard.Numeric);
        
        if (string.IsNullOrEmpty(currentPin)) return;
        
        var isValid = await _userService.VerifyPinAsync(user.Id, currentPin);
        if (!isValid)
        {
            await _dialogService.ShowAlertAsync("Incorrect PIN", "The PIN you entered is incorrect.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmAsync(
            "Remove PIN",
            "Are you sure you want to remove PIN protection? Anyone will be able to access your profile.",
            "Remove", "Cancel");
        
        if (!confirm) return;

        try
        {
            await _userService.RemovePinAsync(user.Id);
            HasPinEnabled = false;
            await _dialogService.ShowAlertAsync("PIN Removed", "PIN protection has been removed from your profile.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to remove PIN: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SwitchUserAsync()
    {
        await Shell.Current.GoToAsync("//userselection");
    }

    public async Task InitializeAsync()
    {
        LoadUserProfile();
        await RefreshStatsAsync();
    }

    [RelayCommand]
    private async Task RefreshStatsAsync()
    {
        try
        {
            IsBusy = true;
            var stats = await _exporter.GetStatsAsync();
            
            TotalSessions = stats.TotalSessions;
            TotalMessages = stats.TotalMessages;
            RatedMessages = stats.RatedMessages;
            PositiveRatings = stats.PositiveRatings;
            NegativeRatings = stats.NegativeRatings;
            MessagesWithFeedback = stats.MessagesWithFeedback;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllRatedAsync()
    {
        try
        {
            IsBusy = true;
            
            if (RatedMessages == 0)
            {
                await _dialogService.ShowAlertAsync("No Data", "No rated messages to export. Rate some AI responses first!");
                return;
            }

            var filePath = await _exporter.ExportToJsonlAsync(onlyPositiveRatings: false);
            
            await _dialogService.ShowAlertAsync(
                "Export Complete",
                $"Exported {RatedMessages} rated conversations to:\n{filePath}");
                
            // Optionally share the file
            await ShareFileAsync(filePath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPositiveOnlyAsync()
    {
        try
        {
            IsBusy = true;
            
            if (PositiveRatings == 0)
            {
                await _dialogService.ShowAlertAsync("No Data", "No positive ratings to export. Give some thumbs up first!");
                return;
            }

            var filePath = await _exporter.ExportToJsonlAsync(onlyPositiveRatings: true);
            
            await _dialogService.ShowAlertAsync(
                "Export Complete",
                $"Exported {PositiveRatings} positive-rated conversations to:\n{filePath}");
                
            await ShareFileAsync(filePath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Export Failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShareFileAsync(string filePath)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Training Data",
                File = new ShareFile(filePath)
            });
        }
        catch
        {
            // Sharing not available on all platforms
        }
    }

    [RelayCommand]
    private async Task NavigateToOfflineModelsAsync()
    {
        await _navigationService.NavigateToAsync("OfflineModelsPage");
    }
}
