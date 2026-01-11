using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class MultiCharacterSelectionPage : ContentPage
{
    private readonly MultiCharacterSelectionViewModel _viewModel;

    public MultiCharacterSelectionPage(MultiCharacterSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private void OnCharacterTapped(object sender, EventArgs e)
    {
        if (sender is View view && view.BindingContext is SelectableCharacter character)
        {
            _viewModel.ToggleCharacterCommand.Execute(character);
        }
    }
}
