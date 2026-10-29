namespace CrusaderWars.Core.Dialogs;

public enum DialogIcon
{
    Error,
    Warning,
    Question,
    Information
}

public enum DialogButtons
{
    Ok,
    YesNo,
    RetryCancel
}

public enum DialogChoice
{
    None,
    Ok,
    Yes,
    No,
    Retry,
    Cancel
}

/// <summary>
/// Modal user notification, replacing direct MessageBox calls in domain code.
/// The owner overload takes an opaque window handle (IWin32Window on Windows);
/// implementations without a windowing system may ignore it.
/// </summary>
public interface IDialogService
{
    /// <summary>OK-only dialog, fire and forget.</summary>
    void Show(string message, string title, DialogIcon icon = DialogIcon.Error);

    /// <summary>OK-only dialog modal to the given owner window.</summary>
    void Show(object? owner, string message, string title, DialogIcon icon = DialogIcon.Error);

    /// <summary>Dialog with a choice; returns the user's answer.</summary>
    DialogChoice Show(string message, string title, DialogButtons buttons, DialogIcon icon = DialogIcon.Warning);
}
