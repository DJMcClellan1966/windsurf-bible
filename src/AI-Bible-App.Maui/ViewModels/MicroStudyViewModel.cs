using System.Collections.ObjectModel;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AI_Bible_App.Maui.ViewModels;

public partial class MicroStudyQuestionItemViewModel : ObservableObject
{
    public MicroStudyQuestionItemViewModel(string question)
    {
        Question = question;
    }

    public string Question { get; }

    [ObservableProperty]
    private string answer = string.Empty;

    [ObservableProperty]
    private string critiqueFeedback = string.Empty;

    [ObservableProperty]
    private string critiqueVerses = string.Empty;

    [ObservableProperty]
    private bool isCritiquing;

    [ObservableProperty]
    private bool hasCritique;
}

public partial class MicroStudyViewModel : BaseViewModel
{
    private readonly IMicroStudyService _microStudyService;
    private readonly IReadingPlanRepository _readingPlanRepository;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;

    private string? _progressId;

    [ObservableProperty]
    private string planId = string.Empty;

    [ObservableProperty]
    private int dayNumber;

    [ObservableProperty]
    private string dayTitle = string.Empty;

    [ObservableProperty]
    private string passagesText = string.Empty;

    [ObservableProperty]
    private string excerptReference = string.Empty;

    [ObservableProperty]
    private string excerptText = string.Empty;

    [ObservableProperty]
    private string claim = string.Empty;

    [ObservableProperty]
    private bool multiVoiceEnabled;

    [ObservableProperty]
    private bool isDayCompleted;

    public bool CanMarkComplete => !IsBusy && !IsDayCompleted && !string.IsNullOrWhiteSpace(_progressId) && DayNumber > 0;

    [ObservableProperty]
    private ObservableCollection<MicroStudyQuestionItemViewModel> questions = new();

    public MicroStudyViewModel(
        IMicroStudyService microStudyService,
        IReadingPlanRepository readingPlanRepository,
        IUserService userService,
        IDialogService dialogService)
    {
        _microStudyService = microStudyService;
        _readingPlanRepository = readingPlanRepository;
        _userService = userService;
        _dialogService = dialogService;
        Title = "Micro-Study";
    }

    public async Task InitializeAsync(string planId, int dayNumber, bool multiVoiceEnabled)
    {
        PlanId = planId;
        DayNumber = dayNumber;
        MultiVoiceEnabled = multiVoiceEnabled;
        PersistMultiVoicePreference(multiVoiceEnabled);
        await LoadAsync();
    }

    partial void OnMultiVoiceEnabledChanged(bool value)
    {
        if (!IsBusy && !string.IsNullOrWhiteSpace(PlanId) && DayNumber > 0)
        {
            PersistMultiVoicePreference(value);
            _ = LoadAsync();
        }
    }

    private void PersistMultiVoicePreference(bool enabled)
    {
        if (string.IsNullOrWhiteSpace(PlanId))
            return;

        var userId = _userService.CurrentUser?.Id ?? "default";
        var prefKey = $"guided_multivoice:{userId}:{PlanId}";
        Preferences.Set(prefKey, enabled);
    }

    private async Task LoadProgressStateAsync()
    {
        var userId = _userService.CurrentUser?.Id ?? "default";
        var progress = await _readingPlanRepository.GetActiveProgressAsync(userId);

        if (progress == null || !string.Equals(progress.PlanId, PlanId, StringComparison.OrdinalIgnoreCase))
        {
            _progressId = null;
            IsDayCompleted = false;
            OnPropertyChanged(nameof(CanMarkComplete));
            return;
        }

        _progressId = progress.Id;
        IsDayCompleted = progress.CompletedDays.Contains(DayNumber);
        OnPropertyChanged(nameof(CanMarkComplete));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Questions.Clear();

            await LoadProgressStateAsync();

            var session = await _microStudyService.BuildSessionAsync(PlanId, DayNumber, MultiVoiceEnabled);
            DayTitle = session.DayTitle;
            PassagesText = string.Join(", ", session.Passages);
            ExcerptReference = session.ExcerptReference;
            ExcerptText = session.ExcerptText;
            Claim = session.Claim;

            foreach (var q in session.Questions)
                Questions.Add(new MicroStudyQuestionItemViewModel(q.Question));

            OnPropertyChanged(nameof(CanMarkComplete));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MicroStudy] Load error: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to load micro-study.", "OK");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanMarkComplete));
        }
    }

    [RelayCommand]
    private async Task CritiqueAnswerAsync(MicroStudyQuestionItemViewModel? item)
    {
        if (item == null)
            return;

        if (string.IsNullOrWhiteSpace(item.Answer))
        {
            await _dialogService.ShowAlertAsync("Answer needed", "Write a short answer first, then tap Critique.", "OK");
            return;
        }

        try
        {
            item.IsCritiquing = true;
            var critique = await _microStudyService.CritiqueAnswerAsync(PlanId, DayNumber, item.Question, item.Answer, MultiVoiceEnabled);
            item.CritiqueFeedback = critique.Feedback;
            item.CritiqueVerses = critique.VerseReferences.Count > 0 ? string.Join("; ", critique.VerseReferences) : "None";
            item.HasCritique = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MicroStudy] Critique error: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to critique answer.", "OK");
        }
        finally
        {
            item.IsCritiquing = false;
        }
    }

    [RelayCommand]
    private async Task MarkCompleteAsync()
    {
        if (string.IsNullOrWhiteSpace(_progressId))
            return;

        try
        {
            await _readingPlanRepository.MarkDayCompletedAsync(_progressId, DayNumber);
            await LoadProgressStateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MicroStudy] Mark complete error: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to mark day complete.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoDeeperAsync()
    {
        await Shell.Current.GoToAsync($"GuidedStudy?planId={Uri.EscapeDataString(PlanId)}&dayNumber={DayNumber}&multiVoice={MultiVoiceEnabled}");
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
