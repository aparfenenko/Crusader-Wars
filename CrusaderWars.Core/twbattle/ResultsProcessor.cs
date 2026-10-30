using System.Text;
using CrusaderWars.Core.Paths;
using CrusaderWars.armies;
using CrusaderWars.data.battle_results;
using CrusaderWars.data.save_file;
using CrusaderWars.data.battle_results.editors;
using CrusaderWars.sieges;

namespace CrusaderWars.twbattle
{
    /// <summary>
    /// Applies TW:Attila battle results to the extracted CK3 gamestate files and repacks
    /// the save. Verbatim port of the results block of BattleProcessor.ProcessBattle
    /// (minus the WinForms post-battle report), so the CLI runs the same write sequence
    /// in the same order with the same inputs. Intended for the single-process `run`
    /// cycle: the armies passed in must be the scaled in-memory armies the battle was
    /// generated from, exactly like on Windows.
    /// </summary>
    public static class ResultsProcessor
    {
        public static string Apply(
            List<Army> attacker_armies,
            List<Army> defender_armies,
            List<Army> left_side,
            List<Army> right_side,
            string path_log_attila,
            string path_editedSave)
        {
            // --- Capture pre-battle state for logging ---
            Dictionary<string, int> originalAttackerSizes = new Dictionary<string, int>();
            foreach (var army in attacker_armies)
            {
                if (army.ArmyRegiments != null)
                {
                    foreach (var armyRegiment in army.ArmyRegiments)
                    {
                        if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;
                        if (armyRegiment.Regiments != null)
                        {
                            foreach (var regiment in armyRegiment.Regiments)
                            {
                                if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;
                                string key = $"{army.ID}_{regiment.ID}";
                                if (!originalAttackerSizes.ContainsKey(key))
                                {
                                    originalAttackerSizes.Add(key, Int32.Parse(regiment.CurrentNum));
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<string, int> originalDefenderSizes = new Dictionary<string, int>();
            foreach (var army in defender_armies)
            {
                if (army.ArmyRegiments != null)
                {
                    foreach (var armyRegiment in army.ArmyRegiments)
                    {
                        if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;
                        if (armyRegiment.Regiments != null)
                        {
                            foreach (var regiment in armyRegiment.Regiments)
                            {
                                if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;
                                string key = $"{army.ID}_{regiment.ID}";
                                if (!originalDefenderSizes.ContainsKey(key))
                                {
                                    originalDefenderSizes.Add(key, Int32.Parse(regiment.CurrentNum));
                                }
                            }
                        }
                    }
                }
            }

            //  SET CASUALITIES
            CrusaderWarsEnvironment.Logger.Debug("Setting casualties for attacker armies...");
            foreach (var army in attacker_armies)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Processing army ID: {army.ID}");
                CasualtyProcessor.ReadAttilaResults(army, path_log_attila);
                CasualtyProcessor.CheckForSlainCommanders(army, path_log_attila);
                CasualtyProcessor.CheckKnightsKills(army);
                CasualtyProcessor.CheckForSlainKnights(army);
            }
            CrusaderWarsEnvironment.Logger.Debug("Setting casualties for defender armies...");
            foreach (var army in defender_armies)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Processing army ID: {army.ID}");
                CasualtyProcessor.ReadAttilaResults(army, path_log_attila);
                CasualtyProcessor.CheckForSlainCommanders(army, path_log_attila);
                CasualtyProcessor.CheckKnightsKills(army);
                CasualtyProcessor.CheckForSlainKnights(army);
            }

            CasualtyProcessor.LogPostBattleReport(attacker_armies, originalAttackerSizes, "ATTACKER");
            if (BattleState.IsSiegeBattle)
            {
                var siegeEngines = SiegeEngineGenerator.Generate(attacker_armies);
                if (siegeEngines != null && siegeEngines.Any())
                {
                    CrusaderWarsEnvironment.Logger.Debug("  --- Siege Engines ---");
                    foreach (var engine in siegeEngines)
                    {
                        CrusaderWarsEnvironment.Logger.Debug($"  - Type: {engine.Key}, Quantity: {engine.Value}");
                    }
                }
            }
            CasualtyProcessor.LogPostBattleReport(defender_armies, originalDefenderSizes, "DEFENDER");

            // DETERMINE WINNER FIRST so EditLivingFile can correctly calculate prisoner chances
            string winner = BattleResultReader.GetAttilaWinner(path_log_attila, left_side[0].CombatSide, right_side[0].CombatSide);
            BattleResult.IsAttackerVictorious = (winner == "attacker");
            CrusaderWarsEnvironment.Logger.Debug($"Battle winner determined: {winner}. IsAttackerVictorious set to: {BattleResult.IsAttackerVictorious}");

            // DETERMINE CHARACTER FATES BEFORE WAR SCORE CALCULATION
            BattleResult.DetermineCharacterFates(attacker_armies.Concat(defender_armies).ToList());

            // CALCULATE WAR SCORE BEFORE GENERATING REPORT
            BattleResult.CalculateAndSetWarScore(attacker_armies, defender_armies);

            //  EDIT VASSAL CONTRACTS FILE
            BattleResult.EditVassalContractsFile();

            //  EDIT LIVING FILE
            CrusaderWarsEnvironment.Logger.Debug("Editing Living.txt file...");
            BattleResult.EditLivingFile(attacker_armies, defender_armies);

            if (!BattleState.IsSiegeBattle || (BattleState.IsSiegeBattle && BattleState.HasReliefArmy))
            {
                // Field Battle OR Siege with Relief Army: Edit files as normal
                CrusaderWarsEnvironment.Logger.Debug("Field battle or siege with relief army detected. Editing combat files...");

                var mobile_attacker_armies = attacker_armies.Where(a => !a.IsGarrison()).ToList();
                var mobile_defender_armies = defender_armies.Where(a => !a.IsGarrison()).ToList();

                //  EDIT COMBATS FILE
                BattleResult.EditCombatFile(mobile_attacker_armies, mobile_defender_armies);
                //  EDIT COMBATS RESULTS FILE
                BattleResult.EditCombatResultsFile(mobile_attacker_armies, mobile_defender_armies);
            }
            else
            {
                // Standard Siege Battle (no relief army): copy original files to temp to prevent corruption
                CrusaderWarsEnvironment.Logger.Debug("Standard siege battle (no relief army) detected. Skipping modification of Combats.txt and CombatResults.txt.");

                string combatsSourcePath = Writter.DataFilesPaths.Combats_Path();
                string combatsDestPath = Writter.DataTEMPFilesPaths.Combats_Path();
                if (File.Exists(combatsSourcePath))
                {
                    File.Copy(combatsSourcePath, combatsDestPath, true);
                    CrusaderWarsEnvironment.Logger.Debug("Copied original Combats.txt to temp folder.");
                }

                string combatResultsSourcePath = Writter.DataFilesPaths.CombatResults_Path();
                string combatResultsDestPath = Writter.DataTEMPFilesPaths.CombatResults_Path();
                if (File.Exists(combatResultsSourcePath))
                {
                    File.Copy(combatResultsSourcePath, combatResultsDestPath, true);
                    CrusaderWarsEnvironment.Logger.Debug("Copied original CombatResults.txt to temp folder.");
                }
            }

            //  EDIT REGIMENTS FILE
            CrusaderWarsEnvironment.Logger.Debug("Editing Regiments.txt file...");
            BattleResult.EditRegimentsFile(attacker_armies, defender_armies);

            //  EDIT ARMY REGIMENTS FILE
            CrusaderWarsEnvironment.Logger.Debug("Editing ArmyRegiments.txt file...");
            ArmyRegimentsFileEditor.EditArmyRegimentsFile(attacker_armies, defender_armies);

            //  EDIT SIEGES FILE
            if (BattleState.IsSiegeBattle)
            {
                CrusaderWarsEnvironment.Logger.Debug("Editing Sieges.txt file...");
                BattleResult.EditSiegesFile(path_log_attila, left_side[0].CombatSide, right_side[0].CombatSide, attacker_armies, defender_armies);
            }
            else
            {
                string sourcePath = Writter.DataFilesPaths.Sieges_Path();
                string destPath = Writter.DataTEMPFilesPaths.Sieges_Path();
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                    CrusaderWarsEnvironment.Logger.Debug("Copied original Sieges.txt to temp folder for non-siege battle.");
                }
            }

            // EDIT WARS FILE
            if (BattleResult.WarScoreValue.HasValue)
            {
                CrusaderWarsEnvironment.Logger.Debug("Editing Wars.txt file...");
                BattleResult.EditWarsFile(attacker_armies, defender_armies, BattleResult.WarScoreValue.Value);
            }

            //  WRITE TO CK3 SAVE FILE
            CrusaderWarsEnvironment.Logger.Debug("Writing results to gamestate file...");
            BattleResult.SendToSaveFile(path_editedSave);

            // Add battle log footer
            try
            {
                var footer = new StringBuilder();
                footer.AppendLine("-------------------------------------------------------------------------");
                footer.AppendLine("--- BATTLE COMPLETE ---");
                footer.AppendLine("=========================================================================");
                footer.AppendLine();
                File.AppendAllText(@"./data/battle.log", footer.ToString());
            }
            catch (Exception ex)
            {
                CrusaderWarsEnvironment.Logger.Debug($"Failed to write battle log footer: {ex.Message}");
            }

            //  COMPRESS CK3 SAVE FILE AND SEND TO CK3 SAVE FILE FOLDER
            CrusaderWarsEnvironment.Logger.Debug("Compressing new save file...");
            SaveFile.Compress();
            CrusaderWarsEnvironment.Logger.Debug("Finalizing save file...");
            SaveFile.Finish();
            BattleState.ClearBattleState();
            Unit.ResetUniqueIDCounter();
            CrusaderWarsEnvironment.Logger.Debug("Battle finished. Clearing battle state.");

            return winner;
        }
    }
}
