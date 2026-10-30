using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Avalonia.Views;

/// <summary>
/// Reliable .app bundle picker: the native macOS file panel drops .app bundles from its
/// results (Avalonia classifies them as folders), so we scan the usual install locations
/// ourselves and let the user pick from a list. Manual typing in the parent field stays
/// available as the fallback.
/// </summary>
public static class AppBundleScanner
{
    public static List<(string Name, string Path)> FindBundles(string keyword)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new[]
        {
            "/Applications",
            Path.Combine(home, "Applications"),
            Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "common")
        };

        var bundles = new List<(string Name, string Path)>();
        foreach (string root in roots)
        {
            if (!Directory.Exists(root)) continue;
            if (root.EndsWith("common"))
            {
                // Steam layout: common/<Game>/<Game>.app
                foreach (var game in Directory.EnumerateDirectories(root))
                    bundles.AddRange(Directory.EnumerateDirectories(game, "*.app")
                        .Select(p => (Path.GetFileNameWithoutExtension(p), p)));
            }
            else
            {
                bundles.AddRange(Directory.EnumerateDirectories(root, "*.app")
                    .Select(p => (Path.GetFileNameWithoutExtension(p), p)));
            }
        }

        return bundles
            .DistinctBy(b => b.Path)
            .OrderBy(b => b.Name)
            .ToList();
    }

    public static List<(string Name, string Path)> FindMatching(string keyword) =>
        FindBundles(keyword)
            .Where(b => b.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
}

/// <summary>In-app dialog listing discovered .app bundles for one field.</summary>
public sealed class AppBundlePickerWindow : Window
{
    public string? SelectedPath { get; private set; }

    public AppBundlePickerWindow(string title, List<(string Name, string Path)> bundles, string fallbackHint)
    {
        Title = title;
        Width = 640;
        Height = 420;
        CanResize = false;

        var panel = new StackPanel { Margin = new global::Avalonia.Thickness(12), Spacing = 8 };

        var list = new ListBox { Height = 260 };
        foreach (var (name, path) in bundles)
        {
            var row = new StackPanel { Orientation = Orientation.Vertical };
            row.Children.Add(new TextBlock { Text = name, FontWeight = global::Avalonia.Media.FontWeight.Bold });
            row.Children.Add(new TextBlock { Text = path, FontSize = 11, Opacity = 0.7 });
            list.Items.Add(row);
        }
        panel.Children.Add(list);

        if (bundles.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = fallbackHint, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        }

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "Select", MinWidth = 90, IsEnabled = bundles.Count > 0 };
        var cancel = new Button { Content = "Cancel", MinWidth = 90 };
        ok.Click += (_, _) =>
        {
            if (list.SelectedItem is StackPanel selected &&
                selected.Children.OfType<TextBlock>().LastOrDefault() is TextBlock pathText)
            {
                SelectedPath = pathText.Text;
            }
            Close();
        };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        Content = panel;
    }
}
