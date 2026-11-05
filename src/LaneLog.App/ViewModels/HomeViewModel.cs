using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaneLog.Core.Models;
using LaneLog.Core.Services;
using Microsoft.Extensions.Logging;
using SQLite;

namespace LaneLog.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly GameDatabase database;
    private readonly ILogger<HomeViewModel> logger;

    public HomeViewModel(GameDatabase database, ILogger<HomeViewModel> logger)
    {
        this.database = database;
        this.logger = logger;
    }

    public ObservableCollection<BowlingGame> Games { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditGameCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string gameCount = "--";

    [ObservableProperty]
    private string averageScore = "--";

    [ObservableProperty]
    private string bestScore = "--";

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var games = await database.GetGamesAsync();
            var statistics = GameStatisticsCalculator.Calculate(games);

            Games.Clear();
            foreach (var game in games)
            {
                Games.Add(game);
            }

            GameCount = statistics.Count.ToString(CultureInfo.CurrentCulture);
            AverageScore = statistics.Average?.ToString("F1", CultureInfo.CurrentCulture) ?? "--";
            BestScore = statistics.Best?.ToString(CultureInfo.CurrentCulture) ?? "--";
            IsEmpty = games.Count == 0;
        }
        catch (Exception exception) when (exception is SQLiteException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogError(exception, "Could not load saved games.");
            IsEmpty = false;
            ErrorMessage = "Couldn't load your games. Try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task AddGameAsync() => OpenEditorAsync(null);

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task EditGameAsync(BowlingGame? game)
    {
        if (game is null || game.GameId <= 0)
        {
            ErrorMessage = "Select a saved game to edit.";
            return Task.CompletedTask;
        }

        return OpenEditorAsync(game.GameId);
    }

    private async Task OpenEditorAsync(int? gameId)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var shell = Shell.Current ?? throw new InvalidOperationException("The app navigation is unavailable.");
            var route = gameId is null ? "game-editor" : $"game-editor?id={gameId.Value}";
            await shell.GoToAsync(route);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            logger.LogError(exception, "Could not open the game editor.");
            ErrorMessage = "Couldn't open the game. Try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
