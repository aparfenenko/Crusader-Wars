using System;
using System.IO;
using System.Xml;
using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;
using CrusaderWars.mod_manager;

namespace CrusaderWars.client
{
    /// <summary>
    /// Options.xml persistence: full-file load/create plus individual-entry updates.
    /// Ported from the WinForms Options form so the same file semantics run on every
    /// platform: dialogs go through IDialogService and methods return false where the
    /// Windows application would have exited, leaving the exit decision to the caller.
    /// </summary>
    public static class OptionsFileStore
    {
        private static readonly string OptionsFilePath = Path.Combine(".", "settings", "Options.xml");
        private static readonly string OptionsXsdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings", "schemas", "Options.xsd");
        private static bool _isRetryingOptionsLoad = false;

        public static bool ReadOptionsFile()
        {
            CrusaderWarsEnvironment.Logger.Debug("Reading options file...");
            try
            {
                string file = OptionsFilePath;

                if (!File.Exists(file))
                {
                    CrusaderWarsEnvironment.Logger.Debug("Options.xml not found. Creating default.");
                    CreateDefaultOptionsFile();
                    // No validation needed for a freshly created file, so we can return.
                    // The file will be read on the next application start.
                }
                else
                {
                    var validationErrors = XmlValidator.Validate(file, OptionsXsdPath);
                    if (validationErrors.Count > 0)
                    {
                        if (_isRetryingOptionsLoad)
                        {
                            CrusaderWarsEnvironment.Dialogs.Show("The default Options.xml file is also invalid. The application will now exit.", "Fatal Error", DialogIcon.Error);
                            return false;
                        }

                        bool reset = RepairCorruptedFile(file, "Options.xml", validationErrors, CreateDefaultOptionsFile);
                        if (reset)
                        {
                            _isRetryingOptionsLoad = true;
                            return ReadOptionsFile(); // Re-run to load the new default file
                        }
                        return false; // User declined the reset; caller decides to exit
                    }
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                ModOptions.optionsValuesCollection.Clear();
                var CloseCK3_Value = GetOptionValue(xmlDoc, "CloseCK3", "Enabled");
                var CloseAttila_Value = GetOptionValue(xmlDoc, "CloseAttila", "Enabled");
                var FullArmies_Value = GetOptionValue(xmlDoc, "FullArmies", "Disabled");
                var TimeLimit_Value = GetOptionValue(xmlDoc, "TimeLimit", "Enabled");
                var BattleMapsSize_Value = GetOptionValue(xmlDoc, "BattleMapsSize", "Dynamic");
                var DefensiveDeployables_Value = GetOptionValue(xmlDoc, "DefensiveDeployables", "Enabled");
                var UnitCards_Value = GetOptionValue(xmlDoc, "UnitCards", "Enabled");
                var LeviesMax_Value = GetOptionValue(xmlDoc, "LeviesMax", "300");
                var RangedMax_Value = GetOptionValue(xmlDoc, "RangedMax", "200");
                var InfantryMax_Value = GetOptionValue(xmlDoc, "InfantryMax", "200");
                var CavalryMax_Value = GetOptionValue(xmlDoc, "CavalryMax", "100");
                var BattleScale_Value = GetOptionValue(xmlDoc, "BattleScale", "100%");
                var AutoScaleUnits_Value = GetOptionValue(xmlDoc, "AutoScaleUnits", "Enabled");
                var SeparateArmies_Value = GetOptionValue(xmlDoc, "SeparateArmies", "Friendly Only");
                var SiegeEnginesInFieldBattles_Value = GetOptionValue(xmlDoc, "SiegeEnginesInFieldBattles", "Enabled");
                var ShowPostBattleReport_Value = GetOptionValue(xmlDoc, "ShowPostBattleReport", "Enabled");

                // New Commander and Knight wound chances
                var CommanderWoundedChance_Value = GetOptionValue(xmlDoc, "CommanderWoundedChance", "65");
                var CommanderSeverelyInjuredChance_Value = GetOptionValue(xmlDoc, "CommanderSeverelyInjuredChance", "10");
                var CommanderBrutallyMauledChance_Value = GetOptionValue(xmlDoc, "CommanderBrutallyMauledChance", "5");
                var CommanderMaimedChance_Value = GetOptionValue(xmlDoc, "CommanderMaimedChance", "5");
                var CommanderOneLeggedChance_Value = GetOptionValue(xmlDoc, "CommanderOneLeggedChance", "2");
                var CommanderOneEyedChance_Value = GetOptionValue(xmlDoc, "CommanderOneEyedChance", "3");
                var CommanderDisfiguredChance_Value = GetOptionValue(xmlDoc, "CommanderDisfiguredChance", "2");
                var CommanderSlainChance_Value = GetOptionValue(xmlDoc, "CommanderSlainChance", "8");
                var CommanderPrisonerChance_Value = GetOptionValue(xmlDoc, "CommanderPrisonerChance", "60");

                var KnightWoundedChance_Value = GetOptionValue(xmlDoc, "KnightWoundedChance", "65");
                var KnightSeverelyInjuredChance_Value = GetOptionValue(xmlDoc, "KnightSeverelyInjuredChance", "10");
                var KnightBrutallyMauledChance_Value = GetOptionValue(xmlDoc, "KnightBrutallyMauledChance", "5");
                var KnightMaimedChance_Value = GetOptionValue(xmlDoc, "KnightMaimedChance", "5");
                var KnightOneLeggedChance_Value = GetOptionValue(xmlDoc, "KnightOneLeggedChance", "2");
                var KnightOneEyedChance_Value = GetOptionValue(xmlDoc, "KnightOneEyedChance", "3");
                var KnightDisfiguredChance_Value = GetOptionValue(xmlDoc, "KnightDisfiguredChance", "2");
                var KnightSlainChance_Value = GetOptionValue(xmlDoc, "KnightSlainChance", "8");
                var KnightPrisonerChance_Value = GetOptionValue(xmlDoc, "KnightPrisonerChance", "60");

                // Add new OptInPreReleases option
                var OptInPreReleases_Value = GetOptionValue(xmlDoc, "OptInPreReleases", "False");
                var SelectedCustomMapper_Value = GetOptionValue(xmlDoc, "SelectedCustomMapper", "");
                var CombineKnights_Value = GetOptionValue(xmlDoc, "CombineKnights", "Disabled");
                var LinuxSetupCompleted_Value = GetOptionValue(xmlDoc, "LinuxSetupCompleted", "False");
                var LevyMinSize_Value = GetOptionValue(xmlDoc, "LevyMinSize", "10");


                xmlDoc.Save(file);
                CrusaderWarsEnvironment.Logger.Debug("All options read from XML.");

                ModOptions.optionsValuesCollection.Add("CloseCK3", CloseCK3_Value);
                ModOptions.optionsValuesCollection.Add("CloseAttila", CloseAttila_Value);
                ModOptions.optionsValuesCollection.Add("FullArmies", FullArmies_Value);
                ModOptions.optionsValuesCollection.Add("TimeLimit", TimeLimit_Value);
                ModOptions.optionsValuesCollection.Add("BattleMapsSize", BattleMapsSize_Value);
                ModOptions.optionsValuesCollection.Add("DefensiveDeployables", DefensiveDeployables_Value);
                ModOptions.optionsValuesCollection.Add("UnitCards", UnitCards_Value);
                ModOptions.optionsValuesCollection.Add("SeparateArmies", SeparateArmies_Value);
                ModOptions.optionsValuesCollection.Add("LeviesMax", LeviesMax_Value);
                ModOptions.optionsValuesCollection.Add("RangedMax", RangedMax_Value);
                ModOptions.optionsValuesCollection.Add("InfantryMax", InfantryMax_Value);
                ModOptions.optionsValuesCollection.Add("CavalryMax", CavalryMax_Value);
                ModOptions.optionsValuesCollection.Add("BattleScale", BattleScale_Value);
                ModOptions.optionsValuesCollection.Add("AutoScaleUnits", AutoScaleUnits_Value);
                ModOptions.optionsValuesCollection.Add("SiegeEnginesInFieldBattles", SiegeEnginesInFieldBattles_Value);
                ModOptions.optionsValuesCollection.Add("ShowPostBattleReport", ShowPostBattleReport_Value);
                ModOptions.optionsValuesCollection.Add("CommanderWoundedChance", CommanderWoundedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderSeverelyInjuredChance", CommanderSeverelyInjuredChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderBrutallyMauledChance", CommanderBrutallyMauledChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderMaimedChance", CommanderMaimedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderOneLeggedChance", CommanderOneLeggedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderOneEyedChance", CommanderOneEyedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderDisfiguredChance", CommanderDisfiguredChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderSlainChance", CommanderSlainChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderPrisonerChance", CommanderPrisonerChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightWoundedChance", KnightWoundedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightSeverelyInjuredChance", KnightSeverelyInjuredChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightBrutallyMauledChance", KnightBrutallyMauledChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightMaimedChance", KnightMaimedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightOneLeggedChance", KnightOneLeggedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightOneEyedChance", KnightOneEyedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightDisfiguredChance", KnightDisfiguredChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightSlainChance", KnightSlainChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightPrisonerChance", KnightPrisonerChance_Value);
                ModOptions.optionsValuesCollection.Add("OptInPreReleases", OptInPreReleases_Value); // Add new option
                ModOptions.optionsValuesCollection.Add("SelectedCustomMapper", SelectedCustomMapper_Value);
                ModOptions.optionsValuesCollection.Add("CombineKnights", CombineKnights_Value);
                ModOptions.optionsValuesCollection.Add("LinuxSetupCompleted", LinuxSetupCompleted_Value);
                ModOptions.optionsValuesCollection.Add("LevyMinSize", LevyMinSize_Value);
                ModOptions.SelectedCustomMapper = SelectedCustomMapper_Value;
                CrusaderWarsEnvironment.Logger.Debug("Options collection populated.");
                return true;
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Error(ex);
                CrusaderWarsEnvironment.Dialogs.Show("Error reading game options. Restart the mod and try again", "Crusader Conflicts: Data Error", DialogIcon.Error);
                return false;
            }
        }

        public static void CreateDefaultOptionsFile()
        {
            CrusaderWarsEnvironment.Logger.Debug("Creating default Options.xml file...");
            try
            {
                string file = OptionsFilePath;
                string dir = Path.GetDirectoryName(file)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                XmlDocument xmlDoc = new XmlDocument();
                XmlElement root = xmlDoc.CreateElement("Options");
                xmlDoc.AppendChild(root);

                AddDefaultOption(xmlDoc, root, "CloseCK3", "Enabled");
                AddDefaultOption(xmlDoc, root, "CloseAttila", "Enabled");
                AddDefaultOption(xmlDoc, root, "FullArmies", "Disabled");
                AddDefaultOption(xmlDoc, root, "TimeLimit", "Enabled");
                AddDefaultOption(xmlDoc, root, "BattleMapsSize", "Dynamic");
                AddDefaultOption(xmlDoc, root, "DefensiveDeployables", "Enabled");
                AddDefaultOption(xmlDoc, root, "UnitCards", "Enabled");
                AddDefaultOption(xmlDoc, root, "LeviesMax", "300");
                AddDefaultOption(xmlDoc, root, "RangedMax", "200");
                AddDefaultOption(xmlDoc, root, "InfantryMax", "200");
                AddDefaultOption(xmlDoc, root, "CavalryMax", "100");
                AddDefaultOption(xmlDoc, root, "BattleScale", "100%");
                AddDefaultOption(xmlDoc, root, "AutoScaleUnits", "Enabled");
                AddDefaultOption(xmlDoc, root, "SeparateArmies", "Friendly Only");
                AddDefaultOption(xmlDoc, root, "SiegeEnginesInFieldBattles", "Enabled");
                AddDefaultOption(xmlDoc, root, "ShowPostBattleReport", "Enabled");
                AddDefaultOption(xmlDoc, root, "CommanderWoundedChance", "65");
                AddDefaultOption(xmlDoc, root, "CommanderSeverelyInjuredChance", "10");
                AddDefaultOption(xmlDoc, root, "CommanderBrutallyMauledChance", "5");
                AddDefaultOption(xmlDoc, root, "CommanderMaimedChance", "5");
                AddDefaultOption(xmlDoc, root, "CommanderOneLeggedChance", "2");
                AddDefaultOption(xmlDoc, root, "CommanderOneEyedChance", "3");
                AddDefaultOption(xmlDoc, root, "CommanderDisfiguredChance", "2");
                AddDefaultOption(xmlDoc, root, "CommanderSlainChance", "8");
                AddDefaultOption(xmlDoc, root, "CommanderPrisonerChance", "60");
                AddDefaultOption(xmlDoc, root, "KnightWoundedChance", "65");
                AddDefaultOption(xmlDoc, root, "KnightSeverelyInjuredChance", "10");
                AddDefaultOption(xmlDoc, root, "KnightBrutallyMauledChance", "5");
                AddDefaultOption(xmlDoc, root, "KnightMaimedChance", "5");
                AddDefaultOption(xmlDoc, root, "KnightOneLeggedChance", "2");
                AddDefaultOption(xmlDoc, root, "KnightOneEyedChance", "3");
                AddDefaultOption(xmlDoc, root, "KnightDisfiguredChance", "2");
                AddDefaultOption(xmlDoc, root, "KnightSlainChance", "8");
                AddDefaultOption(xmlDoc, root, "KnightPrisonerChance", "60");
                AddDefaultOption(xmlDoc, root, "OptInPreReleases", "False");
                AddDefaultOption(xmlDoc, root, "SelectedCustomMapper", string.Empty);
                AddDefaultOption(xmlDoc, root, "CombineKnights", "Disabled");
                AddDefaultOption(xmlDoc, root, "LinuxSetupCompleted", "False");
                AddDefaultOption(xmlDoc, root, "LevyMinSize", "10");

                xmlDoc.Save(file);
                CrusaderWarsEnvironment.Logger.Debug("Default Options.xml file created successfully.");
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Error(ex);
                CrusaderWarsEnvironment.Dialogs.Show("Error creating default options file. The application may not function correctly.",
                    "Crusader Conflicts: File Error", DialogIcon.Error);
            }
        }

        /// <summary>
        /// Persists the in-memory options dictionary back to Options.xml, updating
        /// existing Option nodes and appending missing ones (mac GUI options editor).
        /// </summary>
        public static void SaveOptionsFile()
        {
            try
            {
                string file = OptionsFilePath;
                string dir = Path.GetDirectoryName(file)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(file))
                {
                    xmlDoc.Load(file);
                }
                if (xmlDoc.DocumentElement is null)
                {
                    XmlElement rootElement = xmlDoc.CreateElement("Options");
                    xmlDoc.AppendChild(rootElement);
                }

                foreach (var entry in ModOptions.optionsValuesCollection)
                {
                    var node = xmlDoc.SelectSingleNode("//Option[@name='" + entry.Key + "']");
                    if (node != null)
                    {
                        node.InnerText = entry.Value ?? string.Empty;
                    }
                    else
                    {
                        XmlElement option = xmlDoc.CreateElement("Option");
                        option.SetAttribute("name", entry.Key);
                        option.InnerText = entry.Value ?? string.Empty;
                        xmlDoc.DocumentElement!.AppendChild(option);
                    }
                }

                xmlDoc.Save(file);
                CrusaderWarsEnvironment.Logger.Debug("Options.xml saved from in-memory collection.");
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Error(ex);
                CrusaderWarsEnvironment.Dialogs.Show("Error saving game options.", "Crusader Conflicts: File Error", DialogIcon.Error);
            }
        }

        /// <summary>
        /// Corrupted-file repair flow shared by the settings loaders: backs the broken
        /// file up, asks the user whether to reset it, and recreates the default on
        /// confirmation. Returns the user's choice; false must be treated as "exit" by
        /// the caller (the Windows app calls Application.Exit there).
        /// </summary>
        public static bool RepairCorruptedFile(string xmlPath, string xmlFileName, System.Collections.Generic.List<string> validationErrors, Action createDefaultFileAction)
        {
            CrusaderWarsEnvironment.Logger.Debug($"{xmlFileName} failed XSD validation with {validationErrors.Count} errors:");
            foreach (string error in validationErrors)
            {
                CrusaderWarsEnvironment.Logger.Debug($"  - {error}");
            }

            try
            {
                string backupPath = xmlPath + ".corrupted.bak";
                File.Copy(xmlPath, backupPath, overwrite: true);
                CrusaderWarsEnvironment.Logger.Debug($"Backup of corrupted {xmlFileName} created at {backupPath}");
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Warning: Could not create backup of corrupted {xmlFileName}: {ex.Message}");
            }

            var choice = CrusaderWarsEnvironment.Dialogs.Show(
                $"The file '{xmlFileName}' is corrupted or invalid and cannot be loaded.\n\nWould you like to reset it to default settings?\n\nClicking 'No' will exit the application.",
                "Configuration File Error",
                DialogButtons.YesNo,
                DialogIcon.Warning);

            if (choice == DialogChoice.Yes)
            {
                createDefaultFileAction();
                CrusaderWarsEnvironment.Logger.Debug($"Default file created for {xmlFileName} at {xmlPath}");
                return true;
            }

            CrusaderWarsEnvironment.Logger.Debug($"User chose to exit due to corrupted {xmlFileName}");
            return false;
        }

        private static string GetOptionValue(XmlDocument doc, string optionName, string defaultValue)
        {
            if (doc.DocumentElement is null) return defaultValue;
            XmlNode? node = doc.SelectSingleNode($"//Option[@name='{optionName}']");
            if (node != null)
            {
                var text = node.InnerText?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
                // Self-heal empty/whitespace-only values so they cannot wipe the UI dropdowns on save
                CrusaderWarsEnvironment.Logger.Debug($"Option '{optionName}' is empty in Options.xml. Resetting to default value '{defaultValue}'.");
                node.InnerText = defaultValue;
                return defaultValue;
            }
            else
            {
                CrusaderWarsEnvironment.Logger.Debug($"Option '{optionName}' not found in Options.xml. Creating with default value '{defaultValue}'.");
                XmlElement newOption = doc.CreateElement("Option");
                newOption.SetAttribute("name", optionName);
                newOption.InnerText = defaultValue; // This line sets the InnerText
                doc.DocumentElement?.AppendChild(newOption);
                return defaultValue;
            }
        }

        private static void AddDefaultOption(XmlDocument doc, XmlElement root,
            string name, string defaultValue)
        {
            XmlElement option = doc.CreateElement("Option");
            option.SetAttribute("name", name);
            option.InnerText = defaultValue;
            root.AppendChild(option);
        }

        public static void SetArmiesControl(ModOptions.ArmiesSetup value)
        {
            CrusaderWarsEnvironment.Logger.Debug($"Setting 'Armies Control' to {value}.");
            string valueString = "";
            switch (value)
            {
                case ModOptions.ArmiesSetup.All_Controled:
                    valueString = "All Controled";
                    break;
                case ModOptions.ArmiesSetup.Friendly_Only:
                    valueString = "Friendly Only";
                    break;
                case ModOptions.ArmiesSetup.All_Separate:
                    valueString = "All Separate";
                    break;
            }

            try
            {
                string file = OptionsFilePath;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var separateArmiesNode = xmlDoc.SelectSingleNode("//Option [@name='SeparateArmies']");
                if (separateArmiesNode != null)
                {
                    separateArmiesNode.InnerText = valueString;
                    xmlDoc.Save(file);
                    CrusaderWarsEnvironment.Logger.Debug("'SeparateArmies' option updated in Options.xml.");

                    ModOptions.optionsValuesCollection["SeparateArmies"] = valueString;
                    CrusaderWarsEnvironment.Logger.Debug("In-memory options collections updated.");
                }
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Error in SetArmiesControl: {ex.Message}");
            }
        }
    }
}
