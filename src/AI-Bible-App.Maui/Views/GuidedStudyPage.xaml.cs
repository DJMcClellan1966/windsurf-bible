using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

[QueryProperty(nameof(PlanId), "planId")]
[QueryProperty(nameof(DayNumber), "dayNumber")]
[QueryProperty(nameof(MultiVoice), "multiVoice")]
public partial class GuidedStudyPage : ContentPage
{
    private readonly GuidedStudyViewModel _viewModel;

    private string? _planId;
    private string? _dayNumber;
    private string? _multiVoice;

    public string? PlanId
    {
        get => _planId;
        set => _planId = value;
    }

    public string? DayNumber
    {
        get => _dayNumber;
        set => _dayNumber = value;
    }

    public string? MultiVoice
    {
        get => _multiVoice;
        set => _multiVoice = value;
    }

    public GuidedStudyPage(GuidedStudyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrWhiteSpace(_planId) || string.IsNullOrWhiteSpace(_dayNumber))
            return;

        if (!int.TryParse(_dayNumber, out var day))
            return;

        var mv = true;
        if (!string.IsNullOrWhiteSpace(_multiVoice))
            bool.TryParse(_multiVoice, out mv);

        await _viewModel.InitializeAsync(_planId, day, mv);
    }
}
