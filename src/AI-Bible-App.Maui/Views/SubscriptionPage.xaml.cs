using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSubscriptionDataAsync();
    }
}
