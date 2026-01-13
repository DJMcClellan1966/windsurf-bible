using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Core.Services;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Maui.ViewModels;

public partial class SubscriptionViewModel : BaseViewModel
{
    private readonly IPaymentService _paymentService;
    private readonly IConversationQuotaService _quotaService;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SubscriptionViewModel> _logger;

    [ObservableProperty]
    private string currentTier = "Free";

    [ObservableProperty]
    private string currentStatus = "No active subscription";

    [ObservableProperty]
    private string quotaText = "0 of 10 messages used today";

    [ObservableProperty]
    private double quotaProgress = 0.0;

    [ObservableProperty]
    private bool showQuota = true;

    [ObservableProperty]
    private bool canUpgradeToPremium = true;

    [ObservableProperty]
    private bool canUpgradeToPremiumPlus = true;

    [ObservableProperty]
    private bool hasActiveSubscription = false;

    [ObservableProperty]
    private string subscriptionDetails = string.Empty;

    public SubscriptionViewModel(
        IPaymentService paymentService,
        IConversationQuotaService quotaService,
        IUserService userService,
        IDialogService dialogService,
        ILogger<SubscriptionViewModel> logger)
    {
        _paymentService = paymentService;
        _quotaService = quotaService;
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;

        Title = "Subscription";
    }

    public async Task LoadSubscriptionDataAsync()
    {
        try
        {
            IsBusy = true;

            var currentUser = _userService.CurrentUser;
            if (currentUser == null)
            {
                await _dialogService.ShowAlertAsync("Error", "No user logged in", "OK");
                return;
            }

            // Get subscription info
            var subscription = await _paymentService.GetSubscriptionAsync(currentUser.Id);
            if (subscription != null && subscription.IsActive)
            {
                CurrentTier = subscription.Tier.ToString();
                CurrentStatus = $"Active - {subscription.BillingPeriod}";
                HasActiveSubscription = true;
                ShowQuota = false;

                var endDate = subscription.SubscriptionEndDate?.ToString("MMMM dd, yyyy") ?? "Unknown";
                SubscriptionDetails = $"Next billing date: {endDate}\nBilling: ${GetPrice(subscription.Tier, subscription.BillingPeriod == "yearly")}";

                // Hide upgrade options based on current tier
                CanUpgradeToPremium = subscription.Tier < SubscriptionTier.Premium;
                CanUpgradeToPremiumPlus = subscription.Tier < SubscriptionTier.PremiumPlus;
            }
            else
            {
                // Free tier - show quota
                CurrentTier = "Free";
                CurrentStatus = "10 conversations per day";
                HasActiveSubscription = false;
                ShowQuota = true;
                CanUpgradeToPremium = true;
                CanUpgradeToPremiumPlus = true;

                var quota = await _quotaService.GetDailyQuotaAsync(currentUser.Id);
                QuotaText = $"{quota.MessagesUsed} of {quota.MessagesLimit} messages used today";
                QuotaProgress = (double)quota.MessagesUsed / quota.MessagesLimit;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription data");
            await _dialogService.ShowAlertAsync("Error", "Failed to load subscription information", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpgradeAsync(string parameter)
    {
        try
        {
            var parts = parameter.Split('|');
            if (parts.Length != 2)
            {
                await _dialogService.ShowAlertAsync("Error", "Invalid subscription selection", "OK");
                return;
            }

            var tierStr = parts[0];
            var periodStr = parts[1];
            var isYearly = periodStr.Equals("Yearly", StringComparison.OrdinalIgnoreCase);

            if (!Enum.TryParse<SubscriptionTier>(tierStr, out var tier))
            {
                await _dialogService.ShowAlertAsync("Error", "Invalid subscription tier", "OK");
                return;
            }

            var currentUser = _userService.CurrentUser;
            if (currentUser == null)
            {
                await _dialogService.ShowAlertAsync("Error", "No user logged in", "OK");
                return;
            }

            IsBusy = true;

            var result = await _paymentService.CreateCheckoutSessionAsync(currentUser.Id, tier, isYearly);
            
            if (result.Success && !string.IsNullOrEmpty(result.CheckoutUrl))
            {
                // Open Stripe checkout in browser
                await Launcher.OpenAsync(result.CheckoutUrl);
                
                await _dialogService.ShowAlertAsync(
                    "Checkout Started",
                    "Complete your payment in the browser. The app will update automatically once payment is confirmed.",
                    "OK");
            }
            else
            {
                await _dialogService.ShowAlertAsync("Error", result.ErrorMessage ?? "Failed to create checkout session", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting upgrade");
            await _dialogService.ShowAlertAsync("Error", "Failed to start upgrade process", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelSubscriptionAsync()
    {
        try
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Cancel Subscription",
                "Are you sure you want to cancel? You'll keep access until the end of your billing period.",
                "Yes, Cancel",
                "No");

            if (!confirm)
                return;

            var currentUser = _userService.CurrentUser;
            if (currentUser == null)
                return;

            IsBusy = true;

            var result = await _paymentService.CancelSubscriptionAsync(currentUser.Id);
            
            if (result.Success)
            {
                await _dialogService.ShowAlertAsync("Success", result.Message ?? "Subscription canceled", "OK");
                await LoadSubscriptionDataAsync();
            }
            else
            {
                await _dialogService.ShowAlertAsync("Error", result.Message ?? "Failed to cancel subscription", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription");
            await _dialogService.ShowAlertAsync("Error", "Failed to cancel subscription", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string GetPrice(SubscriptionTier tier, bool isYearly)
    {
        return tier switch
        {
            SubscriptionTier.Premium => isYearly ? "49.99/year" : "4.99/month",
            SubscriptionTier.PremiumPlus => isYearly ? "99/year" : "9.99/month",
            _ => "0"
        };
    }
}
