using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaneLog.Core.Models;
using LaneLog.Core.Services;
using Microsoft.Extensions.Logging;
using SQLite;

namespace LaneLog.App.ViewModels;

public partial class GameEditorViewModel : ObservableObject, IQueryAttributable
{
    private const string AlleyChoiceError = "Choose an alley or select No alley.";
    private const string NewAlleyError = "Enter a new alley name or select No alley.";

    private readonly GameDatabase database;
    private readonly ILogger<GameEditorViewModel> logger;
    private bool initialized;
    private bool invalidGameId;
    private int? requestedGameId;
    private int gameId;
    private string? photoRelativePath;

    public GameEditorViewModel(GameDatabase database, ILogger<GameEditorViewModel> logger)
    {
        this.database = database;
        this.logger = logger;
    }

    public ObservableCollection<AlleyChoice> AlleyChoices { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyCanExecuteChangedFor(nameof(SaveGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(SaveGameCommand))]
    private bool isReady;

    [ObservableProperty]
    private string pageTitle = "Add game";

    [ObservableProperty]
    private DateTime playedOn = DateTime.Today;

    [ObservableProperty]
    private string scoreText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewAlley))]
    private AlleyChoice? selectedAlley;

    [ObservableProperty]
    private string newAlleyName = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    public bool CanEdit => !IsBusy && IsReady;

    public bool CanCancel => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsNewAlley => SelectedAlley?.IsNew == true;

    public bool IsLeaving { get; private set; }

    partial void OnScoreTextChanged(string value)
    {
        if (ErrorMessage == GameValidator.ScoreError)
        {
            ErrorMessage = string.Empty;
        }
    }

    partial void OnSelectedAlleyChanged(AlleyChoice? value)
    {
        if (ErrorMessage is AlleyChoiceError or NewAlleyError)
        {
            ErrorMessage = string.Empty;
        }
    }

    partial void OnNewAlleyNameChanged(string value)
    {
        if (ErrorMessage == NewAlleyError && !string.IsNullOrWhiteSpace(value))
        {
            ErrorMessage = string.Empty;
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value))
        {
            return;
        }

        PageTitle = "Edit game";
        if (!int.TryParse(value?.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
        {
            invalidGameId = true;
            return;
        }

        requestedGameId = id;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (invalidGameId)
        {
            ErrorMessage = "This game link is invalid. Go back to your history.";
            return;
        }

        IsBusy = true;
        try
        {
            BowlingGame? game = null;
            if (requestedGameId is int id)
            {
                game = await database.GetGameAsync(id);
                if (game is null)
                {
                    ErrorMessage = "That game could not be found. Go back to your history.";
                    return;
                }
            }

            var names = await database.GetAlleyNamesAsync();
            var currentName = game?.AlleyName?.Trim() ?? string.Empty;
            if (currentName.Length > 0)
            {
                var existingIndex = names.FindIndex(name =>
                    string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    names[existingIndex] = currentName;
                }
                else
                {
                    names.Add(currentName);
                    names.Sort(StringComparer.CurrentCultureIgnoreCase);
                }
            }

            var noAlley = new AlleyChoice("No alley", null, false);
            AlleyChoices.Add(noAlley);
            AlleyChoice? currentChoice = null;
            foreach (var name in names)
            {
                var choice = new AlleyChoice($"Saved: {name}", name, false);
                AlleyChoices.Add(choice);
                if (string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase))
                {
                    currentChoice = choice;
                }
            }

            AlleyChoices.Add(new AlleyChoice("Add a new alley...", null, true));
            SelectedAlley = currentChoice ?? noAlley;

            if (game is not null)
            {
                gameId = game.GameId;
                PlayedOn = game.PlayedOn;
                ScoreText = game.TotalScore.ToString(CultureInfo.InvariantCulture);
                Notes = game.Notes;
                photoRelativePath = game.PhotoRelativePath;
            }

            IsReady = true;
        }
        catch (Exception exception) when (exception is SQLiteException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogError(exception, "Could not load the game or saved alleys.");
            ErrorMessage = requestedGameId is null
                ? "Couldn't load saved alleys. Go back and try again."
                : "Couldn't load this game or saved alleys. Go back and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task SaveGameAsync()
    {
        if (!CanEdit)
        {
            return;
        }

        if (!GameValidator.TryParseScore(ScoreText, out var score))
        {
            ErrorMessage = GameValidator.ScoreError;
            return;
        }

        var choice = SelectedAlley;
        if (choice is null)
        {
            ErrorMessage = AlleyChoiceError;
            return;
        }

        var alleyName = choice.IsNew ? NewAlleyName?.Trim() ?? string.Empty : choice.SavedName ?? string.Empty;
        if (choice.IsNew && alleyName.Length == 0)
        {
            ErrorMessage = NewAlleyError;
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var game = new BowlingGame
            {
                GameId = gameId,
                PlayedOn = PlayedOn.Date,
                TotalScore = score,
                AlleyName = alleyName,
                Notes = Notes?.Trim() ?? string.Empty,
                PhotoRelativePath = photoRelativePath
            };

            try
            {
                await database.SaveGameAsync(game);
            }
            catch (Exception exception) when (exception is SQLiteException or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                logger.LogError(exception, "Could not save the game.");
                ErrorMessage = "Couldn't save your game. Your entries are still here; try again.";
                return;
            }

            gameId = game.GameId;
            PageTitle = "Edit game";
            await ReturnToHistoryAsync("Game saved, but couldn't return to history. Use Back.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task CancelAsync()
    {
        if (!CanCancel)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await ReturnToHistoryAsync("Couldn't return to history. Try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReturnToHistoryAsync(string failureMessage)
    {
        try
        {
            IsLeaving = true;
            var shell = Shell.Current ?? throw new InvalidOperationException("The app navigation is unavailable.");
            await shell.GoToAsync("..");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            logger.LogError(exception, "Could not return to game history.");
            ErrorMessage = failureMessage;
        }
        finally
        {
            IsLeaving = false;
        }
    }
}
