using LaneLog.App.Services;
using LaneLog.App.ViewModels;
using LaneLog.App.Views;
using LaneLog.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace LaneLog.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if MAUI_DEVFLOW
		builder.AddMauiDevFlowAgent();
#endif

		builder.Services.AddSingleton(_ =>
			new GameDatabase(Path.Combine(FileSystem.Current.AppDataDirectory, "lanelog.db3")));
		builder.Services.AddSingleton(_ => new PhotoFileStore(FileSystem.Current.AppDataDirectory));
		builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
		builder.Services.AddSingleton<PhotoService>();
		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddTransient<GameEditorViewModel>();
		builder.Services.AddTransient<GameEditorPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
