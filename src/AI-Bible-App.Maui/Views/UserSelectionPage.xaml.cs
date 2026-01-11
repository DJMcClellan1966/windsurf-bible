namespace AI_Bible_App.Maui.Views;

public partial class UserSelectionPage : ContentPage
{
    public UserSelectionPage(ViewModels.UserSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ViewModels.UserSelectionViewModel vm)
        {
            await vm.LoadUsersAsync();
        }
    }
}
