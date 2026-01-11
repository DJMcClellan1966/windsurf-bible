using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

#pragma warning disable MVVMTK0045 // AOT compatibility warning for WinRT scenarios

namespace AI_Bible_App.Maui.ViewModels;

public partial class UserSelectionViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<AppUser> users = new();

    [ObservableProperty]
    private string newUserName = string.Empty;

    [ObservableProperty]
    private string selectedEmoji = "😊";

    // PIN entry state
    [ObservableProperty]
    private bool isPinEntryVisible;

    [ObservableProperty]
    private string pinEntry = string.Empty;

    [ObservableProperty]
    private AppUser? pendingUser;

    [ObservableProperty]
    private string pinError = string.Empty;

    public UserSelectionViewModel(
        IUserService userService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _userService = userService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        Title = "Select User";
    }

    public async Task LoadUsersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            // Reset PIN entry state when loading
            IsPinEntryVisible = false;
            PinEntry = string.Empty;
            PendingUser = null;
            PinError = string.Empty;
            
            var allUsers = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<AppUser>(allUsers.OrderByDescending(u => u.LastActiveAt));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to load users: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectUser(AppUser user)
    {
        if (user == null || IsBusy) return;

        // If user has PIN, show PIN entry
        if (user.HasPin)
        {
            PendingUser = user;
            PinEntry = string.Empty;
            PinError = string.Empty;
            IsPinEntryVisible = true;
            return;
        }

        // No PIN required, proceed directly
        await LoginUserAsync(user);
    }

    [RelayCommand]
    private async Task SubmitPin()
    {
        if (PendingUser == null || string.IsNullOrEmpty(PinEntry))
        {
            PinError = "Please enter your PIN";
            return;
        }

        try
        {
            IsBusy = true;
            var isValid = await _userService.VerifyPinAsync(PendingUser.Id, PinEntry);
            
            if (isValid)
            {
                await LoginUserAsync(PendingUser);
            }
            else
            {
                PinError = "Incorrect PIN. Please try again.";
                PinEntry = string.Empty;
            }
        }
        catch (Exception ex)
        {
            PinError = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelPinEntry()
    {
        IsPinEntryVisible = false;
        PendingUser = null;
        PinEntry = string.Empty;
        PinError = string.Empty;
    }

    private async Task LoginUserAsync(AppUser user)
    {
        try
        {
            IsBusy = true;
            await _userService.SwitchUserAsync(user.Id);
            
            // Reset PIN state
            IsPinEntryVisible = false;
            PendingUser = null;
            PinEntry = string.Empty;
            
            // Navigate to main app
            await Shell.Current.GoToAsync("//characters");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to switch user: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateUser()
    {
        if (string.IsNullOrWhiteSpace(NewUserName))
        {
            await _dialogService.ShowAlertAsync("Name Required", "Please enter a name for your profile.");
            return;
        }

        if (NewUserName.Length > 30)
        {
            await _dialogService.ShowAlertAsync("Name Too Long", "Please enter a name with 30 characters or less.");
            return;
        }

        try
        {
            IsBusy = true;
            var user = await _userService.CreateUserAsync(NewUserName.Trim(), SelectedEmoji);
            
            // Auto-select the new user
            await _userService.SwitchUserAsync(user.Id);
            
            // Clear the input
            NewUserName = string.Empty;
            
            // Navigate to main app
            await Shell.Current.GoToAsync("//characters");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"Failed to create user: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
