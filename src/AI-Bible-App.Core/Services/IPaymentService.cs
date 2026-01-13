using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Services;

public class CheckoutSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SubscriptionUpdateResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public SubscriptionStatus? NewStatus { get; set; }
}

public interface IPaymentService
{
    /// <summary>
    /// Create a Stripe checkout session for subscription purchase
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        string userId, 
        SubscriptionTier tier, 
        bool isYearly = false);

    /// <summary>
    /// Verify payment was successful and activate subscription
    /// </summary>
    Task<bool> VerifyPaymentAsync(string sessionId);

    /// <summary>
    /// Cancel user's active subscription
    /// </summary>
    Task<SubscriptionUpdateResult> CancelSubscriptionAsync(string userId);

    /// <summary>
    /// Get current subscription status from Stripe
    /// </summary>
    Task<UserSubscription?> GetSubscriptionAsync(string userId);

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    Task HandleWebhookEventAsync(string json, string signature);

    /// <summary>
    /// Create or retrieve Stripe customer ID for user
    /// </summary>
    Task<string> GetOrCreateCustomerAsync(string userId, string email, string name);
}
