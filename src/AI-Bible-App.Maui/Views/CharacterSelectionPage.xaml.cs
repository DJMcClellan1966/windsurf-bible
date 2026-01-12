using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.ViewModels;

namespace AI_Bible_App.Maui.Views;

public partial class CharacterSelectionPage : ContentPage
{
    private readonly CharacterSelectionViewModel _viewModel;
    private bool _isCarouselView = true;
    
    public CharacterSelectionPage(CharacterSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Connect carousel to indicator
        CharacterIndicator.SetBinding(IndicatorView.ItemsSourceProperty, 
            new Binding(nameof(CharacterSelectionViewModel.Characters), source: viewModel));
        CharacterCarousel.IndicatorView = CharacterIndicator;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
    
    private void OnCharacterSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnCharacterSelected fired");
            
            var collectionView = sender as CollectionView;
            var character = e.CurrentSelection.FirstOrDefault() as BiblicalCharacter;
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Character: {character?.Name ?? "null"}");
            
            // Clear selection immediately to allow re-selection
            if (collectionView != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Clearing selection");
                collectionView.SelectedItem = null;
            }
            
            // Execute command on main thread with a slight delay to ensure selection is cleared
            if (character != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Dispatching command for {character.Name}");
                
                Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Inside Dispatcher.Dispatch");
                        
                        if (_viewModel.SelectCharacterCommand.CanExecute(character))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing command");
                            await _viewModel.SelectCharacterCommand.ExecuteAsync(character);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Command cannot execute");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Command execution failed: {ex}");
                        await DisplayAlert("Error", $"Failed to select character: {ex.Message}", "OK");
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Character is null, skipping");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] OnCharacterSelected crashed: {ex}");
        }
    }
    
    private void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
    {
        // Provide haptic feedback on character change
        if (e.CurrentItem is BiblicalCharacter character)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
    }
    
    private async void OnCharacterCardTapped(object sender, TappedEventArgs e)
    {
        if (CharacterCarousel.CurrentItem is BiblicalCharacter character)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            
            try
            {
                if (_viewModel.SelectCharacterCommand.CanExecute(character))
                {
                    await _viewModel.SelectCharacterCommand.ExecuteAsync(character);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnCharacterCardTapped failed: {ex}");
                await DisplayAlert("Error", $"Failed to select character: {ex.Message}", "OK");
            }
        }
    }
    
    private void OnCardsViewClicked(object sender, EventArgs e)
    {
        if (_isCarouselView) return;
        
        _isCarouselView = true;
        CarouselContainer.IsVisible = true;
        ListContainer.IsVisible = false;
        
        // Update button styles
        CardsViewButton.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
        CardsViewButton.TextColor = Colors.White;
        ListViewButton.BackgroundColor = (Color)Application.Current!.Resources["Gray300"];
        ListViewButton.TextColor = (Color)Application.Current!.Resources["Gray800"];
        
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    }
    
    private void OnListViewClicked(object sender, EventArgs e)
    {
        if (!_isCarouselView) return;
        
        _isCarouselView = false;
        CarouselContainer.IsVisible = false;
        ListContainer.IsVisible = true;
        
        // Update button styles
        ListViewButton.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
        ListViewButton.TextColor = Colors.White;
        CardsViewButton.BackgroundColor = (Color)Application.Current!.Resources["Gray300"];
        CardsViewButton.TextColor = (Color)Application.Current!.Resources["Gray800"];
        
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    }
    
    private async void OnSwipeChatInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is BiblicalCharacter character)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            
            try
            {
                if (_viewModel.SelectCharacterCommand.CanExecute(character))
                {
                    await _viewModel.SelectCharacterCommand.ExecuteAsync(character);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] OnSwipeChatInvoked failed: {ex}");
                await DisplayAlert("Error", $"Failed to select character: {ex.Message}", "OK");
            }
        }
    }
    
    private async void OnSwipeInfoInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is BiblicalCharacter character)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            await DisplayAlert(
                character.Name,
                $"{character.Title}\n\n{character.Description}\n\nEra: {character.Era}",
                "Close");
        }
    }
    
    private async void OnBibleReaderClicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        await Shell.Current.GoToAsync("///BibleReader");
    }
    
    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        await Shell.Current.GoToAsync("///HistoryDashboard");
    }
    
    private async void OnCreateCharacterClicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        await Shell.Current.GoToAsync("///CreateCharacter");
    }
}
