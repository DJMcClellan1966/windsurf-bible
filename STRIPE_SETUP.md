# Stripe Setup Guide

## Step 1: Get Your API Keys

1. **Go to Stripe Dashboard**: https://dashboard.stripe.com/
2. **Enable Test Mode**: Toggle "Test mode" in the top right (should see a colored banner)
3. **Navigate to Developers → API Keys**: https://dashboard.stripe.com/test/apikeys

You'll see two keys:
- **Publishable key**: Starts with `pk_test_...` (safe to expose in client code)
- **Secret key**: Starts with `sk_test_...` (⚠️ NEVER expose publicly)

## Step 2: Create Your Products & Prices

1. **Go to Products**: https://dashboard.stripe.com/test/products
2. **Create Product: "Premium"**
   - Name: `AI Bible Premium`
   - Description: `Unlimited conversations, Phi-4 model, 5 users, cloud sync`
   - Click "Save product"

3. **Add Pricing**:
   - **Monthly**: $4.99/month recurring
     - Click "Add another price"
     - Price: `4.99` USD
     - Billing period: `Monthly`
     - Save and copy the **Price ID** (starts with `price_...`)
   
   - **Yearly**: $49.99/year recurring (save 17%)
     - Click "Add another price"
     - Price: `49.99` USD
     - Billing period: `Yearly`
     - Save and copy the **Price ID**

4. **Create Product: "Premium Plus"**
   - Name: `AI Bible Premium Plus`
   - Description: `Premium character packs, roundtables, fine-tuned models, GPT-4 access`
   - Click "Save product"

5. **Add Pricing**:
   - **Monthly**: $9.99/month recurring
     - Copy the **Price ID**
   
   - **Yearly**: $99/year recurring (save 17%)
     - Copy the **Price ID**

## Step 3: Add Keys to appsettings.json

Open: `src/AI-Bible-App.Maui/appsettings.json`

Replace the placeholders in the `Stripe` section:

```json
"Stripe": {
  "PublishableKey": "pk_test_YOUR_KEY_HERE",          ← Paste your publishable key
  "SecretKey": "sk_test_YOUR_KEY_HERE",                ← Paste your secret key
  "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET_HERE",   ← Leave for now (Step 4)
  "PriceIds": {
    "PremiumMonthly": "price_YOUR_PREMIUM_MONTHLY_ID",      ← Paste Price ID
    "PremiumYearly": "price_YOUR_PREMIUM_YEARLY_ID",        ← Paste Price ID
    "PremiumPlusMonthly": "price_YOUR_PREMIUM_PLUS_MONTHLY_ID",  ← Paste Price ID
    "PremiumPlusYearly": "price_YOUR_PREMIUM_PLUS_YEARLY_ID"     ← Paste Price ID
  }
}
```

### Example (with fake keys):
```json
"Stripe": {
  "PublishableKey": "pk_test_51AbCdEf1234567890GhIjKlMnOpQr",
  "SecretKey": "sk_test_51AbCdEf1234567890GhIjKlMnOpQr",
  "WebhookSecret": "whsec_1234567890abcdefghijklmnopqrstuvwxyz",
  "PriceIds": {
    "PremiumMonthly": "price_1AbCdEfGhIjKlMnO",
    "PremiumYearly": "price_1AbCdEfGhIjKlMnP",
    "PremiumPlusMonthly": "price_1AbCdEfGhIjKlMnQ",
    "PremiumPlusYearly": "price_1AbCdEfGhIjKlMnR"
  }
}
```

## Step 4: Set Up Webhooks (Later)

Webhooks notify your app when payments succeed/fail. We'll set this up after implementing the payment service.

1. Go to: https://dashboard.stripe.com/test/webhooks
2. Click "Add endpoint"
3. Endpoint URL: `https://yourdomain.com/api/stripe/webhook` (local testing: use ngrok)
4. Select events to listen for:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
5. Copy the **Signing secret** (starts with `whsec_...`)
6. Paste into `appsettings.json` → `Stripe.WebhookSecret`

## Step 5: Install Stripe.net NuGet Package

Run in PowerShell from workspace root:

```powershell
dotnet add src/AI-Bible-App.Infrastructure/AI-Bible-App.Infrastructure.csproj package Stripe.net
```

## Step 6: Test Cards

When testing in Test Mode, use these cards:

| Card Number          | Result                          |
|---------------------|---------------------------------|
| 4242 4242 4242 4242 | ✅ Success                       |
| 4000 0000 0000 9995 | ❌ Declined (insufficient funds) |
| 4000 0025 0000 3155 | ⚠️ Requires authentication (3D Secure) |

- **Expiry**: Any future date (e.g., 12/34)
- **CVC**: Any 3 digits (e.g., 123)
- **ZIP**: Any 5 digits (e.g., 12345)

## Step 7: View Test Transactions

After making test payments:
- **Payments**: https://dashboard.stripe.com/test/payments
- **Customers**: https://dashboard.stripe.com/test/customers
- **Subscriptions**: https://dashboard.stripe.com/test/subscriptions
- **Logs**: https://dashboard.stripe.com/test/logs

## Security Checklist

- ✅ Secret key in `appsettings.json` (ignored by git)
- ⚠️ **NEVER** commit secret key to GitHub
- ✅ Use test mode keys during development
- ✅ Switch to live mode keys only in production
- ✅ Store live keys in environment variables or Azure Key Vault

## Going Live (When Ready)

1. Toggle off "Test mode" in Stripe Dashboard
2. Complete Stripe account activation:
   - Business details
   - Bank account for payouts
   - Tax information
3. Create products/prices again in **Live Mode**
4. Get **live** API keys (start with `pk_live_...` and `sk_live_...`)
5. Update production `appsettings.json` with live keys
6. Set up live webhook endpoint
7. Test with real card (charge yourself $0.50, then refund)

## Useful Stripe Links

- **Dashboard**: https://dashboard.stripe.com/
- **Documentation**: https://stripe.com/docs/api
- **Testing Guide**: https://stripe.com/docs/testing
- **Checkout Docs**: https://stripe.com/docs/payments/checkout
- **Subscription Docs**: https://stripe.com/docs/billing/subscriptions/overview

---

## Next Steps After Setup

Once you have your keys in `appsettings.json`:

1. ✅ Install `Stripe.net` NuGet package
2. Create `IPaymentService.cs` interface
3. Implement `StripePaymentService.cs`
4. Create `SubscriptionPage.xaml` UI
5. Test checkout flow with card 4242 4242 4242 4242

**Ready to code the payment service!** 🚀
