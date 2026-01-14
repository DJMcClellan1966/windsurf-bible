namespace AI_Bible_App.Maui.Views;

public partial class ExistingLoginPage : ContentPage
{
    public ExistingLoginPage(ViewModels.ExistingLoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
