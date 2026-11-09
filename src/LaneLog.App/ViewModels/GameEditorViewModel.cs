using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaneLog.App.Services;
using LaneLog.Core.Models;
using LaneLog.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace LaneLog.App.ViewModels;

public partial class GameEditorViewModel : ObservableObject, IQueryAttributable
{
    private const string AlleyChoiceError = "Choose an alley or select No alley.";
    private const string NewAlleyError = "Enter a new alley name or select No alley.";
    private const string SelectedPhotoError = "The selected photo is unavailable. Choose it again or remove it.";

    private readonly GameDatabase database;
    private readonly PhotoService photos;
    private readonly PhotoFileStore photoFiles;
    private readonly ILogger<GameEditorViewModel> logger;
    private readonly HashSet<string> photosToClean = new(StringComparer.Ordinal);
    private bool initialized;
    private bool invalidGameId;
    private int? requestedGameId;
    private int gameId;
    private string? savedPhotoRelativePath;

    public GameEditorViewModel(
        GameDatabase database,
        PhotoService photos,
        PhotoFileStore photoFiles,
        ILogger<GameEditorViewModel> logger)
    {
        this.database = database;
        this.photos = photos;
        this.photoFiles = photoFiles;
        this.logger = logger;
    }

    public ObservableCollection<AlleyChoice> AlleyChoices { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyCanExecuteChangedFor(nameof(SaveGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChoosePhotoCommand))]
    [NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(SaveGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChoosePhotoCommand))]
    [NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
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
    [NotifyPropertyChangedFor(nameof(HasPhotoReference))]
    private string? photoRelativePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhotoPreview))]
    private string? photoPreviewPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhotoMessage))]
    private string photoMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    public bool CanEdit => !IsBusy && IsReady;

    public bool CanCancel => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasPhotoReference => PhotoRelativePath is not null;

    public bool HasPhotoPreview => PhotoPreviewPath is not null;

    public bool HasPhotoMessage => !string.IsNullOrEmpty(PhotoMessage);

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
                savedPhotoRelativePath = game.PhotoRelativePath;
                SetPhoto(savedPhotoRelativePath);
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
    private Task ChoosePhotoAsync() => ChangePhotoAsync(capture: false);

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task TakePhotoAsync() => ChangePhotoAsync(capture: true);

    private async Task ChangePhotoAsync(bool capture)
    {
        if (!CanEdit)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var selectedPath = capture ? await photos.CaptureAsync() : await photos.PickAsync();
            if (selectedPath is null)
            {
                if (!HasPhotoReference || HasPhotoPreview)
                {
                    PhotoMessage = "No photo was chosen. Your current photo is unchanged.";
                }

                return;
            }

            var previousPath = PhotoRelativePath;
            SetPhoto(selectedPath);
            if (HasPhotoPreview)
            {
                if (ErrorMessage == SelectedPhotoError)
                {
                    ErrorMessage = string.Empty;
                }

                PhotoMessage = "Photo ready. Save the game to keep it.";
            }

            QueueUncommittedPhoto(previousPath);
            if (!await CleanupPhotosAsync())
            {
                PhotoMessage = HasPhotoPreview
                    ? "Photo ready, but an unused copy couldn't be removed. Saving will retry."
                    : "The selected photo is unavailable, and an unused copy couldn't be removed.";
            }
        }
        catch (OperationCanceledException)
        {
            if (!HasPhotoReference || HasPhotoPreview)
            {
                PhotoMessage = "No photo was chosen. Your current photo is unchanged.";
            }
        }
        catch (PhotoSelectionLimitException exception)
        {
            logger.LogWarning(exception, "More than one photo was selected.");
            PhotoMessage = "Choose only one score photo at a time.";
        }
        catch (PermissionException exception)
        {
            logger.LogWarning(exception, "Photo access was denied.");
            PhotoMessage = "Photo access was denied. Check device permissions or choose another photo.";
        }
        catch (FeatureNotSupportedException exception)
        {
            logger.LogWarning(exception, "Photo capture or selection is unavailable.");
            PhotoMessage = "That photo option isn't available on this device. Try choosing a photo instead.";
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException
            or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException
            or ArgumentException or NotSupportedException)
        {
            logger.LogError(exception, "Could not choose or copy a score photo.");
            PhotoMessage = "Couldn't add that photo. Your game is unchanged; try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task RemovePhotoAsync()
    {
        if (!CanEdit)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var previousPath = PhotoRelativePath;
            SetPhoto(null);
            QueueUncommittedPhoto(previousPath);
            if (ErrorMessage == SelectedPhotoError)
            {
                ErrorMessage = string.Empty;
            }

            PhotoMessage = savedPhotoRelativePath is null
                ? "Photo removed from this draft."
                : "Photo will be removed when you save the game.";

            if (!await CleanupPhotosAsync())
            {
                PhotoMessage = "Photo removed from the draft, but an unused copy couldn't be removed. Saving will retry.";
            }
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

        if (PhotoRelativePath is not null
            && !string.Equals(PhotoRelativePath, savedPhotoRelativePath, StringComparison.Ordinal)
            && (PhotoPreviewPath is null || !File.Exists(PhotoPreviewPath)))
        {
            ErrorMessage = SelectedPhotoError;
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
                PhotoRelativePath = PhotoRelativePath
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
            var previousSavedPhoto = savedPhotoRelativePath;
            savedPhotoRelativePath = PhotoRelativePath;
            if (previousSavedPhoto is not null
                && !string.Equals(previousSavedPhoto, savedPhotoRelativePath, StringComparison.Ordinal))
            {
                photosToClean.Add(previousSavedPhoto);
            }

            if (!await CleanupPhotosAsync())
            {
                ErrorMessage = "Game saved, but an old photo couldn't be removed. Save again to retry.";
                return;
            }

            await ReturnToHistoryAsync("Game saved, but couldn't return to history. Use Back.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DiscardUncommittedPhotoAsync()
    {
        QueueUncommittedPhoto(PhotoRelativePath);
        await CleanupPhotosAsync();
    }

    private void SetPhoto(string? relativePath)
    {
        PhotoRelativePath = relativePath;
        PhotoPreviewPath = null;
        PhotoMessage = string.Empty;
        if (relativePath is null)
        {
            return;
        }

        try
        {
            var fullPath = photoFiles.GetFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                PhotoPreviewPath = fullPath;
                return;
            }

            logger.LogWarning("A game photo is missing from app storage.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException
            or IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            logger.LogWarning(exception, "A game photo path is unavailable.");
        }

        PhotoMessage = "This photo is unavailable. Choose a replacement or remove the attachment.";
    }

    private void QueueUncommittedPhoto(string? relativePath)
    {
        if (relativePath is not null
            && !string.Equals(relativePath, savedPhotoRelativePath, StringComparison.Ordinal))
        {
            photosToClean.Add(relativePath);
        }
    }

    private async Task<bool> CleanupPhotosAsync()
    {
        var succeeded = true;
        foreach (var relativePath in photosToClean.ToArray())
        {
            try
            {
                photoFiles.GetFullPath(relativePath);
            }
            catch (Exception exception) when (exception is InvalidDataException or ArgumentException or NotSupportedException)
            {
                logger.LogWarning(exception, "An invalid photo reference was not deleted.");
                photosToClean.Remove(relativePath);
                continue;
            }

            try
            {
                if (!await database.IsPhotoInUseAsync(relativePath))
                {
                    photoFiles.Delete(relativePath);
                }

                photosToClean.Remove(relativePath);
            }
            catch (Exception exception) when (exception is SQLiteException or IOException
                or UnauthorizedAccessException or System.Security.SecurityException
                or InvalidOperationException or ArgumentException or NotSupportedException)
            {
                logger.LogError(exception, "Could not remove an unused app-owned photo.");
                succeeded = false;
            }
        }

        return succeeded;
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
