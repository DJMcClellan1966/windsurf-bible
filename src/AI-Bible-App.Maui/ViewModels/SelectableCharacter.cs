using AI_Bible_App.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AI_Bible_App.Maui.ViewModels;

/// <summary>
/// Wrapper class for BiblicalCharacter that adds selection state for multi-character selection UI
/// </summary>
public partial class SelectableCharacter : ObservableObject
{
    [ObservableProperty]
    private BiblicalCharacter _character = null!;
    
    [ObservableProperty]
    private bool _isSelected;
    
    public SelectableCharacter(BiblicalCharacter character)
    {
        Character = character;
        IsSelected = false;
    }
}
