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

    public bool IsEmptyOrMissing()
    {
        if (!File.Exists(_paths.AttilaUserScript))
            return true;
        return string.IsNullOrWhiteSpace(File.ReadAllText(_paths.AttilaUserScript));
    }

    public void EnsureEmpty(bool clearIfNeeded)
    {
        if (IsEmptyOrMissing())
            return;

        if (!clearIfNeeded)
        {
            throw new InvalidOperationException(
                $"user.script.txt is not empty and may conflict with Crusader Conflicts: {_paths.AttilaUserScript}");
        }

        File.WriteAllText(_paths.AttilaUserScript, "");
        _logger.Warn($"Cleared {_paths.AttilaUserScript}");
    }
}
