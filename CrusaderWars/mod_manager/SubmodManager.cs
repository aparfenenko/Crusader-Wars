using System.Collections.Generic;
using System.Windows.Forms;

namespace CrusaderWars.mod_manager
{
    /// <summary>
    /// Windows-side facade over the ActiveSubmods store: keeps the application-exit
    /// semantics of the WinForms app while the XML flow lives in Core.
    /// </summary>
    public static class SubmodManager
    {
        public static void LoadActiveSubmods()
        {
            if (!ActiveSubmods.Load())
            {
                Application.Exit();
            }
        }

        public static void SaveActiveSubmods()
        {
            ActiveSubmods.Save();
        }

        public static List<string> GetActiveSubmodsForPlaythrough(string playthroughTag)
        {
            return ActiveSubmods.GetActiveSubmodsForPlaythrough(playthroughTag);
        }

        public static void SetActiveSubmodsForPlaythrough(string playthroughTag, List<string> activeSubmodTags)
        {
            ActiveSubmods.SetActiveSubmodsForPlaythrough(playthroughTag, activeSubmodTags);
        }
    }
}
