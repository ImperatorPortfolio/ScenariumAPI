using System;
using ScenariumAPI.Data;

namespace ScenariumAPI.Runtime
{
    public partial class CampaignRuntime
    {
        public void RestoreState(CampaignRuntimeStateData state)
        {
            if (state == null)
                return;

            state.EnsureCollections();
            State = state;
            _log("Runtime state restored: " + state.CampaignId);
        }

        public string GetRuntimeSummary()
        {
            if (Campaign == null || State == null)
                return "No campaign runtime loaded.";

            return "Campaign: " + Campaign.DisplayName +
                " | Scenarios: " + State.Scenarios.Count +
                " | Factions: " + State.Factions.Count +
                " | Nodes: " + State.ConquestNodes.Count;
        }

        public string GetNodeSummaryLines()
        {
            if (Campaign == null || State == null)
                return "No campaign runtime loaded.";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var nodeState in State.ConquestNodes)
            {
                var def = GetNodeDefinition(nodeState.NodeId);
                string name = def != null ? def.DisplayName : nodeState.NodeId;
                sb.AppendLine(nodeState.State + " | " + nodeState.NodeId + " | " + name);
            }

            return sb.ToString();
        }

        public string GetFactionSummaryLines()
        {
            if (State == null)
                return "No faction runtime state loaded.";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var faction in State.Factions)
                sb.AppendLine(faction.Tag + " | " + faction.State + (faction.Defeated ? " | DEFEATED" : ""));

            return sb.ToString();
        }
    }
}
