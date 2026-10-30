using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrusaderWars.Avalonia.ViewModels;

namespace CrusaderWars.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var viewModel = ViewModel;
        viewModel.Log.CollectionChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogBox.Text = string.Join(Environment.NewLine, viewModel.Log);
                LogBox.CaretIndex = LogBox.Text?.Length ?? 0;
                StatusText.Text = viewModel.Status;
                StartButton.IsEnabled = !viewModel.IsBusy && !viewModel.StartupFailed;
            });
        };
        StatusText.Text = viewModel.Status;
    }

    private async void OnStartBattle(object? sender, RoutedEventArgs e)
    {
        await ViewModel.StartBattleAsync();
    }

    private void OnOptions(object? sender, RoutedEventArgs e)
    {
        var optionsWindow = new OptionsWindow { DataContext = ViewModel };
        optionsWindow.Show(this);
    }

    private void OnPathSetup(object? sender, RoutedEventArgs e)
    {
        var pathWindow = new PathSetupWindow { DataContext = ViewModel };
        pathWindow.Show(this);
    }
}
