namespace AI_Bible_App.Maui.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(ViewModels.OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
