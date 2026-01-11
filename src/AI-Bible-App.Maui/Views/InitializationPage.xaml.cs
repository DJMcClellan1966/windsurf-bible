using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class InitializationPage : ContentPage
{
	private readonly InitializationViewModel _viewModel;

	public InitializationPage(InitializationViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		
		await _viewModel.InitializeAsync();
		
		// Navigate to main app if successful
		if (!_viewModel.HasError)
		{
			await Shell.Current.GoToAsync("//characters");
		}
	}

	private async void OnContinueClicked(object sender, EventArgs e)
	{
		// Allow user to continue without full initialization
		await Shell.Current.GoToAsync("//characters");
	}
}
