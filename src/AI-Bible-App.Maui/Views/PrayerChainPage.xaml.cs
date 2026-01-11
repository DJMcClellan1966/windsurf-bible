using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class PrayerChainPage : ContentPage
{
    public PrayerChainPage(PrayerChainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PrayerChainViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
