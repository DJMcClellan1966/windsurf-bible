namespace AI_Bible_App.Maui.Views;

public partial class DevotionalPage : ContentPage
{
    private readonly ViewModels.DevotionalViewModel _viewModel;

    public DevotionalPage(ViewModels.DevotionalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
