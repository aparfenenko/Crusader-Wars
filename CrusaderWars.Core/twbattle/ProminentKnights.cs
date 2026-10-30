using CrusaderWars.Core.Paths;
using CrusaderWars.armies;
using CrusaderWars.unit_mapper;
using CrusaderWars.data.save_file;

namespace CrusaderWars.twbattle
{
    /// <summary>
    /// Assigns the best knights (by prowess) to lead Men-At-Arms units before battle
    /// file generation. Extracted from BattleProcessor so the CLI can run the same
    /// preparation without the UI-bound orchestrator.
    /// </summary>
    public static class ProminentKnights
    {
        public static void Process(List<Army> attacker_armies, List<Army> defender_armies)
        {

            CrusaderWarsEnvironment.Logger.Debug("Processing prominent knights for MAA leadership...");

            var allArmies = attacker_armies.Concat(defender_armies);

            foreach (var army in allArmies)
            {
                if (army.Units == null || !army.Units.Any()) continue;

                // Get all knights from this army
                var armyKnights = new List<Knight>();
                if (army.Knights != null && army.Knights.HasKnights())
                {
                    armyKnights.AddRange(army.Knights.GetKnightsList());
                }

                // Also check merged armies for knights
                if (army.MergedArmies != null)
                {
                    foreach (var mergedArmy in army.MergedArmies)
                    {
                        if (mergedArmy.Knights != null && mergedArmy.Knights.HasKnights())
                        {
                            armyKnights.AddRange(mergedArmy.Knights.GetKnightsList());
                        }
                    }
                }

                if (!armyKnights.Any()) continue;

                // Sort knights by prowess (highest first) to assign best knights to MAA
                var sortedKnights = armyKnights.OrderByDescending(k => k.GetProwess()).ToList();

                // Find MAA units that could be led by knights
                var maaUnits = army.Units.Where(u => u.GetRegimentType() == RegimentType.MenAtArms).ToList();

                // Also check merged armies for MAA units
                if (army.MergedArmies != null)
                {
                    foreach (var mergedArmy in army.MergedArmies)
                    {
                        if (mergedArmy.Units != null)
                        {
                            maaUnits.AddRange(mergedArmy.Units.Where(u => u.GetRegimentType() == RegimentType.MenAtArms));
                        }
                    }
                }

                // Assign knights to lead MAA units
                int knightIndex = 0;
                foreach (var maaUnit in maaUnits)
                {
                    if (knightIndex >= sortedKnights.Count) break;

                    var knight = sortedKnights[knightIndex];
                    maaUnit.KnightCommander = knight;
                    knightIndex++;

                    CrusaderWarsEnvironment.Logger.Debug($"Assigned knight {knight.GetName()} (Prowess: {knight.GetProwess()}) to lead MAA unit {maaUnit.GetName()}");

                    // Remove the knight from its original KnightSystem so it doesn't appear in the combined bodyguard unit.
                    army.Knights?.RemoveKnight(knight);
                    if (army.MergedArmies != null)
                    {
                        foreach (var mergedArmy in army.MergedArmies)
                        {
                            mergedArmy.Knights?.RemoveKnight(knight);
                        }
                    }

                    // Find the combined knights unit and subtract this knight's soldiers.
                    var combinedKnightsUnit = army.Units.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Knight);
                    if (combinedKnightsUnit != null)
                    {
                        int soldiersToRemove = knight.GetSoldiers();
                        combinedKnightsUnit.AddSoldiers(-soldiersToRemove);
                        CrusaderWarsEnvironment.Logger.Debug($"Removed {soldiersToRemove} soldiers from combined knights unit for knight {knight.GetName()}.");
                    }
                }
            }

            CrusaderWarsEnvironment.Logger.Debug("Finished processing prominent knights.");
        }
    }
}
