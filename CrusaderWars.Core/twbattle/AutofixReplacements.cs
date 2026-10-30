using System.Collections.Generic;

namespace CrusaderWars.twbattle
{
    /// <summary>
    /// Unit replacements decided by the autofix flow (BattleProcessor) and consumed
    /// during battle file generation (UnitsFile). Split out of BattleProcessor so
    /// battle file generation has no dependency on the UI-bound orchestrator.
    /// </summary>
    public static class AutofixReplacements
    {
        public static Dictionary<string, (string replacementKey, bool isSiege)> Replacements { get; private set; } =
            new Dictionary<string, (string, bool)>();

        public static void Set(Dictionary<string, (string replacementKey, bool isSiege)> replacements)
        {
            Replacements = replacements ?? new Dictionary<string, (string, bool)>();
        }

        public static void Clear()
        {
            Replacements.Clear();
        }

        public static bool TryGetReplacement(string key, out (string replacementKey, bool isSiege) replacement)
        {
            return Replacements.TryGetValue(key, out replacement);
        }
    }
}
