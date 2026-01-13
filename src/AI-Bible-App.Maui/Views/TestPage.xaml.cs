namespace AI_Bible_App.Maui.Views;

public partial class TestPage : ContentPage
{
    public TestPage()
    {
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//characters");
    }
}
