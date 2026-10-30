using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.data.battle_results; // Added for BattleResult class
using CrusaderWars.Core.Paths;

namespace CrusaderWars.data.save_file
{
    public static class Writter
    {
        static bool NeedSkiping { get; set; }
        static bool CombatResults_NeedsSkiping { get; set; }
        static bool Combats_NeedsSkiping { get; set; }
        static bool Sieges_NeedsSkiping { get; set; }
        static bool PlayedCharacter_NeedsSkipping { get; set; }
        static bool Wars_NeedsSkiping { get; set; }
        static bool VassalContracts_NeedsSkiping { get; set; }
        static bool Opinions_NeedsSkiping { get; set; }
        static bool CourtPositions_NeedsSkiping { get; set; }
        public static void SendDataToFile(string savePath)
        {
            // Reset static state variables
            NeedSkiping = false;
            CombatResults_NeedsSkiping = false;
            Combats_NeedsSkiping = false;
            Sieges_NeedsSkiping = false;
            PlayedCharacter_NeedsSkipping = false;
            Wars_NeedsSkiping = false;
            VassalContracts_NeedsSkiping = false;
            Opinions_NeedsSkiping = false;
            CourtPositions_NeedsSkiping = false;

            CrusaderWarsEnvironment.Logger.Debug($"Starting to write data back to save file: {Path.GetFullPath(savePath)}");
            // Removed resultsFound and combatsFound boolean variables.

            //string tempFilePath = Directory.GetCurrentDirectory() + "\\CrusaderWars_Battle.ck3";
            string tempFilePath = @"./data/save_file_data/gamestate";
            CrusaderWarsEnvironment.Logger.Debug($"Writing to temporary file: {Path.GetFullPath(tempFilePath)}");

            using (var inputFileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var streamReader = new StreamReader(inputFileStream))
            using (StreamWriter streamWriter = new StreamWriter(outputFileStream))
            {
                streamWriter.NewLine = "\n";
                string? line;
                int lineCount = 0;
                while ((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {
                    lineCount++;
                    if (lineCount % 500000 == 0)
                    {
                        CrusaderWarsEnvironment.Logger.Debug($"... Processed {lineCount} lines for writing ...");
                    }

                    //Line Skipper
                    if (NeedSkiping && line == "pending_character_interactions={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Skipping block until 'pending_character_interactions={' is found.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (CombatResults_NeedsSkiping && line == "\t}") // Modified: Match specific indentation
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping CombatResults block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        CombatResults_NeedsSkiping = false;
                        continue; // Added to prevent writing the closing brace
                    }
                    else if (Combats_NeedsSkiping && line == "\t}") // Modified: Match specific indentation
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping Combats block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        Combats_NeedsSkiping = false;
                        continue; // Added to prevent writing the closing brace
                    }
                    // Corrected condition to match the top-level closing brace for the entire sieges block
                    else if (Sieges_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping Sieges block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        Sieges_NeedsSkiping = false;
                        continue; // Added to prevent writing the closing brace
                        // siegesFound = false; // No longer needed
                    }
                    else if (Wars_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping Wars block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        Wars_NeedsSkiping = false;
                        continue;
                    }
                    else if (VassalContracts_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping VassalContracts block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        VassalContracts_NeedsSkiping = false;
                        continue;
                    }
                    else if (Opinions_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping Opinions block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        Opinions_NeedsSkiping = false;
                        continue;
                    }
                    else if (CourtPositions_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping CourtPositions block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        CourtPositions_NeedsSkiping = false;
                        continue;
                    }
                    else if (CourtPositions_NeedsSkiping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping CourtPositions block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        CourtPositions_NeedsSkiping = false;
                        continue;
                    }
                    else if (PlayedCharacter_NeedsSkipping && line == "}")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Finished skipping played_character block.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        PlayedCharacter_NeedsSkipping = false;
                        continue;
                    }
                    else if (NeedSkiping && line == "\tarmy_regiments={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Skipping block until '\tarmy_regiments={' is found.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "\tarmies={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Skipping block until '\tarmies={' is found.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "dead_unprunable={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Skipping block until 'dead_unprunable={' is found.");
                        CrusaderWarsEnvironment.Logger.Debug($"Stopped skipping at line: {line}");
                        NeedSkiping = false;
                    }

                    //Achievements
                    if (line == "\tcan_get_achievements=no")
                    {
                        streamWriter.WriteLine("\tcan_get_achievements=yes");
                        continue;
                    }

                    //Combat Result START
                    else if (line == "\tcombat_results={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified CombatResults block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.CombatResults_Path(), FileType.CombatResults);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED BATTLE RESULTS SENT!");
                        CombatResults_NeedsSkiping = true;
                    }

                    //Combat START
                    else if (line == "\tcombats={")
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified Combats block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Combats_Path(), FileType.Combats);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED COMBATS SENT!");
                        Combats_NeedsSkiping = true;
                    }
                    else if (line == "\tregiments={" && !NeedSkiping)
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified Regiments block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Regiments_Path(), FileType.Regiments);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "\tarmy_regiments={" && !NeedSkiping)
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified ArmyRegiments block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.ArmyRegiments_Path(), FileType.ArmyRegiments);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED ARMY REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "living={" && !NeedSkiping)
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified Living block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Living_Path(), FileType.Living);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED LIVING SENT!");
                        NeedSkiping = true;
                    }
                    // NEW BLOCK: Replace the entire played_character block
                    else if (line.StartsWith("played_character={") && !PlayedCharacter_NeedsSkipping)
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified played_character block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.PlayedCharacter_Path(), FileType.PlayedCharacter);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED PLAYED CHARACTER SENT!");
                        PlayedCharacter_NeedsSkipping = true;
                    }
                    // NEW BLOCK: Replace the entire currently_played_characters block
                    else if (line.StartsWith("currently_played_characters={"))
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified currently_played_characters block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.CurrentlyPlayedCharacters_Path(), FileType.CurrentlyPlayedCharacters);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED CURRENTLY PLAYED CHARACTERS SENT!");
                        continue;
                    }
                    else if (line == "court_positions={" && !CourtPositions_NeedsSkiping)
                    {
                        string tempCourtPositionsPath = DataTEMPFilesPaths.CourtPositions_Path();
                        if (File.Exists(tempCourtPositionsPath) && new FileInfo(tempCourtPositionsPath).Length > 0)
                        {
                            CrusaderWarsEnvironment.Logger.Debug("Writing modified CourtPositions block.");
                            WriteDataToSaveFile(streamWriter, tempCourtPositionsPath, FileType.CourtPositions);
                            CrusaderWarsEnvironment.Logger.Debug("EDITED COURT POSITIONS SENT!");
                            CourtPositions_NeedsSkiping = true;
                        }
                        else
                        {
                            CrusaderWarsEnvironment.Logger.Debug($"Temporary CourtPositions file not found or is empty. Preserving original block.");
                            streamWriter.WriteLine(line);
                        }
                    }
                    else if (line == "court_positions={" && !CourtPositions_NeedsSkiping)
                    {
                        string tempCourtPositionsPath = DataTEMPFilesPaths.CourtPositions_Path();
                        if (File.Exists(tempCourtPositionsPath) && new FileInfo(tempCourtPositionsPath).Length > 0)
                        {
                            CrusaderWarsEnvironment.Logger.Debug("Writing modified CourtPositions block.");
                            WriteDataToSaveFile(streamWriter, tempCourtPositionsPath, FileType.CourtPositions);
                            CrusaderWarsEnvironment.Logger.Debug("EDITED COURT POSITIONS SENT!");
                            CourtPositions_NeedsSkiping = true;
                        }
                        else
                        {
                            CrusaderWarsEnvironment.Logger.Debug($"Temporary CourtPositions file not found or is empty. Preserving original block.");
                            streamWriter.WriteLine(line);
                        }
                    }
                    // NEW BLOCK: Replace the entire sieges block when "sieges={" is encountered
                    else if (line == "sieges={" && !Sieges_NeedsSkiping)
                    {
                        string tempSiegesPath = DataTEMPFilesPaths.Sieges_Path();
                        if (File.Exists(tempSiegesPath))
                        {
                            CrusaderWarsEnvironment.Logger.Debug("Writing modified Sieges block from temporary file.");
                            WriteDataToSaveFile(streamWriter, tempSiegesPath, FileType.Sieges);
                            CrusaderWarsEnvironment.Logger.Debug("EDITED SIEGES SENT!");
                            Sieges_NeedsSkiping = true;
                        }
                        else
                        {
                            // If the temporary Sieges.txt doesn't exist, it means no siege battle occurred
                            // or the data wasn't processed. Preserve the original block.
                            CrusaderWarsEnvironment.Logger.Debug($"Temporary Sieges file not found at {Path.GetFullPath(tempSiegesPath)}. Preserving original sieges block.");
                            streamWriter.WriteLine(line); // Write the "sieges={" line
                            // DO NOT set Sieges_NeedsSkiping = true, so the original content is copied.
                        }
                    }
                    else if (line == "wars={" && !Wars_NeedsSkiping)
                    {
                        CrusaderWarsEnvironment.Logger.Debug("Writing modified Wars block.");
                        WriteDataToSaveFile(streamWriter, DataTEMPFilesPaths.Wars_Path(), FileType.Wars);
                        CrusaderWarsEnvironment.Logger.Debug("EDITED WARS SENT!");
                        Wars_NeedsSkiping = true;
                    }
                    else if (line == "vassal_contracts={" && !VassalContracts_NeedsSkiping)
                    {
                        string tempVassalPath = DataTEMPFilesPaths.VassalContracts_Path();
                        if (File.Exists(tempVassalPath))
                        {
                            CrusaderWarsEnvironment.Logger.Debug("Writing modified VassalContracts block.");
                            WriteDataToSaveFile(streamWriter, tempVassalPath, FileType.VassalContracts);
                            CrusaderWarsEnvironment.Logger.Debug("EDITED VASSAL CONTRACTS SENT!");
                            VassalContracts_NeedsSkiping = true;
                        }
                        else
                        {
                            CrusaderWarsEnvironment.Logger.Debug($"Temporary VassalContracts file not found. Preserving original block.");
                            streamWriter.WriteLine(line);
                        }
                    }
                    else if (line == "opinions={" && !Opinions_NeedsSkiping)
                    {
                        string tempOpinionsPath = DataTEMPFilesPaths.Opinions_Path();
                        if (File.Exists(tempOpinionsPath) && new FileInfo(tempOpinionsPath).Length > 0)
                        {
                            CrusaderWarsEnvironment.Logger.Debug("Writing modified Opinions block.");
                            WriteDataToSaveFile(streamWriter, tempOpinionsPath, FileType.Opinions);
                            CrusaderWarsEnvironment.Logger.Debug("EDITED OPINIONS SENT!");
                            Opinions_NeedsSkiping = true;
                        }
                        else
                        {
                            CrusaderWarsEnvironment.Logger.Debug($"Temporary Opinions file not found or is empty. Preserving original block.");
                            streamWriter.WriteLine(line);
                        }
                    }
                    else if (!NeedSkiping && !CombatResults_NeedsSkiping && !Combats_NeedsSkiping && !Sieges_NeedsSkiping && !PlayedCharacter_NeedsSkipping && !Wars_NeedsSkiping && !VassalContracts_NeedsSkiping && !Opinions_NeedsSkiping && !CourtPositions_NeedsSkiping)
                    {
                        streamWriter.WriteLine(line);
                    }

                }
                CrusaderWarsEnvironment.Logger.Debug("Finished writing to temporary file. All blocks processed.");
                streamWriter.Close();
                streamReader.Close();
                outputFileStream.Close();
                inputFileStream.Close();
            }
            CrusaderWarsEnvironment.Logger.Debug($"Finished writing to temporary file: {Path.GetFullPath(tempFilePath)}");

            //string save_games_path = Properties.Settings.Default.VAR_dir_save;
            //string editedSavePath = Path.Combine(save_games_path, "CrusaderWars_Battle.ck3");
            //File.Delete(savePath);
            //File.Move(tempFilePath, editedSavePath);
        }


        static void WriteDataToSaveFile(StreamWriter streamWriter, string data_file_path, FileType fileType)
        {
            CrusaderWarsEnvironment.Logger.Debug($"Reading entire content from {data_file_path} to write into save file stream.");

            // Read the entire content of the temporary file
            string fileContent = File.ReadAllText(data_file_path);

            // Write the content directly to the stream.
            // Using Write instead of WriteLine prevents adding an extra newline.
            streamWriter.Write(fileContent);

            CrusaderWarsEnvironment.Logger.Debug($"Wrote {fileContent.Length} characters of {fileType} data to save file stream.");
        }

        public enum FileType
        {
            Living,
            CombatResults,
            Combats,
            ArmyRegiments,
            Regiments,
            Sieges,
            PlayedCharacter,
            CurrentlyPlayedCharacters,
            Wars,
            VassalContracts,
            Opinions,
            CourtPositions
        }

        public struct DataFilesPaths
        {
            public static string CombatResults_Path() { return @"./data/save_file_data/CombatResults.txt"; }
            public static string Combats_Path() { return @"./data/save_file_data/Combats.txt"; }
            public static string Regiments_Path() { return @"./data/save_file_data/Regiments.txt"; }
            public static string ArmyRegiments_Path() { return @"./data/save_file_data/ArmyRegiments.txt"; }
            public static string Living_Path() { return @"./data/save_file_data/Living.txt"; }
            public static string Cultures_Path() { return @"./data/save_file_data/Cultures.txt"; }
            public static string Mercenaries_Path() { return @"./data/save_file_data/Mercenaries.txt"; }
            public static string Armies_Path() { return @"./data/save_file_data/Armies.txt"; }
            public static string Counties_Path() { return @"./data/save_file_data/Counties.txt"; }
            public static string Traits_Path() { return @"./data/save_file_data/Traits.txt"; }
            public static string Units_Path() { return @"./data/save_file_data/Units.txt"; }
            public static string CourtPositions_Path() { return @"./data/save_file_data/CourtPositions.txt"; }
            public static string LandedTitles() { return @"./data/save_file_data/LandedTitles.txt"; }
            public static string Accolades() { return @"./data/save_file_data/Accolades.txt"; }
            public static string Dynasties_Path() { return @"./data/save_file_data/Dynasties.txt"; }
            public static string Sieges_Path() { return @"./data/save_file_data/Sieges.txt"; }
            public static string PlayedCharacter_Path() { return @"./data/save_file_data/PlayedCharacter.txt"; }
            public static string CurrentlyPlayedCharacters_Path() { return @"./data/save_file_data/CurrentlyPlayedCharacters.txt"; }
            public static string Wars_Path() { return @"./data/save_file_data/Wars.txt"; }
            public static string VassalContracts_Path() { return @"./data/save_file_data/VassalContracts.txt"; }
            public static string Opinions_Path() { return @"./data/save_file_data/Opinions.txt"; }

        }

        public struct DataTEMPFilesPaths
        {
            public static string CombatResults_Path() { return @"./data/save_file_data/temp/CombatResults.txt"; }
            public static string Combats_Path() { return @"./data/save_file_data/temp/Combats.txt"; }
            public static string Regiments_Path() { return @"./data/save_file_data/temp/Regiments.txt"; }
            public static string ArmyRegiments_Path() { return @"./data/save_file_data/temp/ArmyRegiments.txt"; }
            public static string Living_Path() { return @"./data/save_file_data/temp/Living.txt"; }
            public static string Sieges_Path() { return @"./data/save_file_data/temp/Sieges.txt"; }
            public static string PlayedCharacter_Path() { return @"./data/save_file_data/temp/PlayedCharacter.txt"; }
            public static string CurrentlyPlayedCharacters_Path() { return @"./data/save_file_data/temp/CurrentlyPlayedCharacters.txt"; }
            public static string CourtPositions_Path() { return @"./data/save_file_data/temp/CourtPositions.txt"; }
            public static string Wars_Path() { return @"./data/save_file_data/temp/Wars.txt"; }
            public static string VassalContracts_Path() { return @"./data/save_file_data/temp/VassalContracts.txt"; }
            public static string Opinions_Path() { return @"./data/save_file_data/temp/Opinions.txt"; }

        }

    }
}
