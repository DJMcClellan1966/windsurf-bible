namespace AI_Bible_App.Maui.Views;

public partial class EditNotesPage : ContentPage
{
    public string ReflectionTitle { get; set; } = "Reflection";
    public string Notes { get; set; } = string.Empty;
    
    private readonly TaskCompletionSource<string?> _tcs = new();
    
    public EditNotesPage(string title, string? currentNotes)
    {
        InitializeComponent();
        ReflectionTitle = title;
        Notes = currentNotes ?? string.Empty;
        BindingContext = this;
    }

    public Task<string?> GetResultAsync() => _tcs.Task;

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(NotesEditor.Text);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }
}
