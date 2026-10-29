using System;
using System.Windows.Forms;
using CrusaderWars.Core.Dialogs;

namespace CrusaderWars
{
    /// <summary>
    /// WinForms-backed dialog implementation. Callers that pass an owner hand the native
    /// window (main form) so the dialog is modal to it, matching the previous
    /// MessageBox.Show(owner, ...) call sites.
    /// </summary>
    internal sealed class WindowsDialogService : IDialogService
    {
        public void Show(string message, string title, DialogIcon icon) =>
            MessageBox.Show(message, title, MessageBoxButtons.OK, MapIcon(icon), MessageBoxDefaultButton.Button1, 0);

        public void Show(object? owner, string message, string title, DialogIcon icon) =>
            MessageBox.Show(owner as IWin32Window, message, title, MessageBoxButtons.OK, MapIcon(icon), MessageBoxDefaultButton.Button1, 0);

        public DialogChoice Show(string message, string title, DialogButtons buttons, DialogIcon icon) =>
            Map(MessageBox.Show(message, title, MapButtons(buttons), MapIcon(icon), MessageBoxDefaultButton.Button1, 0));

        private static MessageBoxIcon MapIcon(DialogIcon icon) => icon switch
        {
            DialogIcon.Error => MessageBoxIcon.Error,
            DialogIcon.Warning => MessageBoxIcon.Warning,
            DialogIcon.Question => MessageBoxIcon.Question,
            DialogIcon.Information => MessageBoxIcon.Information,
            _ => MessageBoxIcon.None
        };

        private static MessageBoxButtons MapButtons(DialogButtons buttons) => buttons switch
        {
            DialogButtons.Ok => MessageBoxButtons.OK,
            DialogButtons.YesNo => MessageBoxButtons.YesNo,
            DialogButtons.RetryCancel => MessageBoxButtons.RetryCancel,
            _ => MessageBoxButtons.OK
        };

        private static DialogChoice Map(DialogResult result) => result switch
        {
            DialogResult.OK => DialogChoice.Ok,
            DialogResult.Yes => DialogChoice.Yes,
            DialogResult.No => DialogChoice.No,
            DialogResult.Retry => DialogChoice.Retry,
            DialogResult.Cancel => DialogChoice.Cancel,
            _ => DialogChoice.None
        };
    }
}
