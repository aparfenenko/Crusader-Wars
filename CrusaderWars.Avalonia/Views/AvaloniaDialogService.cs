using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media;
using Avalonia.Threading;
using CrusaderWars.Core.Dialogs;

namespace CrusaderWars.Avalonia.Views;

/// <summary>
/// Avalonia-backed dialog service: modal windows instead of console prompts, so domain
/// errors raised on the battle-cycle background thread are visible in the GUI.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly System.Func<Window?> _ownerProvider;

    public AvaloniaDialogService(System.Func<Window?> ownerProvider)
    {
        _ownerProvider = ownerProvider;
    }

    public void Show(string message, string title, DialogIcon icon) =>
        ShowModal(message, title, DialogButtons.Ok, icon);

    public void Show(object? owner, string message, string title, DialogIcon icon) =>
        ShowModal(message, title, DialogButtons.Ok, icon);

    public DialogChoice Show(string message, string title, DialogButtons buttons, DialogIcon icon) =>
        ShowModal(message, title, buttons, icon);

    private DialogChoice ShowModal(string message, string title, DialogButtons buttons, DialogIcon icon)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            // Cannot block the UI thread on a modal dialog; surface it non-modally.
            var fireAndForget = BuildDialog(message, title, buttons, icon);
            var owner = _ownerProvider();
            if (owner is null) fireAndForget.Show();
            else fireAndForget.Show(owner);
            return DialogChoice.None;
        }

        var ownerWindow = Dispatcher.UIThread.InvokeAsync(_ownerProvider).GetAwaiter().GetResult();
        if (ownerWindow is null)
        {
            System.Console.WriteLine($"[{title}] {message}");
            return DialogChoice.None;
        }

        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = BuildDialog(message, title, buttons, icon);
            await dialog.ShowDialog(ownerWindow);
            return dialog.Choice;
        }).GetAwaiter().GetResult();
    }

    private static DialogWindow BuildDialog(string message, string title, DialogButtons buttons, DialogIcon icon)
    {
        return new DialogWindow(message, title, buttons, icon);
    }

    /// <summary>Minimal modal dialog window with the requested button set.</summary>
    private sealed class DialogWindow : Window
    {
        public DialogChoice Choice { get; private set; } = DialogChoice.None;

        public DialogWindow(string message, string title, DialogButtons buttons, DialogIcon icon)
        {
            Title = title;
            Width = 480;
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            Topmost = true;

            var iconText = icon switch
            {
                DialogIcon.Error => "✖",
                DialogIcon.Warning => "⚠",
                DialogIcon.Question => "?",
                DialogIcon.Information => "ℹ",
                _ => ""
            };

            var panel = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 12 };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(new TextBlock
            {
                Text = iconText,
                FontSize = 22,
                Foreground = icon switch
                {
                    DialogIcon.Error => Brushes.Red,
                    DialogIcon.Warning => Brushes.Orange,
                    _ => Brushes.DodgerBlue
                }
            });
            header.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 16 });
            panel.Children.Add(header);

            panel.Children.Add(new ScrollViewer
            {
                MaxHeight = 320,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }
            });

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

            void AddButton(string caption, DialogChoice choice)
            {
                var button = new Button { Content = caption, MinWidth = 90 };
                button.Click += (_, _) => { Choice = choice; Close(); };
                buttonRow.Children.Add(button);
            }

            switch (buttons)
            {
                case DialogButtons.YesNo:
                    AddButton("Yes", DialogChoice.Yes);
                    AddButton("No", DialogChoice.No);
                    break;
                case DialogButtons.RetryCancel:
                    AddButton("Retry", DialogChoice.Retry);
                    AddButton("Cancel", DialogChoice.Cancel);
                    break;
                default:
                    AddButton("OK", DialogChoice.Ok);
                    break;
            }

            panel.Children.Add(buttonRow);
            Content = panel;
        }
    }
}
