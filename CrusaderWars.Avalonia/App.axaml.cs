using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CrusaderWars.Avalonia.ViewModels;
using CrusaderWars.Avalonia.Views;
using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            var sinkLogger = new SinkLogger(viewModel);

            try
            {
                var (paths, detector) = RuntimeBootstrap.Initialize(sinkLogger, new ConsoleDialogService());

                // Re-initialize with the final composite logger now that the workspace
                // (and its data/ folder) is known.
                var fileLogger = new FileAppLogger(System.IO.Path.Combine(paths.WorkspaceDir, "data"));
                CrusaderWarsEnvironment.Initialize(
                    new AppPathsGamePaths(paths),
                    new ConsoleDialogService(),
                    new CompositeLogger(fileLogger, sinkLogger));

                viewModel.Initialize(paths, detector);
            }
            catch (Exception ex)
            {
                viewModel.AppendLog($"[ERR] Startup failed: {ex.Message}");
                viewModel.AppendLog("Open 'Path Setup' and make sure the workspace is complete (see detect).");
                viewModel.StartupFailed = true;
            }

            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Logs straight into the UI log before the workspace (and its log folder) exists.</summary>
    private sealed class SinkLogger : IAppLogger
    {
        private readonly MainWindowViewModel _viewModel;

        public SinkLogger(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Debug(string message) { }
        public void Info(string message) => _viewModel.AppendLog($"[INF] {message}");
        public void Warn(string message) => _viewModel.AppendLog($"[WRN] {message}");
        public void Error(string message) => _viewModel.AppendLog($"[ERR] {message}");
        public void Error(Exception exception) => _viewModel.AppendLog($"[ERR] {exception.GetType().Name}: {exception.Message}");
    }

    private sealed class CompositeLogger : IAppLogger
    {
        private readonly IAppLogger _file;
        private readonly IAppLogger _sink;

        public CompositeLogger(IAppLogger file, IAppLogger sink)
        {
            _file = file;
            _sink = sink;
        }

        public void Debug(string message) => _file.Debug(message);
        public void Info(string message) { _file.Info(message); _sink.Info(message); }
        public void Warn(string message) { _file.Warn(message); _sink.Warn(message); }
        public void Error(string message) { _file.Error(message); _sink.Error(message); }
        public void Error(Exception exception) { _file.Error(exception); _sink.Error(exception); }
    }
}
