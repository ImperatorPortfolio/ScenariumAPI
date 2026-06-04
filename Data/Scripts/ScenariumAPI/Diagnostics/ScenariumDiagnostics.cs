using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Integrations.MES;

namespace ScenariumAPI.Diagnostics
{
    public class ScenariumDiagnostics
    {
        public string BuildRuntimeReport(CampaignRuntime runtime, MesBindingBridge mesBridge)
        {
            StringBuilder sb = new StringBuilder();

            if (runtime == null || runtime.Campaign == null || runtime.State == null)
            {
                sb.AppendLine("Runtime: NOT LOADED");
                return sb.ToString();
            }

            sb.AppendLine("Runtime: LOADED");
            sb.AppendLine("Campaign: " + runtime.Campaign.DisplayName + " (" + runtime.State.CampaignId + ")");
            sb.AppendLine("Scenario: " + runtime.State.CurrentScenarioId);
            sb.AppendLine("Sector: " + runtime.State.CurrentSectorId);
            sb.AppendLine("State: " + runtime.State.State);
            sb.AppendLine("Factions: " + runtime.State.Factions.Count);
            sb.AppendLine("Nodes: " + runtime.State.ConquestNodes.Count);
            sb.AppendLine("Quests: " + runtime.State.Quests.Count);

            if (mesBridge != null && mesBridge.Snapshot != null && mesBridge.Snapshot.Permissions != null)
                sb.AppendLine("MES Permissions: " + mesBridge.Snapshot.Permissions.Count);
            else
                sb.AppendLine("MES Permissions: unavailable");

            return sb.ToString();
        }

        public string BuildFactsReport(CampaignRuntime runtime)
        {
            StringBuilder sb = new StringBuilder();

            if (runtime == null || runtime.State == null)
            {
                sb.AppendLine("No runtime facts available.");
                return sb.ToString();
            }

            sb.AppendLine("FACTION FACTS");
            foreach (var faction in runtime.State.Factions)
                sb.AppendLine("Scenarium.Faction." + faction.Tag + ".State=" + faction.State + " Defeated=" + faction.Defeated);

            sb.AppendLine("");
            sb.AppendLine("NODE FACTS");
            foreach (var node in runtime.State.ConquestNodes)
                sb.AppendLine("Scenarium.Node." + node.NodeId + ".State=" + node.State);

            sb.AppendLine("");
            sb.AppendLine("WORLD FACTS");
            sb.AppendLine("WorldState data object present: " + (runtime.State.WorldState != null));

            return sb.ToString();
        }
    }
}
