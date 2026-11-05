using LaneLog.App.ViewModels;
using LaneLog.Core.Models;

namespace LaneLog.App.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnGameSelected(object? sender, SelectionChangedEventArgs args)
    {
        if (args.CurrentSelection.FirstOrDefault() is not BowlingGame game)
        {
            return;
        }

        GameList.SelectedItem = null;
        await viewModel.EditGameCommand.ExecuteAsync(game);
    }
}
