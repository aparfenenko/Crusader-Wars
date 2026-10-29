using System.Text;
using System.Text.Json;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;
using ICSharpCode.SharpZipLib.Zip;

namespace CrusaderWars.Core.Save;

/// <summary>
/// Portable CK3 save unzip/rezip used after Attila battles.
/// Copied from CrusaderWars/data/save_file/SaveFile.cs without WinForms/Settings.
/// </summary>
public sealed class SaveFileService
{
    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;

    public SaveFileService(AppPaths paths, IAppLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public void Uncompress()
    {
        Directory.CreateDirectory(_paths.SaveWorkDir);
        string lastSavePath = _paths.LastSaveCk3;
        string myLastSavePath = Path.Combine(_paths.SaveWorkDir, "last_save.zip");
        string gamestatePath = Path.Combine(_paths.SaveWorkDir, "gamestate_file");
        Directory.CreateDirectory(gamestatePath);

        if (!File.Exists(lastSavePath))
            throw new FileNotFoundException($"CK3 last_save.ck3 not found: {lastSavePath}");

        File.Copy(lastSavePath, myLastSavePath, true);
        ReadMetaData(lastSavePath);
        ExtractGamestate(lastSavePath, gamestatePath);
        _logger.Info($"Uncompressed save from {lastSavePath}");
    }

    public void Compress()
    {
        string editedGamestatePath = Path.Combine(_paths.SaveWorkDir, "gamestate");
        if (!File.Exists(editedGamestatePath))
        {
            string extracted = Path.Combine(_paths.SaveWorkDir, "gamestate_file", "gamestate");
            if (!File.Exists(extracted))
                throw new FileNotFoundException($"Edited gamestate not found at {editedGamestatePath} or {extracted}");
            File.Copy(extracted, editedGamestatePath, true);
            _logger.Warn("No edited gamestate found; packing the extracted original gamestate.");
        }

        string newSaveFilePath = Path.Combine(_paths.SaveWorkDir, "battle_results.zip");
        string metadataPath = Path.Combine(_paths.SaveWorkDir, "metadata.txt");
        CompressFileWithMetadata(editedGamestatePath, newSaveFilePath, metadataPath);
        _logger.Info($"Compressed battle save to {newSaveFilePath}");
    }

    public void Finish()
    {
        string newSaveFilePath = Path.Combine(_paths.SaveWorkDir, "battle_results.zip");
        if (!File.Exists(newSaveFilePath))
            throw new FileNotFoundException($"battle_results.zip not found: {newSaveFilePath}");

        Directory.CreateDirectory(_paths.SaveGames);
        File.Copy(newSaveFilePath, _paths.LastSaveCk3, true);
        File.Copy(newSaveFilePath, _paths.BattleResultsCk3, true);
        EditContinueGameJson();
        DeleteOlderFiles();
        _logger.Info($"Wrote {_paths.BattleResultsCk3} and {_paths.LastSaveCk3}");
    }

    public void ExtractGamestate(string zipFilePath, string extractPath)
    {
        string extractedGamestate = Path.Combine(_paths.SaveWorkDir, "gamestate_file", "gamestate");
        string edited = Path.Combine(_paths.SaveWorkDir, "gamestate");
        if (File.Exists(extractedGamestate)) File.Delete(extractedGamestate);
        if (File.Exists(edited)) File.Delete(edited);
        Directory.CreateDirectory(extractPath);

        using FileStream fs = new(zipFilePath, FileMode.Open, FileAccess.Read);
        long zipDataStart = FindStartOfZipData(fs);
        if (zipDataStart < 0)
            throw new InvalidDataException("ZIP file signature not found in CK3 save.");

        using FileStream zipStream = new(zipFilePath, FileMode.Open, FileAccess.Read);
        zipStream.Seek(zipDataStart, SeekOrigin.Begin);
        using var zipFile = new ZipFile(zipStream);
        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile)
                continue;
            string dest = Path.Combine(extractPath, entry.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            using Stream zipStreamEntry = zipFile.GetInputStream(entry);
            using FileStream fileStream = File.Create(dest);
            zipStreamEntry.CopyTo(fileStream);
        }
    }

    private void CompressFileWithMetadata(string fileToZip, string zipFilePath, string metadata)
    {
        string tempZipPath = Path.GetTempFileName();
        try
        {
            using (var zipStream = File.Create(tempZipPath))
            using (var zipOutput = new ZipOutputStream(zipStream))
            {
                zipOutput.SetLevel(5);
                var entry = new ZipEntry(Path.GetFileName(fileToZip)) { DateTime = DateTime.Now };
                zipOutput.PutNextEntry(entry);
                using FileStream fileStream = File.OpenRead(fileToZip);
                fileStream.CopyTo(zipOutput);
                zipOutput.CloseEntry();
            }

            using FileStream finalZipStream = new(zipFilePath, FileMode.Create, FileAccess.Write);
            byte[] metadataBytes = Encoding.UTF8.GetBytes(File.ReadAllText(metadata));
            finalZipStream.Write(metadataBytes, 0, metadataBytes.Length);
            using FileStream tempZipStream = new(tempZipPath, FileMode.Open, FileAccess.Read);
            tempZipStream.CopyTo(finalZipStream);
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    private void EditContinueGameJson()
    {
        string jsonPath = _paths.ContinueGameJson;
        if (!File.Exists(jsonPath))
        {
            _logger.Warn($"continue_game.json not found at {jsonPath}");
            return;
        }

        string jsonString = File.ReadAllText(jsonPath);
        var jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        if (jsonDictionary == null || !jsonDictionary.ContainsKey("title"))
        {
            _logger.Warn("Key 'title' not found in continue_game.json.");
            return;
        }

        jsonDictionary["title"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize("battle_results.ck3"));
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonDictionary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void DeleteOlderFiles()
    {
        string[] files =
        [
            Path.Combine(_paths.SaveWorkDir, "last_save.zip"),
            Path.Combine(_paths.SaveWorkDir, "gamestate"),
            Path.Combine(_paths.SaveWorkDir, "gamestate_file", "gamestate"),
            Path.Combine(_paths.SaveWorkDir, "battle_results.zip")
        ];

        foreach (string file in files)
        {
            if (!File.Exists(file)) continue;
            if (file.EndsWith($"{Path.DirectorySeparatorChar}gamestate_file{Path.DirectorySeparatorChar}gamestate", StringComparison.Ordinal)
                || file.EndsWith("/gamestate_file/gamestate", StringComparison.Ordinal))
            {
                File.Copy(file, file + ".bak", true);
            }
            File.Delete(file);
        }
    }

    private void ReadMetaData(string lastSavePath)
    {
        string metadataPath = Path.Combine(_paths.SaveWorkDir, "metadata.txt");
        using StreamReader streamReader = new(lastSavePath);
        using StreamWriter streamWriter = new(metadataPath, false, Encoding.UTF8);
        streamWriter.NewLine = "\n";
        string? line;
        while ((line = streamReader.ReadLine()) != null)
        {
            streamWriter.WriteLine(line);
            if (line == "}")
                break;
        }
    }

    private static long FindStartOfZipData(FileStream fs)
    {
        const int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
        {
            for (int i = 0; i < bytesRead - 1; i++)
            {
                if (buffer[i] == 0x50 && buffer[i + 1] == 0x4B)
                    return fs.Position - (bytesRead - i);
            }
        }
        return -1;
    }
}
