using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CrusaderWars.client;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Avalonia.Views;

/// <summary>
/// General options editor (no mod tabs — the mac launcher does not manage Attila mods).
/// Edits the in-memory options dictionary and persists it via OptionsFileStore.
/// </summary>
public partial class OptionsWindow : Window
{
    private readonly Dictionary<string, TextBox> _editors = new();

    public OptionsWindow()
    {
        InitializeComponent();
        Opened += (_, _) => BuildRows();
    }

    private void BuildRows()
    {
        OptionsPanel.Children.Clear();
        _editors.Clear();

        foreach (var entry in ModOptions.optionsValuesCollection)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var label = new TextBlock
            {
                Text = entry.Key,
                Width = 260,
                VerticalAlignment = VerticalAlignment.Center
            };
            var editor = new TextBox { Text = entry.Value, Width = 180 };
            _editors[entry.Key] = editor;
            row.Children.Add(label);
            row.Children.Add(editor);
            OptionsPanel.Children.Add(row);
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        foreach (var pair in _editors)
        {
            ModOptions.optionsValuesCollection[pair.Key] = pair.Value.Text ?? string.Empty;
        }
        OptionsFileStore.SaveOptionsFile();
        SaveStatus.Text = "Saved.";
        SaveStatus.Foreground = Brushes.Green;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
