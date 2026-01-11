using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class OfflineModelsPage : ContentPage
{
    public OfflineModelsPage(OfflineModelsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is OfflineModelsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
