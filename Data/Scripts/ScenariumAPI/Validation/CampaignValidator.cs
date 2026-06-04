using System.Collections.Generic;
using ScenariumAPI.Data;

namespace ScenariumAPI.Validation
{
    public class CampaignValidator
    {
        public ScenariumDataValidationResult Validate(CampaignData campaign)
        {
            var result = new ScenariumDataValidationResult();

            if (campaign == null)
            {
                result.AddError("Campaign is null.");
                return result;
            }

            campaign.EnsureCollections();

            if (string.IsNullOrWhiteSpace(campaign.CampaignId))
                result.AddError("CampaignId is required.");

            if (string.IsNullOrWhiteSpace(campaign.DisplayName))
                result.AddWarning("DisplayName is empty.");

            var scenarioIds = new HashSet<string>();
            var factionTags = new HashSet<string>();
            var nodeIds = new HashSet<string>();
            var questIds = new HashSet<string>();

            foreach (var scenario in campaign.Scenarios)
            {
                if (scenario == null)
                {
                    result.AddError("Null scenario entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(scenario.ScenarioId))
                    result.AddError("Scenario has empty ScenarioId.");
                else if (!scenarioIds.Add(scenario.ScenarioId))
                    result.AddError("Duplicate ScenarioId: " + scenario.ScenarioId);
            }

            foreach (var faction in campaign.Factions)
            {
                if (faction == null)
                {
                    result.AddError("Null faction entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(faction.Tag))
                    result.AddError("Faction has empty Tag.");
                else if (!factionTags.Add(faction.Tag))
                    result.AddError("Duplicate faction Tag: " + faction.Tag);
            }

            foreach (var quest in campaign.Quests)
            {
                if (quest == null)
                {
                    result.AddError("Null quest entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(quest.QuestId))
                    result.AddError("Quest has empty QuestId.");
                else if (!questIds.Add(quest.QuestId))
                    result.AddError("Duplicate QuestId: " + quest.QuestId);

                if (!string.IsNullOrWhiteSpace(quest.ScenarioId) && !scenarioIds.Contains(quest.ScenarioId))
                    result.AddWarning("Quest references missing ScenarioId: " + quest.QuestId + " -> " + quest.ScenarioId);
            }

            foreach (var node in campaign.ConquestNodes)
            {
                if (node == null)
                {
                    result.AddError("Null conquest node entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.NodeId))
                    result.AddError("Conquest node has empty NodeId.");
                else if (!nodeIds.Add(node.NodeId))
                    result.AddError("Duplicate NodeId: " + node.NodeId);

                if (!string.IsNullOrWhiteSpace(node.FactionTag) && !factionTags.Contains(node.FactionTag))
                    result.AddWarning("Node references missing faction tag: " + node.NodeId + " -> " + node.FactionTag);

                if (!string.IsNullOrWhiteSpace(node.ScenarioId) && !scenarioIds.Contains(node.ScenarioId))
                    result.AddWarning("Node references missing ScenarioId: " + node.NodeId + " -> " + node.ScenarioId);
            }

            foreach (var node in campaign.ConquestNodes)
            {
                if (node == null) continue;

                node.EnsureCollections();

                foreach (string reveal in node.RevealsOnCapture)
                    if (!nodeIds.Contains(reveal))
                        result.AddWarning("Node RevealsOnCapture references missing node: " + node.NodeId + " -> " + reveal);

                foreach (string reveal in node.RevealsOnDestroy)
                    if (!nodeIds.Contains(reveal))
                        result.AddWarning("Node RevealsOnDestroy references missing node: " + node.NodeId + " -> " + reveal);

                foreach (string disable in node.DisablesOnDestroy)
                    if (!nodeIds.Contains(disable))
                        result.AddWarning("Node DisablesOnDestroy references missing node: " + node.NodeId + " -> " + disable);
            }

            return result;
        }
    }
}
