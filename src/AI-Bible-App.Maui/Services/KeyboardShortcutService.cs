using System.Windows.Input;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Service for handling keyboard shortcuts across the application
/// </summary>
public interface IKeyboardShortcutService
{
    void RegisterShortcut(string key, string modifiers, ICommand command);
    void UnregisterShortcut(string key, string modifiers);
    bool HandleKeyPress(string key, bool ctrlPressed, bool shiftPressed, bool altPressed);
}

public class KeyboardShortcutService : IKeyboardShortcutService
{
    private readonly Dictionary<string, ICommand> _shortcuts = new();

    public void RegisterShortcut(string key, string modifiers, ICommand command)
    {
        var shortcutKey = $"{modifiers}+{key}".ToLowerInvariant();
        _shortcuts[shortcutKey] = command;
    }

    public void UnregisterShortcut(string key, string modifiers)
    {
        var shortcutKey = $"{modifiers}+{key}".ToLowerInvariant();
        _shortcuts.Remove(shortcutKey);
    }

    public bool HandleKeyPress(string key, bool ctrlPressed, bool shiftPressed, bool altPressed)
    {
        var modifiers = new List<string>();
        if (ctrlPressed) modifiers.Add("ctrl");
        if (shiftPressed) modifiers.Add("shift");
        if (altPressed) modifiers.Add("alt");

        var modifierString = modifiers.Count > 0 ? string.Join("+", modifiers) : "";
        var shortcutKey = string.IsNullOrEmpty(modifierString) 
            ? key.ToLowerInvariant() 
            : $"{modifierString}+{key}".ToLowerInvariant();

        if (_shortcuts.TryGetValue(shortcutKey, out var command))
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
                return true;
            }
        }

        return false;
    }
}
