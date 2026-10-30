using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Core.Attila;

public sealed class AttilaUserScript
{
    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;

    public AttilaUserScript(AppPaths paths, IAppLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>
    /// True when user.script.txt exists and already enables the CrusaderConflicts battle
    /// pack. Per the mac mod model the launcher never manages or rewrites Attila mods:
    /// the player keeps their own mod list (plus the pack line) in this file once.
    /// </summary>
    public bool PackEnabled()
    {
        if (!File.Exists(_paths.AttilaUserScript))
            return false;

        return File.ReadAllText(_paths.AttilaUserScript)
            .Contains("CrusaderConflicts.pack", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Logs a warning when the pack line is missing. Never throws and never modifies the
    /// file — without the line Attila will not load the battle pack, but clearing the
    /// player's mods is never acceptable.
    /// </summary>
    public void WarnIfPackMissing()
    {
        if (!PackEnabled())
        {
            _logger.Warn(
                $"user.script.txt does not enable CrusaderConflicts.pack — Attila will not load the battle pack. " +
                $"Add 'mod CrusaderConflicts.pack;' to {_paths.AttilaUserScript}");
        }
    }
}
