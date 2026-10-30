using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using CrusaderWars.Core.Paths;
using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;

namespace CrusaderWars
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SupportedOSPlatform("windows")]
        static void Main()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            CrusaderWarsEnvironment.Initialize(
                new WindowsGamePaths(),
                new WindowsDialogService(),
                new FileAppLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"), truncateDebugLogOnCreate: true));
            Logger.Debug("Application starting...");
            Logger.Debug(AppDomain.CurrentDomain.BaseDirectory);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Subscribe to global exception events
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.Run(new HomePage());
        }

        [SupportedOSPlatform("windows")]
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // Handle the exception
            Logger.Debug($"Global thread exception caught: {e.Exception.Message}");
            CrusaderWarsEnvironment.Dialogs.Show("An unexpected error occurred. Please try again.", "Crusader Conflicts: Error", DialogIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log(e.Exception);
        }

        [SupportedOSPlatform("windows")]
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Handle the exception
            Logger.Debug($"Unhandled domain exception caught: {((Exception)e.ExceptionObject).Message}");
            CrusaderWarsEnvironment.Dialogs.Show("An unexpected error occurred. Please try again.", "Crusader Conflicts: Error", DialogIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log((Exception)e.ExceptionObject);
        }

        /// <summary>
        /// Application logging. Delegates to the portable logger on the environment so the
        /// format and files (data/debug.log, data/error.log) stay identical while domain
        /// code can later use the same implementation from Core.
        /// </summary>
        public static class Logger
        {
            public static void Debug(string message)
            {
                CrusaderWarsEnvironment.Logger.Debug(message);
            }

            public static void Log(Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Error(ex);
            }
        }

    }
}
