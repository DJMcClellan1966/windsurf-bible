using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class WisdomCouncilPage : ContentPage
{
    public WisdomCouncilPage(WisdomCouncilViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is WisdomCouncilViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
