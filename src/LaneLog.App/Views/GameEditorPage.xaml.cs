using LaneLog.App.ViewModels;

namespace LaneLog.App.Views;

public partial class GameEditorPage : ContentPage
{
    private readonly GameEditorViewModel viewModel;

    public GameEditorPage(GameEditorViewModel viewModel)
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

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        await viewModel.DiscardUncommittedPhotoAsync();
    }
}
