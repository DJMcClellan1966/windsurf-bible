using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_Bible_App.Infrastructure.Services;

public class FirebaseAuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private string _apiKey;
    public event EventHandler<AuthenticationState>? StateChanged;
    public AuthenticationState CurrentState { get; private set; } = AuthenticationState.Unknown;
    public bool IsAuthenticated => CurrentState == AuthenticationState.Authenticated;

    public FirebaseAuthenticationService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
        _apiKey = _config["Firebase:ApiKey"] ?? "";
    }

    public Task<AuthResult> SignInWithGoogleAsync() => Task.FromResult(AuthResult.Failed("Google sign-in not implemented yet."));
    public Task<AuthResult> SignInWithAppleAsync() => Task.FromResult(AuthResult.Failed("Apple sign-in not implemented yet."));
    public Task SignOutAsync() => Task.CompletedTask;
    public Task<bool> SendPasswordResetAsync(string email) => Task.FromResult(false);
    public Task<bool> TryRestoreSessionAsync() => Task.FromResult(false); // Not implemented yet

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };
        var response = await _httpClient.PostAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;
            var user = new AppUser
            {
                Email = email,
                Name = data.GetPropertyOrDefault("displayName") ?? string.Empty,
                Id = data.GetPropertyOrDefault("localId") ?? string.Empty
            };
            CurrentState = AuthenticationState.Authenticated;
            StateChanged?.Invoke(this, CurrentState);
            return AuthResult.Succeeded(user, AuthProvider.Email);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return AuthResult.Failed($"Sign in failed: {error}");
        }
    }

    public async Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };
        var response = await _httpClient.PostAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;
            // Optionally update displayName
            await UpdateUserProfile(data.GetPropertyOrDefault("idToken") ?? string.Empty, displayName);
            var user = new AppUser
            {
                Email = email,
                Name = displayName,
                Id = data.GetPropertyOrDefault("localId") ?? string.Empty
            };
            CurrentState = AuthenticationState.Authenticated;
            StateChanged?.Invoke(this, CurrentState);
            return AuthResult.Succeeded(user, AuthProvider.Email, isNew: true);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return AuthResult.Failed($"Sign up failed: {error}");
        }
    }

    private async Task UpdateUserProfile(string idToken, string displayName)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}";
        var payload = new
        {
            idToken,
            displayName,
            returnSecureToken = true
        };
        await _httpClient.PostAsJsonAsync(url, payload);
    }
}

public static class JsonElementExtensions
{
    public static string? GetPropertyOrDefault(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }
}
