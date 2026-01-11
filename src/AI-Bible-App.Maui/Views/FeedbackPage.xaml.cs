using AI_Bible_App.Infrastructure.Services;

namespace AI_Bible_App.Maui.Views;

public partial class FeedbackPage : ContentPage
{
    private readonly IFeedbackService _feedbackService;
    private int _selectedRating = 0;
    private readonly Button[] _starButtons;

    public FeedbackPage(IFeedbackService feedbackService)
    {
        InitializeComponent();
        _feedbackService = feedbackService;
        _starButtons = new[] { Star1, Star2, Star3, Star4, Star5 };
        
        MessageEditor.TextChanged += OnMessageTextChanged;
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        // Could add logic to customize form based on type
    }

    private void OnStarClicked(object? sender, EventArgs e)
    {
        if (sender is Button clickedStar)
        {
            var index = Array.IndexOf(_starButtons, clickedStar);
            _selectedRating = index + 1;
            UpdateStarDisplay();
        }
    }

    private void UpdateStarDisplay()
    {
        for (int i = 0; i < _starButtons.Length; i++)
        {
            _starButtons[i].Text = i < _selectedRating ? "★" : "☆";
            _starButtons[i].TextColor = i < _selectedRating 
                ? Color.FromArgb("#FFD700") 
                : Colors.Gray;
        }

        RatingLabel.Text = _selectedRating switch
        {
            1 => "Poor",
            2 => "Fair",
            3 => "Good",
            4 => "Great",
            5 => "Excellent!",
            _ => "Tap to rate"
        };
    }

    private void OnMessageTextChanged(object? sender, TextChangedEventArgs e)
    {
        var count = e.NewTextValue?.Length ?? 0;
        CharCountLabel.Text = $"{count}/2000";
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(MessageEditor.Text))
        {
            await DisplayAlert("Missing Feedback", "Please enter your feedback message.", "OK");
            return;
        }

        if (FeedbackTypePicker.SelectedIndex < 0)
        {
            await DisplayAlert("Missing Type", "Please select a feedback type.", "OK");
            return;
        }

        // Disable button during submission
        SubmitButton.IsEnabled = false;
        SubmitButton.Text = "Submitting...";

        try
        {
            var feedbackType = FeedbackTypePicker.SelectedItem?.ToString() ?? "General";
            // Remove emoji prefix
            feedbackType = feedbackType.Length > 2 ? feedbackType[2..].Trim() : feedbackType;

            var feedback = new FeedbackSubmission
            {
                Type = feedbackType,
                Category = CategoryPicker.SelectedItem?.ToString(),
                Message = MessageEditor.Text,
                Rating = _selectedRating > 0 ? _selectedRating : null,
                Context = string.IsNullOrWhiteSpace(ContextEntry.Text) ? null : ContextEntry.Text,
                ContactEmail = string.IsNullOrWhiteSpace(EmailEntry.Text) ? null : EmailEntry.Text,
                DeviceInfo = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}"
            };

            var result = await _feedbackService.SubmitFeedbackAsync(feedback);

            if (result.Success)
            {
                await DisplayAlert("Thank You! 🙏", result.Message, "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to submit feedback: {ex.Message}", "OK");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
            SubmitButton.Text = "Submit Feedback";
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        var hasContent = !string.IsNullOrWhiteSpace(MessageEditor.Text) ||
                         FeedbackTypePicker.SelectedIndex >= 0 ||
                         _selectedRating > 0;

        if (hasContent)
        {
            var confirm = await DisplayAlert("Discard Feedback?", 
                "You have unsaved feedback. Are you sure you want to cancel?", 
                "Yes, Discard", "No, Keep Editing");
            
            if (!confirm) return;
        }

        await Navigation.PopAsync();
    }
}
