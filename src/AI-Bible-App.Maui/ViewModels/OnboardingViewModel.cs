using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AI_Bible_App.Maui.ViewModels;

/// <summary>
/// ViewModel for the Hallow-style onboarding flow.
/// Guides users through personalization questions before account creation.
/// </summary>
public partial class OnboardingViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    
    [ObservableProperty]
    private int currentStep = 0;
    
    [ObservableProperty]
    private int totalSteps = 5;
    
    [ObservableProperty]
    private string stepTitle = "Welcome";
    
    [ObservableProperty]
    private string stepSubtitle = "Let's personalize your experience";
    
    [ObservableProperty]
    private double progress = 0.0;
    
    [ObservableProperty]
    private bool canGoBack;
    
    [ObservableProperty]
    private bool canGoNext = true;
    
    [ObservableProperty]
    private string nextButtonText = "Continue";
    
    [ObservableProperty]
    private bool hasError;
    
    [ObservableProperty]
    private string errorMessage = string.Empty;

    // Step 1: Name
    [ObservableProperty]
    private string preferredName = string.Empty;
    
    // Step 2: Faith Journey
    [ObservableProperty]
    private FaithBackground selectedFaithBackground = FaithBackground.NotSpecified;
    
    // Step 3: Bible Familiarity
    [ObservableProperty]
    private BibleFamiliarity selectedBibleFamiliarity = BibleFamiliarity.Curious;
    
    // Step 4: Goals (multiple selection) - individual toggles for simpler binding
    [ObservableProperty]
    private bool goal1Selected;  // Bible Study
    [ObservableProperty]
    private bool goal2Selected;  // Daily Devotional
    [ObservableProperty]
    private bool goal3Selected;  // Prayer Support
    [ObservableProperty]
    private bool goal4Selected;  // Life Guidance
    [ObservableProperty]
    private bool goal5Selected;  // Historical Learning
    [ObservableProperty]
    private bool goal6Selected;  // Spiritual Growth
    [ObservableProperty]
    private bool goal7Selected;  // Teaching Others
    [ObservableProperty]
    private bool goal8Selected;  // Personal Reflection
    [ObservableProperty]
    private bool goal9Selected;  // Family Devotion
    
    // Step 5: Engagement
    [ObservableProperty]
    private EngagementFrequency selectedFrequency = EngagementFrequency.FewTimesWeek;

    public OnboardingViewModel(IUserService userService, IAuthenticationService authService)
    {
        _userService = userService;
        _authService = authService;
        Title = "Personalize Your Journey";
        UpdateStepInfo();
    }
    
    private void UpdateStepInfo()
    {
        Progress = (double)CurrentStep / TotalSteps;
        CanGoBack = CurrentStep > 0;
        
        switch (CurrentStep)
        {
            case 0:
                StepTitle = "What should we call you?";
                StepSubtitle = "We'll use this to personalize your experience";
                NextButtonText = "Continue";
                break;
            case 1:
                StepTitle = "Tell us about your faith journey";
                StepSubtitle = "This helps us meet you where you are";
                NextButtonText = "Continue";
                break;
            case 2:
                StepTitle = "How familiar are you with the Bible?";
                StepSubtitle = "No wrong answers - we'll adapt to you";
                NextButtonText = "Continue";
                break;
            case 3:
                StepTitle = "What do you hope to gain?";
                StepSubtitle = "Select all that apply";
                NextButtonText = "Continue";
                break;
            case 4:
                StepTitle = "How often would you like to engage?";
                StepSubtitle = "We'll customize reminders based on your preference";
                NextButtonText = "Create My Experience";
                break;
        }
    }
    
    // Direct selection commands for Faith Background (Step 1)
    [RelayCommand]
    private async Task SelectFaith1() { SelectedFaithBackground = FaithBackground.LifelongChristian; await GoNext(); }
    [RelayCommand]
    private async Task SelectFaith2() { SelectedFaithBackground = FaithBackground.ReturningToFaith; await GoNext(); }
    [RelayCommand]
    private async Task SelectFaith3() { SelectedFaithBackground = FaithBackground.NewBeliever; await GoNext(); }
    [RelayCommand]
    private async Task SelectFaith4() { SelectedFaithBackground = FaithBackground.Exploring; await GoNext(); }
    [RelayCommand]
    private async Task SelectFaith5() { SelectedFaithBackground = FaithBackground.OtherFaith; await GoNext(); }
    [RelayCommand]
    private async Task SelectFaith6() { SelectedFaithBackground = FaithBackground.Skeptic; await GoNext(); }
    
    // Direct selection commands for Bible Familiarity (Step 2)
    [RelayCommand]
    private async Task SelectBible1() { SelectedBibleFamiliarity = BibleFamiliarity.NeverRead; await GoNext(); }
    [RelayCommand]
    private async Task SelectBible2() { SelectedBibleFamiliarity = BibleFamiliarity.Curious; await GoNext(); }
    [RelayCommand]
    private async Task SelectBible3() { SelectedBibleFamiliarity = BibleFamiliarity.Beginner; await GoNext(); }
    [RelayCommand]
    private async Task SelectBible4() { SelectedBibleFamiliarity = BibleFamiliarity.Intermediate; await GoNext(); }
    [RelayCommand]
    private async Task SelectBible5() { SelectedBibleFamiliarity = BibleFamiliarity.Advanced; await GoNext(); }
    [RelayCommand]
    private async Task SelectBible6() { SelectedBibleFamiliarity = BibleFamiliarity.Scholar; await GoNext(); }
    
    // Toggle commands for Goals (Step 3) - multi-select
    [RelayCommand]
    private void ToggleGoal1() => Goal1Selected = !Goal1Selected;
    [RelayCommand]
    private void ToggleGoal2() => Goal2Selected = !Goal2Selected;
    [RelayCommand]
    private void ToggleGoal3() => Goal3Selected = !Goal3Selected;
    [RelayCommand]
    private void ToggleGoal4() => Goal4Selected = !Goal4Selected;
    [RelayCommand]
    private void ToggleGoal5() => Goal5Selected = !Goal5Selected;
    [RelayCommand]
    private void ToggleGoal6() => Goal6Selected = !Goal6Selected;
    [RelayCommand]
    private void ToggleGoal7() => Goal7Selected = !Goal7Selected;
    [RelayCommand]
    private void ToggleGoal8() => Goal8Selected = !Goal8Selected;
    [RelayCommand]
    private void ToggleGoal9() => Goal9Selected = !Goal9Selected;
    
    // Direct selection commands for Frequency (Step 4)
    [RelayCommand]
    private async Task SelectFreq1() { SelectedFrequency = EngagementFrequency.Daily; await CompleteOnboarding(); }
    [RelayCommand]
    private async Task SelectFreq2() { SelectedFrequency = EngagementFrequency.FewTimesWeek; await CompleteOnboarding(); }
    [RelayCommand]
    private async Task SelectFreq3() { SelectedFrequency = EngagementFrequency.Weekly; await CompleteOnboarding(); }
    [RelayCommand]
    private async Task SelectFreq4() { SelectedFrequency = EngagementFrequency.Occasionally; await CompleteOnboarding(); }
    [RelayCommand]
    private async Task SelectFreq5() { SelectedFrequency = EngagementFrequency.WhenNeeded; await CompleteOnboarding(); }
    
    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            UpdateStepInfo();
        }
    }
    
    [RelayCommand]
    private async Task GoNext()
    {
        HasError = false;
        
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
            UpdateStepInfo();
        }
        else
        {
            // Final step - complete onboarding
            await CompleteOnboarding();
        }
    }
    
    // Helper to get selected goals as list
    private List<UserGoal> GetSelectedGoals()
    {
        var goals = new List<UserGoal>();
        if (Goal1Selected) goals.Add(UserGoal.DeepBibleStudy);
        if (Goal2Selected) goals.Add(UserGoal.DailyDevotional);
        if (Goal3Selected) goals.Add(UserGoal.PrayerSupport);
        if (Goal4Selected) goals.Add(UserGoal.LifeGuidance);
        if (Goal5Selected) goals.Add(UserGoal.HistoricalLearning);
        if (Goal6Selected) goals.Add(UserGoal.SpiritualGrowth);
        if (Goal7Selected) goals.Add(UserGoal.TeachingOthers);
        if (Goal8Selected) goals.Add(UserGoal.PersonalReflection);
        if (Goal9Selected) goals.Add(UserGoal.FamilyDevotion);
        return goals;
    }
    
    [RelayCommand]
    private void SelectBibleFamiliarity(BibleFamiliarity familiarity)
    {
        SelectedBibleFamiliarity = familiarity;
    }
    
    [RelayCommand]
    private void SelectFrequency(EngagementFrequency frequency)
    {
        SelectedFrequency = frequency;
    }
    
    private async Task CompleteOnboarding()
    {
        try
        {
            IsBusy = true;
            
            // Build the onboarding profile
            var profile = new OnboardingProfile
            {
                PreferredName = string.IsNullOrWhiteSpace(PreferredName) ? null : PreferredName.Trim(),
                FaithBackground = SelectedFaithBackground,
                BibleFamiliarity = SelectedBibleFamiliarity,
                Goals = GetSelectedGoals(),
                PreferredFrequency = SelectedFrequency,
                IsComplete = true,
                CompletedAt = DateTime.UtcNow
            };
            
            // Store in preferences for use during account creation
            var json = System.Text.Json.JsonSerializer.Serialize(profile);
            Preferences.Set("onboarding_profile", json);
            
            // Navigate to account creation
            await Shell.Current.GoToAsync("//accountcreation");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Something went wrong: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task Skip()
    {
        // Skip onboarding and go directly to account creation
        Preferences.Remove("onboarding_profile");
        await Shell.Current.GoToAsync("//accountcreation");
    }
}

// Helper record classes for options
public record FaithBackgroundOption(string Emoji, string Title, string Description, FaithBackground Value);
public record BibleFamiliarityOption(string Emoji, string Title, string Description, BibleFamiliarity Value);
public record GoalOption(string Emoji, string Title, UserGoal Value);
public record FrequencyOption(string Emoji, string Title, string Description, EngagementFrequency Value);
