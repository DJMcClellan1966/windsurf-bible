# Testing Monetization Implementation

## ✅ What We've Built

### 1. **Subscription UI** (SubscriptionPage.xaml)
- Current plan display with tier, status, and quota progress
- Premium card ($4.99/month or $49.99/year) with 7 benefits
- Premium Plus card ($9.99/month or $99/year) with "BEST VALUE" badge
- Monthly/Yearly upgrade buttons
- Cancel subscription functionality
- FAQ section

### 2. **Quota Enforcement** (ChatViewModel)
- Checks conversation quota before sending messages
- Shows upgrade dialog when limit reached (10 messages/day for free tier)
- Records each message sent for tracking
- "Upgrade to Premium" button navigates to SubscriptionPage

### 3. **Backend Services**
- `IConversationQuotaService`: Tracks daily message limits (10/day free tier)
- `IPaymentService`: Stripe integration for checkout, subscriptions, webhooks
- `StripePaymentService`: Full Stripe API integration
- `ConversationQuotaService`: JSON-based quota persistence

### 4. **Navigation & Registration**
- SubscriptionPage registered in DI container
- Route registered: `Shell.Current.GoToAsync("//SubscriptionPage")`
- Auto-loads subscription data on page appearance

---

## 🧪 Testing Instructions

### Test 1: Quota Enforcement (Free Tier)
**Goal**: Verify that free users are limited to 10 conversations per day

**Steps**:
1. Launch the app
2. Select a character and start a chat
3. Send 10 messages (count them carefully)
4. On the 11th message, you should see:
   - Dialog: "Daily Limit Reached"
   - Message: "You've reached your daily limit of 10 conversations"
   - "Upgrade to Premium" button
   - Quota reset time displayed

**Expected Result**: 
✅ First 10 messages send successfully
✅ 11th message blocked with upgrade prompt

---

### Test 2: Navigate to Subscription Page
**Goal**: Verify subscription page is accessible and loads correctly

**Steps**:
1. From the quota limit dialog, click "Upgrade to Premium"
2. **OR** manually navigate (add menu item or use debug navigation)

**Expected Result**: 
✅ SubscriptionPage opens
✅ Shows "Free" tier with quota display
✅ "0 of 10 messages used today" (or current usage)
✅ Premium and Premium Plus cards visible with pricing
✅ Monthly/Yearly buttons enabled

---

### Test 3: Stripe Checkout Flow (Premium - Monthly)
**Goal**: Test Stripe checkout with test card

**Steps**:
1. Navigate to SubscriptionPage
2. Click **"Monthly $4.99"** button under Premium card
3. Browser opens with Stripe checkout page
4. Enter Stripe test card details:
   - **Card Number**: `4242 4242 4242 4242`
   - **Expiration**: `12/34` (any future date)
   - **CVC**: `123` (any 3 digits)
   - **ZIP**: `12345` (any 5 digits)
   - **Email**: Your test email
5. Click "Pay"
6. Browser should redirect to success URL

**Expected Result**: 
✅ Checkout session created successfully
✅ Browser opens Stripe hosted checkout
✅ Test payment completes
✅ Redirected to success page

**⚠️ Known Issue**: App may not auto-refresh subscription status after payment. You may need to manually restart the app or navigate back to SubscriptionPage.

---

### Test 4: Verify Premium Access
**Goal**: Confirm unlimited conversations after upgrade

**Steps**:
1. After successful payment, restart app or reload SubscriptionPage
2. SubscriptionPage should now show:
   - Current Tier: "Premium"
   - Status: "Active - monthly"
   - No quota progress bar
   - "Manage Subscription" section visible
3. Go to chat and send **20+ messages** consecutively

**Expected Result**: 
✅ All messages send without quota dialog
✅ Unlimited access confirmed

---

### Test 5: Stripe Checkout Flow (Premium Plus - Yearly)
**Goal**: Test yearly subscription with higher tier

**Steps**:
1. Navigate to SubscriptionPage (if already Premium, you may need to cancel first)
2. Click **"Yearly $99"** button under Premium Plus card
3. Enter test card: `4242 4242 4242 4242`
4. Complete payment

**Expected Result**: 
✅ Checkout session created successfully
✅ Payment completes
✅ SubscriptionPage shows "Premium Plus" tier
✅ Yearly billing displayed

---

### Test 6: Cancel Subscription
**Goal**: Verify subscription cancellation works

**Steps**:
1. Navigate to SubscriptionPage (must have active subscription)
2. Scroll to "Manage Subscription" section
3. Click **"Cancel Subscription"** button
4. Confirmation dialog appears: "Are you sure? You'll keep access until the end of your billing period."
5. Click "Yes, Cancel"

**Expected Result**: 
✅ Cancellation successful
✅ Status updated to "Canceled" (may need reload)
✅ Message shows "You'll keep access until [date]"

**Note**: In Stripe test mode, access doesn't actually expire at the billing period end. You'd need to manually reset in production.

---

## 🔧 Debugging Tips

### If SubscriptionPage doesn't load:
1. Check DI registration in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddTransient<SubscriptionViewModel>();
   builder.Services.AddTransient<SubscriptionPage>();
   ```
2. Check route registration in `AppShell.xaml.cs`:
   ```csharp
   Routing.RegisterRoute("SubscriptionPage", typeof(SubscriptionPage));
   ```

### If quota doesn't reset:
- Quota resets at midnight local time
- For testing, use `IConversationQuotaService.ResetQuotaAsync(userId)` in code

### If Stripe checkout fails:
1. Check `appsettings.local.json` has actual Stripe keys
2. Verify Stripe products created in dashboard:
   - Premium Monthly: `price_xxx`
   - Premium Yearly: `price_xxx`
   - Premium Plus Monthly: `price_xxx`
   - Premium Plus Yearly: `price_xxx`
3. Ensure price IDs match in config

### If payment doesn't update subscription:
- Current implementation uses polling, not webhooks
- Webhooks require public endpoint (not available in local dev)
- For now, manually restart app to see updated subscription

---

## 📊 Stripe Test Cards

| Card Number | Result |
|-------------|--------|
| `4242 4242 4242 4242` | ✅ Success |
| `4000 0000 0000 0002` | ❌ Card Declined |
| `4000 0000 0000 9995` | ❌ Insufficient Funds |
| `4000 0000 0000 0341` | ⚠️ Requires Authentication (3D Secure) |

**Always use**:
- Expiration: Any future date (e.g., `12/34`)
- CVC: Any 3 digits (e.g., `123`)
- ZIP: Any 5 digits (e.g., `12345`)

---

## 🚀 Next Steps

### To Complete Testing:
1. ✅ Test quota enforcement (10 message limit)
2. ✅ Test Stripe checkout with `4242 4242 4242 4242`
3. ✅ Verify unlimited messages after upgrade
4. ✅ Test yearly subscription
5. ✅ Test subscription cancellation

### Optional Enhancements:
- Add "Upgrade" menu item to MainPage hamburger menu
- Add quota display badge in chat UI header
- Implement webhook endpoint for real-time subscription updates
- Add background subscription refresh timer
- Add trial period support (Stripe has built-in 14-day trials)

---

## 📝 Quota Reset Schedule

**Free Tier**: 10 conversations/day
**Reset Time**: Midnight (00:00) local device time
**Storage**: `%LOCALAPPDATA%\AIBibleApp\Quota\daily-quota.json`

**Format**:
```json
{
  "user-123": {
    "2025-01-12": 5,
    "2025-01-13": 0
  }
}
```

---

## 🔐 Security Notes

- **API Keys**: Stored in `appsettings.local.json` (not committed to git)
- **Test Mode**: All keys start with `pk_test_` and `sk_test_`
- **Production**: Replace with `pk_live_` and `sk_live_` keys before launch
- **Webhooks**: Requires HTTPS public endpoint in production

---

## 💰 Pricing Tiers

| Tier | Price | Quota | Features |
|------|-------|-------|----------|
| **Free** | $0 | 10/day | Basic AI (Phi-3 Mini), 1 user, local storage |
| **Premium** | $4.99/month<br>$49.99/year | Unlimited | Phi-4, 5 users, cloud sync, custom characters |
| **Premium Plus** | $9.99/month<br>$99/year | Unlimited | Everything in Premium + character packs, roundtables, marketplace |
| **Enterprise** | $199/year | Unlimited | 50+ licenses, custom branding, priority support |

---

## ✅ Implementation Checklist

- [x] Create SubscriptionPage.xaml UI
- [x] Create SubscriptionViewModel business logic
- [x] Register services in DI container
- [x] Add route registration
- [x] Modify ChatViewModel for quota checking
- [x] Add "Upgrade to Premium" dialog on quota limit
- [x] Record messages for quota tracking
- [x] Build succeeds with no errors

**Status**: Ready for testing! 🎉

---

## 🐛 Known Issues

1. **Manual Refresh Required**: After Stripe payment, app doesn't auto-detect subscription change. Workaround: Restart app or navigate away and back to SubscriptionPage.

2. **Webhook Endpoint Missing**: No real-time subscription updates. Need to implement public webhook endpoint for production.

3. **Trial Period Not Implemented**: Stripe supports 14-day free trials, but we haven't enabled this feature yet.

---

## 📞 Support

If you encounter issues:
1. Check build output for errors
2. Review `appsettings.local.json` for valid Stripe keys
3. Check Stripe dashboard for payment logs
4. Enable debug logging in `ConversationQuotaService` and `StripePaymentService`

For Stripe issues: https://dashboard.stripe.com/test/logs
