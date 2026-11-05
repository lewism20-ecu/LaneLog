using LaneLog.App.ViewModels;
using LaneLog.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LaneLog.App;

public partial class AppShell : Shell
{
	public AppShell(IServiceProvider services)
	{
		InitializeComponent();

		var home = new Tab { Title = "Home", Route = "home" };
		home.Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "history",
			ContentTemplate = new DataTemplate(() => services.GetRequiredService<HomePage>())
		});

		var tabs = new TabBar { Route = "root" };
		tabs.Items.Add(home);
		Items.Add(tabs);

		Routing.RegisterRoute("game-editor", typeof(GameEditorPage));
	}

	protected override void OnNavigating(ShellNavigatingEventArgs args)
	{
		base.OnNavigating(args);

		if (args.CanCancel
			&& CurrentPage?.BindingContext is GameEditorViewModel editor
			&& editor.IsBusy
			&& !editor.IsLeaving)
		{
			args.Cancel();
		}
	}
}
