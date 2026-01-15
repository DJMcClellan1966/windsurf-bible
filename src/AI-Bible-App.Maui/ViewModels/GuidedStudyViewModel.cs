using System.Linq;
using System.Collections.ObjectModel;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AI_Bible_App.Maui.ViewModels;

public partial class GuidedStudyStepGroup : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<GuidedStudyStep> Items { get; set; } = new();

    [ObservableProperty]
    private bool isExpanded;
}

public partial class GuidedStudyViewModel : BaseViewModel
{
    private readonly IGuidedStudyService _guidedStudyService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string planId = string.Empty;

    [ObservableProperty]
    private int dayNumber;

    [ObservableProperty]
    private string dayTitle = string.Empty;

    [ObservableProperty]
    private string passagesText = string.Empty;

    [ObservableProperty]
    private bool multiVoiceEnabled;

    [ObservableProperty]
    private ObservableCollection<GuidedStudyStepGroup> stepGroups = new();

    public GuidedStudyViewModel(IGuidedStudyService guidedStudyService, IDialogService dialogService)
    {
        _guidedStudyService = guidedStudyService;
        _dialogService = dialogService;
        Title = "Guided Study";
    }

    [RelayCommand]
    private void ToggleGroup(GuidedStudyStepGroup group)
    {
        if (group == null)
            return;

        group.IsExpanded = !group.IsExpanded;
    }

    public async Task InitializeAsync(string planId, int dayNumber, bool multiVoiceEnabled)
    {
        PlanId = planId;
        DayNumber = dayNumber;
        MultiVoiceEnabled = multiVoiceEnabled;
        await LoadAsync();
    }

    partial void OnMultiVoiceEnabledChanged(bool value)
    {
        if (!IsBusy && !string.IsNullOrWhiteSpace(PlanId) && DayNumber > 0)
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StepGroups.Clear();

            var session = await _guidedStudyService.BuildSessionAsync(PlanId, DayNumber, MultiVoiceEnabled);

            DayTitle = session.DayTitle;
            PassagesText = string.Join(", ", session.Passages);

            var groups = session.Steps
                .GroupBy(s => s.Type == GuidedStudyStepType.Passage ? "Passage" : (s.CharacterName ?? "Guide"))
                .ToList();

            foreach (var g in groups)
            {
                var group = new GuidedStudyStepGroup
                {
                    Title = g.Key,
                    IsExpanded = string.Equals(g.Key, "Passage", StringComparison.OrdinalIgnoreCase)
                };

                foreach (var step in g)
                    group.Items.Add(step);

                StepGroups.Add(group);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GuidedStudy] Load error: {ex.Message}");
            await _dialogService.ShowAlertAsync("Error", "Failed to load guided study.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
