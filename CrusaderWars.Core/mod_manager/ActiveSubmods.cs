using System;
using System.Collections.Generic;

namespace CrusaderWars.mod_manager
{
    /// <summary>
    /// In-memory store of active submods per playthrough (AGOT / Realms in Exile / The
    /// Fallen Eagle). The Windows UI (SubmodManager) populates it from
    /// settings\ActiveSubmods.xml; battle generation reads it to pick mappers and
    /// localization. Split out of SubmodManager so the battle pipeline has no dependency
    /// on the mod-management UI.
    /// </summary>
    public static class ActiveSubmods
    {
        private static Dictionary<string, List<string>> ByPlaythrough { get; set; } = new Dictionary<string, List<string>>();

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
