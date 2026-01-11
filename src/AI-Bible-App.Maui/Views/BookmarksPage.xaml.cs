namespace AI_Bible_App.Maui.Views;

public partial class BookmarksPage : ContentPage
{
    private readonly ViewModels.BookmarksViewModel _viewModel;

    public BookmarksPage(ViewModels.BookmarksViewModel viewModel)
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
