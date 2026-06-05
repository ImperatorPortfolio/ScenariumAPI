using System.Collections.Generic;
using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;

public class ScenariumViewModel
{
    // Existing HUD contract fields
    public int Version = 91;
    public string CampaignDisplayName = "No Campaign";
    public string CampaignState = "Unknown";
    public string CurrentSectorId = "Unknown";
    public List<string> FactionLines = new List<string>();
    public List<string> NodeLines = new List<string>();

    // Runtime summary fields
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
            vm.CampaignDisplayName = "No Campaign Loaded";
            vm.CampaignTitle = vm.CampaignDisplayName;
            vm.ScenarioTitle = "No Active Scenario";
            vm.CampaignState = "Not Loaded";
            vm.CurrentSectorId = "Unknown";
            vm.ScenarioSummary = "Campaign runtime is not loaded.";
            vm.QuestSummary = "No active objective.";
            vm.ObjectiveSummary = "No objective data.";
            vm.RecentActivity = "Waiting for campaign.";
            vm.FactionLines.Add("No faction data.");
            vm.NodeLines.Add("No node data.");
            return vm;
        }

        vm.CampaignDisplayName = Safe(runtime.Campaign.DisplayName, runtime.Campaign.CampaignId);
        vm.CampaignTitle = vm.CampaignDisplayName;
        vm.CampaignState = runtime.Campaign.InitialState.ToString();
        vm.CurrentSectorId = Safe(runtime.Campaign.StartSectorId, "Unknown");
        vm.ScenarioTitle = BuildScenarioTitle(runtime);

        BuildFactionLines(runtime, vm);
        BuildNodeLines(runtime, vm);

        vm.ScenarioSummary = BuildScenarioSummary(runtime, vm);
        vm.QuestSummary = BuildQuestSummary(runtime);
        vm.ObjectiveSummary = BuildObjectiveSummary(runtime);
        vm.RecentActivity = "Runtime HUD data refreshed.";

        return vm;
    }

    static string BuildScenarioTitle(CampaignRuntime runtime)
    {
        if (runtime.Campaign.Scenarios != null && runtime.Campaign.Scenarios.Count > 0)
        {
            ScenarioData scenario = runtime.Campaign.Scenarios[0];
            return Safe(scenario.DisplayName, scenario.ScenarioId);
        }

        return Safe(runtime.Campaign.StartScenarioId, "Active Scenario");
    }

    static void BuildFactionLines(CampaignRuntime runtime, ScenariumViewModel vm)
    {
        vm.FactionLines.Clear();

        if (runtime.Campaign.Factions == null || runtime.Campaign.Factions.Count == 0)
        {
            vm.FactionLines.Add("No faction data.");
            return;
        }

        foreach (FactionData faction in runtime.Campaign.Factions)
        {
            if (faction == null)
                continue;

            vm.FactionLines.Add(Safe(faction.Tag, faction.FactionId) + " | " + Safe(faction.DisplayName, faction.FactionId) + " | " + faction.InitialState.ToString());
        }
    }

    static void BuildNodeLines(CampaignRuntime runtime, ScenariumViewModel vm)
    {
        vm.NodeLines.Clear();

        if (runtime.Campaign.ConquestNodes == null || runtime.Campaign.ConquestNodes.Count == 0)
        {
            vm.NodeLines.Add("No node data.");
            return;
        }

        foreach (ConquestNodeData node in runtime.Campaign.ConquestNodes)
        {
            if (node == null)
                continue;

            string mes = "";

            if (node.Integrations != null)
            {
                foreach (IntegrationBindingData integration in node.Integrations)
                {
                    if (integration == null)
                        continue;

                    if (integration.IntegrationType.ToString() == "MES" && integration.BindingKey == "MES.SpawnGroup")
                    {
                        mes = " | MES: " + integration.BindingValue;
                        break;
                    }
                }
            }

            vm.NodeLines.Add(Safe(node.DisplayName, node.NodeId) + " | " + node.InitialState.ToString() + mes);
        }
    }

    static string BuildScenarioSummary(CampaignRuntime runtime, ScenariumViewModel vm)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Campaign: " + vm.CampaignDisplayName);
        sb.AppendLine("Version: " + Safe(runtime.Campaign.Version, "Unknown"));
        sb.AppendLine("State: " + vm.CampaignState);
        sb.AppendLine("Sector: " + vm.CurrentSectorId);
        sb.AppendLine("Factions: " + vm.FactionLines.Count);
        sb.AppendLine("Nodes: " + vm.NodeLines.Count);

        return sb.ToString();
    }

    static string BuildQuestSummary(CampaignRuntime runtime)
    {
        ConquestNodeData node = GetFirstObjectiveNode(runtime);

        if (node == null)
            return "No campaign objective nodes found.";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Current Campaign Objective");
        sb.AppendLine(Safe(node.DisplayName, node.NodeId));
        sb.AppendLine("");
        sb.AppendLine("Task:");
        sb.AppendLine("Destroy or capture the objective control point.");
        sb.AppendLine("");
        sb.AppendLine("Marker:");
        sb.AppendLine("SCENARIUM_OBJECTIVE_CONTROL");

        return sb.ToString();
    }

    static string BuildObjectiveSummary(CampaignRuntime runtime)
    {
        ConquestNodeData node = GetFirstObjectiveNode(runtime);

        if (node == null)
            return "No objective available.";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Node Id: " + node.NodeId);
        sb.AppendLine("Initial State: " + node.InitialState.ToString());
        sb.AppendLine("Faction: " + Safe(node.FactionTag, "Unknown"));

        if (node.Integrations != null)
        {
            foreach (IntegrationBindingData integration in node.Integrations)
            {
                if (integration == null)
                    continue;

                if (integration.IntegrationType.ToString() == "MES" && integration.BindingKey == "MES.SpawnGroup")
                {
                    sb.AppendLine("MES: " + integration.BindingValue);
                    break;
                }
            }
        }

        return sb.ToString();
    }

    static ConquestNodeData GetFirstObjectiveNode(CampaignRuntime runtime)
    {
        if (runtime == null || runtime.Campaign == null || runtime.Campaign.ConquestNodes == null)
            return null;

        foreach (ConquestNodeData node in runtime.Campaign.ConquestNodes)
        {
            if (node == null)
                continue;

            if (node.InitialState.ToString() == "Revealed")
                return node;
        }

        if (runtime.Campaign.ConquestNodes.Count > 0)
            return runtime.Campaign.ConquestNodes[0];

        return null;
    }

    static string Safe(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
