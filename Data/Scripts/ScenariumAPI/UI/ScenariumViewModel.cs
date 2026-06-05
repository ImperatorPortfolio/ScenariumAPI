using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;

namespace ScenariumAPI.UI
{
    public class ScenariumViewModel
    {
        public string CampaignTitle = "No Campaign";
        public string ScenarioTitle = "No Scenario";
        public string ScenarioSummary = "";
        public string QuestSummary = "";
        public string RecentActivity = "";
        public string ObjectiveSummary = "";

        public static ScenariumViewModel FromRuntime(CampaignRuntime runtime)
        {
            ScenariumViewModel vm = new ScenariumViewModel();

            if (runtime == null || runtime.Campaign == null)
            {
                vm.CampaignTitle = "No Campaign Loaded";
                vm.ScenarioTitle = "No Active Scenario";
                vm.ScenarioSummary = "Campaign runtime is not loaded.";
                vm.QuestSummary = "No quest data available.";
                vm.ObjectiveSummary = "No active objective.";
                return vm;
            }

            vm.CampaignTitle = string.IsNullOrWhiteSpace(runtime.Campaign.DisplayName) ? runtime.Campaign.CampaignId : runtime.Campaign.DisplayName;
            vm.ScenarioTitle = BuildScenarioTitle(runtime);
            vm.ScenarioSummary = BuildScenarioSummary(runtime);
            vm.QuestSummary = BuildQuestSummary(runtime);
            vm.ObjectiveSummary = BuildObjectiveSummary(runtime);
            vm.RecentActivity = BuildRecentActivity(runtime);

            return vm;
        }

        static string BuildScenarioTitle(CampaignRuntime runtime)
        {
            if (runtime.Campaign.Scenarios != null && runtime.Campaign.Scenarios.Count > 0)
            {
                ScenarioData scenario = runtime.Campaign.Scenarios[0];
                return string.IsNullOrWhiteSpace(scenario.DisplayName) ? scenario.ScenarioId : scenario.DisplayName;
            }

            return runtime.Campaign.StartScenarioId ?? "Active Scenario";
        }

        static string BuildScenarioSummary(CampaignRuntime runtime)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Campaign: " + Safe(runtime.Campaign.DisplayName, runtime.Campaign.CampaignId));
            sb.AppendLine("Version: " + Safe(runtime.Campaign.Version, "Unknown"));
            sb.AppendLine("Sector: " + Safe(runtime.Campaign.StartSectorId, "Unknown"));
            sb.AppendLine("");

            int hidden = 0;
            int revealed = 0;
            int destroyed = 0;
            int captured = 0;

            if (runtime.State != null && runtime.State.Nodes != null)
            {
                foreach (var node in runtime.State.Nodes)
                {
                    if (node == null)
                        continue;

                    string state = node.State.ToString();

                    if (state == "Hidden")
                        hidden++;
                    else if (state == "Revealed")
                        revealed++;
                    else if (state == "Destroyed")
                        destroyed++;
                    else if (state == "Captured")
                        captured++;
                }
            }

            sb.AppendLine("Revealed: " + revealed);
            sb.AppendLine("Hidden: " + hidden);
            sb.AppendLine("Destroyed: " + destroyed);
            sb.AppendLine("Captured: " + captured);

            return sb.ToString();
        }

        static string BuildQuestSummary(CampaignRuntime runtime)
        {
            StringBuilder sb = new StringBuilder();

            ConquestNodeRuntimeData active = GetFirstActiveNode(runtime);

            if (active == null)
            {
                sb.AppendLine("No active conquest objective.");
                sb.AppendLine("Campaign may be complete or waiting for new data.");
                return sb.ToString();
            }

            ConquestNodeData data = FindNodeData(runtime, active.NodeId);

            sb.AppendLine("Current Objective");
            sb.AppendLine(Safe(data != null ? data.DisplayName : null, active.NodeId));
            sb.AppendLine("");
            sb.AppendLine("Task:");
            sb.AppendLine("Destroy or capture the objective control point.");
            sb.AppendLine("");
            sb.AppendLine("State: " + active.State);

            return sb.ToString();
        }

        static string BuildObjectiveSummary(CampaignRuntime runtime)
        {
            ConquestNodeRuntimeData active = GetFirstActiveNode(runtime);

            if (active == null)
                return "No active objective.";

            ConquestNodeData data = FindNodeData(runtime, active.NodeId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Node: " + Safe(data != null ? data.DisplayName : null, active.NodeId));
            sb.AppendLine("Node Id: " + active.NodeId);
            sb.AppendLine("State: " + active.State);

            if (data != null && data.Integrations != null)
            {
                foreach (var integration in data.Integrations)
                {
                    if (integration != null && integration.IntegrationType == "MES" && integration.BindingKey == "MES.SpawnGroup")
                    {
                        sb.AppendLine("MES: " + integration.BindingValue);
                        break;
                    }
                }
            }

            sb.AppendLine("Marker: SCENARIUM_OBJECTIVE_CONTROL");
            return sb.ToString();
        }

        static string BuildRecentActivity(CampaignRuntime runtime)
        {
            StringBuilder sb = new StringBuilder();

            ConquestNodeRuntimeData active = GetFirstActiveNode(runtime);

            if (active != null)
            {
                sb.AppendLine("Active objective: " + active.NodeId);
                sb.AppendLine("Waiting for objective marker completion.");
            }
            else
            {
                sb.AppendLine("No active revealed nodes.");
            }

            return sb.ToString();
        }

        static ConquestNodeRuntimeData GetFirstActiveNode(CampaignRuntime runtime)
        {
            if (runtime == null || runtime.State == null || runtime.State.Nodes == null)
                return null;

            foreach (var node in runtime.State.Nodes)
            {
                if (node == null)
                    continue;

                string state = node.State.ToString();

                if (state == "Revealed" || state == "Active")
                    return node;
            }

            return null;
        }

        static ConquestNodeData FindNodeData(CampaignRuntime runtime, string nodeId)
        {
            if (runtime == null || runtime.Campaign == null || runtime.Campaign.ConquestNodes == null)
                return null;

            foreach (var node in runtime.Campaign.ConquestNodes)
            {
                if (node != null && node.NodeId == nodeId)
                    return node;
            }

            return null;
        }

        static string Safe(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
