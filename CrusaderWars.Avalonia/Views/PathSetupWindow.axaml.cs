using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Avalonia.Views;

/// <summary>
/// Mac path setup, modeled on the Windows LinuxSetupWizard's detection step:
/// detects CK3/Attila paths, shows the OK/MISSING table and writes paths.json.
/// </summary>
public partial class PathSetupWindow : Window
{
    public PathSetupWindow()
    {
        InitializeComponent();
        Opened += (_, _) => RefreshRows();
    }

    private MacPathDetector CreateDetector() =>
        new(CrusaderWarsEnvironment.Logger);

    private void RefreshRows()
    {
        var detector = CreateDetector();
        var config = System.IO.File.Exists(PathConfig.DefaultConfigPath)
            ? PathConfig.Load()
            : detector.Detect();

        RowsPanel.Children.Clear();
        foreach (var item in detector.Describe(config))
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var mark = new TextBlock
            {
                Text = item.Found ? "OK" : "MISSING",
                Width = 80,
                Foreground = item.Found ? Brushes.Green : Brushes.OrangeRed
            };
            var label = new TextBlock { Text = item.Label, Width = 220 };
            var path = new TextBlock { Text = item.Path };
            row.Children.Add(mark);
            row.Children.Add(label);
            row.Children.Add(path);
            RowsPanel.Children.Add(row);
        }
    }

    private void OnDetectSave(object? sender, RoutedEventArgs e)
    {
        var detector = CreateDetector();
        var detected = detector.Detect();
        detected.Save();
        Hint.Text = $"Saved {PathConfig.DefaultConfigPath}. Restart the app to apply the new paths.";
        RefreshRows();
    }
}
