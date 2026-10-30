namespace CrusaderWars.Core.Dialogs;

/// <summary>
/// Console-backed dialog service for CLI runs: messages go to stdout, choices are
/// answered on stdin (y/yes, r/retry, anything else = no/cancel).
/// </summary>
public sealed class ConsoleDialogService : IDialogService
{
    public void Show(string message, string title, DialogIcon icon) =>
        Console.WriteLine($"[{title}] {message}");

    public void Show(object? owner, string message, string title, DialogIcon icon) =>
        Show(message, title, icon);

    public DialogChoice Show(string message, string title, DialogButtons buttons, DialogIcon icon)
    {
        Console.WriteLine($"[{title}] {message}");
        string hint = buttons switch
        {
            DialogButtons.YesNo => "[y/n]",
            DialogButtons.RetryCancel => "[r/c]",
            _ => "[enter]"
        };
        Console.Write($"{hint} ");
        string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();

        return buttons switch
        {
            DialogButtons.YesNo => answer is "y" or "yes" ? DialogChoice.Yes : DialogChoice.No,
            DialogButtons.RetryCancel => answer is "r" or "retry" ? DialogChoice.Retry : DialogChoice.Cancel,
            _ => DialogChoice.Ok
        };
    }
}
