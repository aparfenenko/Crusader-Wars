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
    private static readonly (string Key, string Label, string BrowseTitle)[] EditableFields =
    {
        ("ck3App", "CK3 app (.app bundle)", "Select the Crusader Kings III .app bundle"),
        ("attilaApp", "Attila app (.app bundle)", "Select the Total War: ATTILA .app bundle"),
        ("ck3Documents", "CK3 documents folder", "Select the 'Crusader Kings III' documents folder"),
        ("saveGames", "CK3 save games folder", "Select the CK3 'save games' folder"),
        ("ck3Log", "CK3 console_history.txt", "Select console_history.txt"),
        ("attilaData", "Attila data folder", "Select the Attila 'data' folder"),
        ("attilaLog", "Attila BattleResults_log.txt", "Select BattleResults_log.txt"),
        ("attilaUserScript", "Attila user.script.txt", "Select user.script.txt"),
        ("rpfmCli", "rpfm_cli executable", "Select the rpfm_cli executable"),
        ("workspaceDir", "Workspace folder", "Select the Crusader Conflicts workspace folder"),
    };

    private readonly Dictionary<string, TextBox> _editors = new();

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

        foreach (var (key, label, browseTitle) in EditableFields)
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
            var browse = new Button { Content = "Browse…", Tag = browseTitle };
            browse.Click += async (_, _) => await BrowseAsync(editor, browseTitle);
            row.Children.Add(text);
            row.Children.Add(editor);
            row.Children.Add(browse);
            RowsPanel.Children.Add(row);
        }
    }

    private async Task BrowseAsync(TextBox editor, string title)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        if (result.Count > 0)
        {
            editor.Text = result[0].Path.LocalPath;
        }
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
