using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class PrayerPage : ContentPage
{
    private readonly PrayerViewModel _viewModel;
    
    public PrayerPage(PrayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
    
    private void OnPrayerTapped(object sender, TappedEventArgs e)
    {
        if (sender is Element element && element.BindingContext is Prayer prayer)
        {
            if (_viewModel.ViewPrayerCommand.CanExecute(prayer))
            {
                _viewModel.ViewPrayerCommand.Execute(prayer);
            }
        }
    }
}
