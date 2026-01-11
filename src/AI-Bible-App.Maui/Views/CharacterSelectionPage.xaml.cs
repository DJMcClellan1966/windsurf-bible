using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly CharacterSelectionViewModel _viewModel;
    
    public CharacterSelectionPage(CharacterSelectionViewModel viewModel)
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
    
    private void OnCharacterSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is BiblicalCharacter character)
        {
            // Clear selection to allow re-selection
            if (sender is CollectionView cv)
                cv.SelectedItem = null;
                
            if (_viewModel.SelectCharacterCommand.CanExecute(character))
            {
                _viewModel.SelectCharacterCommand.Execute(character);
            }
        }
    }
}
