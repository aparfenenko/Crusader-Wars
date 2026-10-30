using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Avalonia.Views;

/// <summary>
/// Mac path setup: every path can be typed manually (GOG, custom installs, anything
/// outside Steam) or picked with a folder browser; "Auto-fill missing" only fills empty
/// fields, never overwriting manual values. Saved to paths.json.
/// </summary>
public partial class PathSetupWindow : Window
{
    private enum PickerMode { AppBundle, File, Folder }

    private static readonly (string Key, string Label, PickerMode Mode)[] EditableFields =
    {
        ("ck3App", "CK3 app (.app bundle)", PickerMode.AppBundle),
        ("attilaApp", "Attila app (.app bundle)", PickerMode.AppBundle),
        ("ck3Documents", "CK3 documents folder", PickerMode.Folder),
        ("saveGames", "CK3 save games folder", PickerMode.Folder),
        ("ck3Log", "CK3 console_history.txt", PickerMode.File),
        ("attilaData", "Attila data folder", PickerMode.Folder),
        ("attilaLog", "Attila BattleResults_log.txt", PickerMode.File),
        ("attilaUserScript", "Attila user.script.txt", PickerMode.File),
        ("rpfmCli", "rpfm_cli executable", PickerMode.File),
        ("workspaceDir", "Workspace folder", PickerMode.Folder),
    };

    private readonly Dictionary<string, TextBox> _editors = new();

    // UTI for .app bundles: makes the macOS open panel select bundles directly.
    private static readonly FilePickerFileType AppBundleType = new("Application bundle")
    {
        AppleUniformTypeIdentifiers = new[] { "com.apple.application-bundle" }
    };

    private static readonly FilePickerFileType AnyFileType = new("All files")
    {
        Patterns = new[] { "*" }
    };

    public PathSetupWindow()
    {
        InitializeComponent();
        Opened += (_, _) => { BuildRows(); RefreshStatus(); };
    }

    private PathConfig LoadConfig() =>
        System.IO.File.Exists(PathConfig.DefaultConfigPath)
            ? PathConfig.Load()
            : new MacPathDetector(CrusaderWarsEnvironment.Logger).Detect();

    private void BuildRows(PathConfig? config = null)
    {
        config ??= LoadConfig();
        RowsPanel.Children.Clear();
        _editors.Clear();

        foreach (var (key, label, mode) in EditableFields)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var text = new TextBlock
            {
                Text = label,
                Width = 240,
                VerticalAlignment = VerticalAlignment.Center
            };
            var editor = new TextBox
            {
                Text = (string?)typeof(PathConfig).GetProperty(PascalName(key))?.GetValue(config) ?? "",
                Width = 480
            };
            _editors[key] = editor;
            var browse = new Button { Content = "Browse…" };
            browse.Click += async (_, _) => await BrowseAsync(editor, key, mode);
            row.Children.Add(text);
            row.Children.Add(editor);
            row.Children.Add(browse);
            RowsPanel.Children.Add(row);
        }
    }

    private async Task BrowseAsync(TextBox editor, string key, PickerMode mode)
    {
        string? selected = mode switch
        {
            // No type filter: with a filter Avalonia classifies the returned .app bundle as
            // a folder and drops it from the results, leaving the field empty. A plain file
            // panel returns the bundle as a file item.
            PickerMode.AppBundle => await PickFile("Select the application bundle (.app)", filter: null),
            PickerMode.File => await PickFile($"Select {key}", AnyFileType),
            PickerMode.Folder => await PickFolder($"Select {key}"),
            _ => null
        };
        if (selected is not null)
        {
            editor.Text = selected;
        }
        LogSelection(key, mode, selected);
    }

    private void LogSelection(string key, PickerMode mode, string? selected)
    {
        string line = selected is null
            ? $"[WRN] Path Setup: browse for '{key}' returned nothing (cancelled or dropped by the picker)."
            : $"[INF] Path Setup: '{key}' = {selected}";
        (DataContext as ViewModels.MainWindowViewModel)?.AppendLog(line);
        try { CrusaderWarsEnvironment.Logger.Warn(line); } catch { /* gateway not ready */ }
    }

    private async Task<string?> PickFile(string title, FilePickerFileType? filter)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filter is null ? null : new[] { filter }
        });
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private async Task<string?> PickFolder(string title)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private static string PascalName(string jsonName) =>
        char.ToUpperInvariant(jsonName[0]) + jsonName[1..];

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var config = LoadConfig();
        foreach (var (key, _, _) in EditableFields)
        {
            config.SetByName(key, _editors[key].Text ?? "");
        }
        config.Save();

        Hint.Text = $"Saved {PathConfig.DefaultConfigPath}. Restart the app to apply the new paths.";
        RefreshStatus(config);
    }

    private void OnAutoFill(object? sender, RoutedEventArgs e)
    {
        var detected = new MacPathDetector(CrusaderWarsEnvironment.Logger).Detect();
        foreach (var (key, _, _) in EditableFields)
        {
            var editor = _editors[key];
            if (string.IsNullOrWhiteSpace(editor.Text))
            {
                editor.Text = (string?)typeof(PathConfig).GetProperty(PascalName(key))?.GetValue(detected) ?? "";
            }
        }
        Hint.Text = "Filled empty fields from Steam detection. Review and press 'Save paths'.";
    }

    private void RefreshStatus(PathConfig? config = null)
    {
        config ??= LoadConfig();
        StatusPanel.Children.Clear();

        foreach (var item in CreateDetector().Describe(config))
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
            StatusPanel.Children.Add(row);
        }
    }

    private static MacPathDetector CreateDetector() =>
        new(CrusaderWarsEnvironment.Logger);
}
