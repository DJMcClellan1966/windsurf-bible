using AI_Bible_App.Core.Models;
using AI_Bible_App.Core.Services;
using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AI_Bible_App.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly string _secretKey;
    private readonly string _publishableKey;
    private readonly Dictionary<string, string> _priceIds;

    public StripePaymentService(
        ILogger<StripePaymentService> logger,
        IConfiguration configuration,
        IUserRepository userRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _userRepository = userRepository;

        _secretKey = _configuration["Stripe:SecretKey"] 
            ?? throw new InvalidOperationException("Stripe SecretKey not configured");
        _publishableKey = _configuration["Stripe:PublishableKey"] 
            ?? throw new InvalidOperationException("Stripe PublishableKey not configured");

        _priceIds = new Dictionary<string, string>
        {
            ["PremiumMonthly"] = _configuration["Stripe:PriceIds:PremiumMonthly"] ?? "",
            ["PremiumYearly"] = _configuration["Stripe:PriceIds:PremiumYearly"] ?? "",
            ["PremiumPlusMonthly"] = _configuration["Stripe:PriceIds:PremiumPlusMonthly"] ?? "",
            ["PremiumPlusYearly"] = _configuration["Stripe:PriceIds:PremiumPlusYearly"] ?? ""
        };

        StripeConfiguration.ApiKey = _secretKey;
        _logger.LogInformation("StripePaymentService initialized with {KeyLength} character secret key", 
            _secretKey.Length);
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        string userId, 
        SubscriptionTier tier, 
        bool isYearly = false)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new CheckoutSessionResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            // Get or create Stripe customer
            var customerId = await GetOrCreateCustomerAsync(userId, user.Email, user.Name);

            // Get price ID based on tier and billing period
            var priceId = GetPriceId(tier, isYearly);
            if (string.IsNullOrEmpty(priceId))
            {
                return new CheckoutSessionResult
                {
                    Success = false,
                    ErrorMessage = "Invalid subscription tier"
                };
            }

            // Create checkout session
            // Use Stripe's hosted success/cancel pages since we're a desktop app
            // The user will see a success message and can return to the app manually
            var options = new SessionCreateOptions
            {
                Customer = customerId,
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                // Use simple redirect URLs - Stripe requires https for hosted checkout
                SuccessUrl = "https://checkout.stripe.com/success",
                CancelUrl = "https://checkout.stripe.com/cancel",
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["tier"] = tier.ToString(),
                    ["billingPeriod"] = isYearly ? "yearly" : "monthly"
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created checkout session {SessionId} for user {UserId}, tier {Tier}", 
                session.Id, userId, tier);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = session.Id,
                CheckoutUrl = session.Url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user {UserId}", userId);
            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> VerifyPaymentAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.PaymentStatus == "paid" && session.Metadata.TryGetValue("userId", out var userId))
            {
                // Update user subscription
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user != null)
                {
                    var tier = Enum.Parse<SubscriptionTier>(session.Metadata["tier"]);
                    var billingPeriod = session.Metadata["billingPeriod"];

                    user.Subscription = new UserSubscription
                    {
                        UserId = userId,
                        Tier = tier,
                        Status = SubscriptionStatus.Active,
                        StripeCustomerId = session.CustomerId,
                        StripeSubscriptionId = session.SubscriptionId,
                        SubscriptionStartDate = DateTime.UtcNow,
                        IsRecurring = true,
                        BillingPeriod = billingPeriod,
                        LastUpdated = DateTime.UtcNow
                    };

                    await _userRepository.UpdateUserAsync(user);
                    _logger.LogInformation("Activated subscription for user {UserId}, tier {Tier}", userId, tier);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<SubscriptionUpdateResult> CancelSubscriptionAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user?.Subscription?.StripeSubscriptionId == null)
            {
                return new SubscriptionUpdateResult
                {
                    Success = false,
                    Message = "No active subscription found"
                };
            }

            var service = new SubscriptionService();
            var subscription = await service.CancelAsync(user.Subscription.StripeSubscriptionId);

            user.Subscription.Status = SubscriptionStatus.Canceled;
            user.Subscription.SubscriptionEndDate = DateTime.UtcNow.AddDays(30); // Typically allows access until current period ends
            user.Subscription.LastUpdated = DateTime.UtcNow;

            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Canceled subscription for user {UserId}", userId);

            return new SubscriptionUpdateResult
            {
                Success = true,
                Message = "Subscription canceled. Access continues until the end of your current billing period.",
                NewStatus = SubscriptionStatus.Canceled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for user {UserId}", userId);
            return new SubscriptionUpdateResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<UserSubscription?> GetSubscriptionAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user?.Subscription?.StripeSubscriptionId == null)
            {
                return user?.Subscription;
            }

            // Refresh from Stripe
            var service = new SubscriptionService();
            var subscription = await service.GetAsync(user.Subscription.StripeSubscriptionId);

            // Update local subscription status
            user.Subscription.Status = subscription.Status switch
            {
                "active" => SubscriptionStatus.Active,
                "canceled" => SubscriptionStatus.Canceled,
                "past_due" => SubscriptionStatus.PastDue,
                "trialing" => SubscriptionStatus.Trial,
                _ => SubscriptionStatus.Expired
            };
            user.Subscription.SubscriptionEndDate = DateTime.UtcNow.AddDays(30); // Updated from Stripe
            user.Subscription.LastUpdated = DateTime.UtcNow;

            await _userRepository.UpdateUserAsync(user);

            return user.Subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription for user {UserId}", userId);
            return null;
        }
    }

    public async Task HandleWebhookEventAsync(string json, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Webhook secret not configured, skipping signature verification");
                return;
            }

            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

            _logger.LogInformation("Received webhook event: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Session;
                    if (session?.Metadata.TryGetValue("userId", out var userId) == true)
                    {
                        await VerifyPaymentAsync(session.Id);
                    }
                    break;

                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription?.Metadata.TryGetValue("userId", out userId) == true)
                    {
                        await GetSubscriptionAsync(userId); // This refreshes the subscription status
                    }
                    break;

                case "invoice.payment_succeeded":
                    _logger.LogInformation("Invoice payment succeeded");
                    break;

                case "invoice.payment_failed":
                    _logger.LogWarning("Invoice payment failed");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event");
            throw;
        }
    }

    public async Task<string> GetOrCreateCustomerAsync(string userId, string email, string name)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (!string.IsNullOrEmpty(user?.Subscription?.StripeCustomerId))
        {
            return user.Subscription.StripeCustomerId;
        }

        // Use a placeholder email if not provided (Stripe requires valid email format)
        var customerEmail = string.IsNullOrWhiteSpace(email) 
            ? $"{userId}@aibible.local" 
            : email;

        // Create new customer
        var options = new CustomerCreateOptions
        {
            Email = customerEmail,
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId
            }
        };

        var service = new CustomerService();
        var customer = await service.CreateAsync(options);

        // Save customer ID
        if (user != null)
        {
            user.Subscription ??= new UserSubscription { UserId = userId };
            user.Subscription.StripeCustomerId = customer.Id;
            await _userRepository.UpdateUserAsync(user);
        }

        _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

        return customer.Id;
    }

    private string GetPriceId(SubscriptionTier tier, bool isYearly)
    {
        return tier switch
        {
            SubscriptionTier.Premium => isYearly ? _priceIds["PremiumYearly"] : _priceIds["PremiumMonthly"],
            SubscriptionTier.PremiumPlus => isYearly ? _priceIds["PremiumPlusYearly"] : _priceIds["PremiumPlusMonthly"],
            _ => string.Empty
        };
    }
}
