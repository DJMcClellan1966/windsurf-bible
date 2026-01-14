namespace AI_Bible_App.Maui.Views;

public partial class AccountCreationPage : ContentPage
{
    public AccountCreationPage(ViewModels.AccountCreationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
