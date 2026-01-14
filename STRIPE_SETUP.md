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

### ✅ Completed Steps
1. ✅ Install `Stripe.net` NuGet package
2. ✅ Create `IPaymentService.cs` interface
3. ✅ Implement `StripePaymentService.cs`
4. ✅ Create `IConversationQuotaService.cs` for free tier limits
5. ✅ Implement `ConversationQuotaService.cs` with JSON tracking
6. ✅ Create `SubscriptionPage.xaml` UI
7. ✅ Modify `ChatViewModel.cs` for quota enforcement
8. ✅ Register services in DI container

### 🗄️ Database/Backend Setup (Required for Email Signup)

**Note:** If you want users to create accounts with email/password, or support multi-device sync, you must add a backend database and authentication service. Local JSON storage is only suitable for single-device, test accounts.


**Steps:**
1. Design database schema for users, sessions, subscriptions, and chat history
2. Choose a backend (e.g., ASP.NET Core, Azure, Supabase, Firebase, etc.)
3. Implement REST API endpoints for signup, login, and user management
4. Integrate authentication:
  - Email/password
  - Google Sign-In (OAuth)
  - Apple Sign-In (OAuth)
  - (See below for Google/Apple setup)
5. Update app to call backend for account creation and login
6. Store Stripe customer/subscription IDs in the database

---

### Google Authentication Setup
1. Go to Google Cloud Console: https://console.cloud.google.com/
2. Create a new project (or select existing)
3. Enable "OAuth 2.0 Client IDs" in APIs & Services → Credentials
4. "GoogleAuth": {
  "ClientId": "[REDACTED_GOOGLE_CLIENT_ID]",
  "ClientSecret": "[REDACTED_GOOGLE_CLIENT_SECRET]",
  "RedirectUri": "https://voicesofscripture.com  "Firebase": {
    "ApiKey": "[REDACTED_FIREBASE_API_KEY]",
    "AuthDomain": "your-app.firebaseapp.com",
    "ProjectId": "your-app",
    "StorageBucket": "your-app.appspot.com",
    "MessagingSenderId": "1234567890",
    "AppId": "1:1234567890:web:abcdef"
  }  "Firebase": {
    "ApiKey": "[REDACTED_FIREBASE_API_KEY]",
    "AuthDomain": "voices-of-scripture-71109.firebaseapp.com",
    "ProjectId": "voices-of-scripture-71109",
    "StorageBucket": "voices-of-scripture-71109.firebasestorage.app",
    "MessagingSenderId": "419623769192",
    "AppId": "1:419623769192:web:cfc0694b89de6ef184d7dd",
    "MeasurementId": "G-HZP75QWP4J"
  }  "Firebase": {
    "ApiKey": "[REDACTED_FIREBASE_API_KEY]",
    "AuthDomain": "voices-of-scripture-71109.firebaseapp.com",
    "ProjectId": "voices-of-scripture-71109",
    "StorageBucket": "voices-of-scripture-71109.firebasestorage.app",
    "MessagingSenderId": "419623769192",
    "AppId": "1:419623769192:web:cfc0694b89de6ef184d7dd",
    "MeasurementId": "G-HZP75QWP4J"
  }/api/auth/google/callback"
} (e.g., `https://yourdomain.com/api/auth/google/callback`)
5. Download client ID and secret
6. Add to your backend's configuration
7. Implement Google OAuth flow in your backend and app
8. Test sign-in and account linking

### Apple Authentication Setup
1. Go to Apple Developer Portal: https://developer.apple.com/account/resources/identifiers/list
2. Register your app's Bundle ID
3. Enable "Sign In with Apple" capability
4. Create a Services ID and configure redirect URI (e.g., `https://yourdomain.com/api/auth/apple/callback`)
5. Generate and download the private key
6. Add Apple client ID, team ID, and key to your backend's configuration
7. Implement Apple OAuth flow in your backend and app
8. Test sign-in and account linking

---

**Common Error:**
> "Signup failed: no user is currently logged in"

This error occurs because the app is trying to create an account without a backend to store user credentials. Add a backend/database to enable real account creation.

### 🧪 Part 3: Testing (NEXT)

Follow the comprehensive testing guide in **[TESTING_MONETIZATION.md](TESTING_MONETIZATION.md)**:

1. **Test Quota Enforcement**: Send 10 messages as free user → verify 11th message shows upgrade dialog
2. **Test Stripe Checkout**: Use test card `4242 4242 4242 4242` to upgrade to Premium
3. **Verify Premium Access**: Confirm unlimited messages after successful payment
4. **Test Yearly Subscription**: Try Premium Plus yearly plan
5. **Test Cancellation**: Cancel subscription and verify status updates

**Ready to test!** 🚀 See [TESTING_MONETIZATION.md](TESTING_MONETIZATION.md) for detailed instructions.
