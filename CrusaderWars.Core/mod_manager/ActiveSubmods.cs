using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CrusaderWars.client;
using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.mod_manager
{
    /// <summary>
    /// In-memory store of active submods per playthrough (AGOT / Realms in Exile / The
    /// Fallen Eagle) plus the ActiveSubmods.xml load/save flow, ported from SubmodManager
    /// so every platform populates the store the same way. Dialogs go through
    /// IDialogService; Load returns false where the Windows application would have
    /// exited, leaving the exit decision to the caller.
    /// </summary>
    public static class ActiveSubmods
    {
        private static readonly string ActiveSubmodsFilePath = Path.Combine(".", "settings", "ActiveSubmods.xml");
        private static readonly string ActiveSubmodsXsdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings", "schemas", "ActiveSubmods.xsd");
        private static bool _isRetryingSubmodsLoad = false;

        private static Dictionary<string, List<string>> ByPlaythrough { get; set; } = new Dictionary<string, List<string>>();

        public static bool Load()
        {
            CrusaderWarsEnvironment.Logger.Debug("Loading active submods from ActiveSubmods.xml...");
            ByPlaythrough.Clear();

            if (!File.Exists(ActiveSubmodsFilePath))
            {
                CrusaderWarsEnvironment.Logger.Debug("ActiveSubmods.xml not found. Creating default file.");
                CreateDefaultFile();
                return true;
            }

            var validationErrors = XmlValidator.Validate(ActiveSubmodsFilePath, ActiveSubmodsXsdPath);
            if (validationErrors.Count > 0)
            {
                if (_isRetryingSubmodsLoad)
                {
                    CrusaderWarsEnvironment.Dialogs.Show("The default ActiveSubmods.xml file is also invalid. The application will now exit.", "Fatal Error", DialogIcon.Error);
                    return false;
                }

                bool reset = OptionsFileStore.RepairCorruptedFile(ActiveSubmodsFilePath, "ActiveSubmods.xml", validationErrors, CreateDefaultFile);
                if (reset)
                {
                    _isRetryingSubmodsLoad = true;
                    return Load(); // Re-run to load the new default file
                }
                return false; // User declined the reset; caller decides to exit
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ActiveSubmodsFilePath);
                var playthroughNodes = xmlDoc.SelectNodes("/ActiveSubmods/Playthrough");
                if (playthroughNodes == null) return true;

                foreach (XmlNode playthroughNode in playthroughNodes)
                {
                    string? playthroughTag = playthroughNode.Attributes?["tag"]?.Value;
                    if (string.IsNullOrEmpty(playthroughTag)) continue;

                    var activeSubmods = new List<string>();
                    var submodNodes = playthroughNode.SelectNodes("Submod");
                    if (submodNodes != null)
                    {
                        foreach (XmlNode submodNode in submodNodes)
                        {
                            string submodTag = submodNode.InnerText;
                            activeSubmods.Add(submodTag);
                            CrusaderWarsEnvironment.Logger.Debug($"  Loading submod '{submodTag}' for playthrough '{playthroughTag}'.");
                        }
                    }
                    ByPlaythrough[playthroughTag] = activeSubmods;
                    CrusaderWarsEnvironment.Logger.Debug($"Loaded {activeSubmods.Count} active submods for playthrough '{playthroughTag}'.");
                }
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Error loading ActiveSubmods.xml: {ex.Message}");
            }
            return true;
        }

        public static void Save()
        {
            CrusaderWarsEnvironment.Logger.Debug("Saving active submods to ActiveSubmods.xml...");
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("ActiveSubmods");
                xmlDoc.AppendChild(root);

                foreach (var entry in ByPlaythrough)
                {
                    XmlElement playthroughElement = xmlDoc.CreateElement("Playthrough");
                    playthroughElement.SetAttribute("tag", entry.Key);
                    CrusaderWarsEnvironment.Logger.Debug($"Saving {entry.Value.Count} active submods for playthrough '{entry.Key}'.");

                    foreach (var submodTag in entry.Value)
                    {
                        XmlElement submodElement = xmlDoc.CreateElement("Submod");
                        submodElement.InnerText = submodTag;
                        playthroughElement.AppendChild(submodElement);
                        CrusaderWarsEnvironment.Logger.Debug($"  Saving submod '{submodTag}' for playthrough '{entry.Key}'.");
                    }
                    root.AppendChild(playthroughElement);
                }

                xmlDoc.Save(ActiveSubmodsFilePath);
                CrusaderWarsEnvironment.Logger.Debug("Successfully saved active submods.");
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Error saving ActiveSubmods.xml: {ex.Message}");
            }
        }

        private static void CreateDefaultFile()
        {
            try
            {
                CrusaderWarsEnvironment.Logger.Debug("Creating default ActiveSubmods.xml file...");
                string dir = Path.GetDirectoryName(ActiveSubmodsFilePath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("ActiveSubmods");
                xmlDoc.AppendChild(root);

                xmlDoc.Save(ActiveSubmodsFilePath);
                CrusaderWarsEnvironment.Logger.Debug("Default ActiveSubmods.xml file created successfully.");
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Error creating default ActiveSubmods.xml: {ex.Message}");
                CrusaderWarsEnvironment.Dialogs.Show($"Error creating default ActiveSubmods.xml: {ex.Message}", "File Creation Error", DialogIcon.Error);
            }
        }

        public static List<string> GetActiveSubmodsForPlaythrough(string playthroughTag)
        {
            if (ByPlaythrough.TryGetValue(playthroughTag, out var activeSubmods))
            {
                return activeSubmods;
            }
            return new List<string>();
        }

        public static void SetActiveSubmodsForPlaythrough(string playthroughTag, List<string> activeSubmodTags)
        {
            if (activeSubmodTags == null || !activeSubmodTags.Any())
            {
                ByPlaythrough.Remove(playthroughTag);
            }
            else
            {
                ByPlaythrough[playthroughTag] = activeSubmodTags;
            }
        }

        public static void Clear()
        {
            ByPlaythrough.Clear();
        }

        public static IReadOnlyDictionary<string, List<string>> Snapshot => ByPlaythrough;

        public static void ReplaceAll(Dictionary<string, List<string>> loaded)
        {
            ByPlaythrough = loaded ?? new Dictionary<string, List<string>>();
        }
    }
}

