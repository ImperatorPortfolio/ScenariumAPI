using System.Collections.Generic;
using ScenariumAPI.Runtime;

namespace ScenariumAPI
{
    public class ScenariumViewModel
    {
        public int Version;
        public bool CampaignLoaded;
        public string CampaignId;
        public string CampaignDisplayName;
        public string CampaignState;
        public string CurrentScenarioId;
        public string CurrentSectorId;
        public List<string> FactionLines = new List<string>();
        public List<string> NodeLines = new List<string>();

        public static ScenariumViewModel FromRuntime(CampaignRuntime runtime)
        {
            ScenariumViewModel model = new ScenariumViewModel();

            if (runtime == null || runtime.Campaign == null || runtime.State == null)
                return model;

            model.CampaignLoaded = true;
            model.CampaignId = runtime.State.CampaignId;
            model.CampaignDisplayName = runtime.Campaign.DisplayName;
            model.CampaignState = runtime.State.State.ToString();
            model.CurrentScenarioId = runtime.State.CurrentScenarioId;
            model.CurrentSectorId = runtime.State.CurrentSectorId;

            foreach (var faction in runtime.State.Factions)
                model.FactionLines.Add(faction.Tag + "     " + faction.State + (faction.Defeated ? "     DEFEATED" : ""));

            foreach (var node in runtime.State.ConquestNodes)
            {
                var def = runtime.GetNodeDefinition(node.NodeId);
                string name = def != null ? def.DisplayName : node.NodeId;
                model.NodeLines.Add("[" + node.State + "]  " + node.NodeId + "  —  " + name);
            }

            model.Version = model.FactionLines.Count + model.NodeLines.Count + model.CampaignState.GetHashCode();

            return model;
        }
    }
}
