using System.Diagnostics;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.Core.Pack;

/// <summary>
/// Builds CrusaderConflicts.pack via rpfm_cli, or copies a prebuilt pack.
/// </summary>
public sealed class PackFileBuilder
{
    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;

    public PackFileBuilder(AppPaths paths, IAppLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public string Deploy()
    {
        if (string.IsNullOrWhiteSpace(_paths.AttilaData))
            throw new InvalidOperationException("Attila data directory is not configured.");

        Directory.CreateDirectory(_paths.AttilaData);
        string destPack = Path.Combine(_paths.AttilaData, "CrusaderConflicts.pack");
        string destThumb = Path.Combine(_paths.AttilaData, "CrusaderWars.png");

        if (File.Exists(_paths.RpfmCli) && Directory.Exists(_paths.BattleFilesDir))
        {
            BuildWithRpfm(destPack);
        }
        else if (File.Exists(_paths.ExistingPack))
        {
            _logger.Warn("rpfm_cli not found; copying prebuilt CrusaderConflicts.pack.");
            File.Copy(_paths.ExistingPack, destPack, true);
        }
        else if (File.Exists(destPack))
        {
            _logger.Warn($"Using existing pack already in Attila data: {destPack}");
        }
        else
        {
            throw new FileNotFoundException(
                "Cannot deploy pack: rpfm_cli is missing and no CrusaderConflicts.pack was found. " +
                "Set rpfmCli in paths.json or place a prebuilt pack in the workspace root.");
        }

        if (File.Exists(_paths.Thumbnail) && !File.Exists(destThumb))
            File.Copy(_paths.Thumbnail, destThumb);

        _logger.Info($"Pack ready at {destPack}");
        return destPack;
    }

    private void BuildWithRpfm(string destPack)
    {
        string createPath = Path.Combine(_paths.WorkspaceDir, "CrusaderConflicts.pack");
        RunRpfm($"--game attila pack create -p \"{createPath}\"");
        string schemaArg = File.Exists(_paths.SchemaRon) ? $"-t \"{_paths.SchemaRon}\" " : "";
        RunRpfm($"--game attila pack add -p \"{createPath}\" {schemaArg}-F \"{_paths.BattleFilesDir}\"");

        if (File.Exists(destPack))
            File.Delete(destPack);
        File.Move(createPath, destPack);
    }

    private void RunRpfm(string arguments)
    {
        _logger.Debug($"rpfm_cli {arguments}");
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _paths.RpfmCli,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        proc.Start();
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (!string.IsNullOrWhiteSpace(stdout))
            _logger.Debug(stdout.Trim());
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"rpfm_cli failed ({proc.ExitCode}): {stderr}");
    }
}
