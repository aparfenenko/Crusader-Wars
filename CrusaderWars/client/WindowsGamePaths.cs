using System;
using CrusaderWars.Core.Paths;

namespace CrusaderWars
{
    /// <summary>
    /// Live view over the legacy Properties.Settings keys for Windows. Every access reads
    /// the current setting value, matching how the domain code behaved before the gateway:
    /// users can re-pick game executables in Options and the paths follow immediately.
    /// </summary>
    internal sealed class WindowsGamePaths : IGamePaths
    {
        public string Ck3ExecutablePath => Properties.Settings.Default.VAR_ck3_path;

        public string AttilaExecutablePath => Properties.Settings.Default.VAR_attila_path;

        public string Ck3ConsoleLogPath =>
            DocumentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";

        public string Ck3SaveGamesDirectory =>
            DocumentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";

        public string AttilaBattleLogPath =>
            GamePathResolver.DeriveWindowsAttilaBattleLog(AttilaExecutablePath);

        private static string DocumentsPath =>
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
