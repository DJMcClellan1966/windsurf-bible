using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AI_Bible_App.Core.Interfaces;
using System.Runtime.ExceptionServices;

namespace AI_Bible_App.Maui;

public partial class App : Application
{
	private readonly IUserService _userService;
	private readonly IModelWarmupService? _warmupService;
	private readonly IConfiguration? _configuration;

	public App(IUserService userService, IModelWarmupService? warmupService = null, IConfiguration? configuration = null)
	{
		InitializeComponent();
		_userService = userService;
		_warmupService = warmupService;
		_configuration = configuration;
		
		// Global exception handling
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			System.Diagnostics.Debug.WriteLine($"[CRASH] Unhandled exception: {ex?.Message}");
			System.Diagnostics.Debug.WriteLine($"[CRASH] Stack trace: {ex?.StackTrace}");
			LogCrashToFile(ex);
		};
		
		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"[CRASH] Unobserved task exception: {e.Exception.Message}");
			System.Diagnostics.Debug.WriteLine($"[CRASH] Stack trace: {e.Exception.StackTrace}");
			LogCrashToFile(e.Exception);
			e.SetObserved(); // Prevent crash
		};
	}
	
	private static void LogCrashToFile(Exception? ex)
	{
		try
		{
			var crashLog = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
			var message = $"[{DateTime.Now}] {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}\n\n";
			File.AppendAllText(crashLog, message);
		}
		catch { }
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = new AppShell();
		var window = new Window(shell);
		
		// Handle startup - check if we need user selection and warm up model
		window.Created += async (s, e) =>
		{
			try
			{
				// Pre-warm AI model in background if enabled (non-blocking)
				if (_warmupService != null && _configuration?["Ollama:PrewarmOnStartup"] != "false")
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await _warmupService.WarmupModelAsync();
						}
						catch (Exception warmupEx)
						{
							System.Diagnostics.Debug.WriteLine($"[App] Model warmup error (non-critical): {warmupEx.Message}");
						}
					});
				}
				
				// Try auto-login with last active user
				var autoLoggedIn = await _userService.TryAutoLoginAsync();
				
				if (!autoLoggedIn)
				{
					// No user found, show user selection page
					await Shell.Current.GoToAsync("//userselection");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[App] Startup error: {ex.Message}");
			}
		};
		
		return window;
	}
}