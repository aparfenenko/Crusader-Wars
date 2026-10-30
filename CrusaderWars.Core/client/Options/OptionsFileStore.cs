using System;
using System.Xml;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.client
{
    /// <summary>
    /// XML persistence for individual Options.xml entries. Extracted from the WinForms
    /// Options form so domain code (BattleFile) can update settings without the UI;
    /// the form class delegates here.
    /// </summary>
    public static class OptionsFileStore
    {
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
                string file = @".\settings\Options.xml";
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
