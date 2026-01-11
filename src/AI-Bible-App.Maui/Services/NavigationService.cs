namespace AI_Bible_App.Maui.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (Shell.Current == null)
        {
            System.Diagnostics.Debug.WriteLine($"[NAV] ERROR: Shell.Current is null!");
            return;
        }
        
        try
        {
            if (parameters != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NAV] Navigation error: {ex}");
            // Try navigation without animation as fallback
            try
            {
                await Shell.Current.GoToAsync(route, false, parameters);
            }
            catch
            {
                throw;
            }
        }
    }

    public async Task GoBackAsync()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("..");
    }
}
