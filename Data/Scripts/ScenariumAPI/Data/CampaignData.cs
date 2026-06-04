using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class CampaignData
    {
        public string CampaignId;
        public string DisplayName;
        public string Description;
        public string Version;
        public string Author;
        public string StartScenarioId;
        public string StartSectorId;
        public ScenariumCampaignState InitialState = ScenariumCampaignState.Loaded;

        public List<ScenarioData> Scenarios = new List<ScenarioData>();
        public List<FactionData> Factions = new List<FactionData>();
        public List<QuestData> Quests = new List<QuestData>();
        public List<ConquestNodeData> ConquestNodes = new List<ConquestNodeData>();
        public List<RewardData> Rewards = new List<RewardData>();
        public List<WorldStateFactData> InitialWorldState = new List<WorldStateFactData>();
        public List<IntegrationBindingData> Integrations = new List<IntegrationBindingData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (Scenarios == null) Scenarios = new List<ScenarioData>();
            if (Factions == null) Factions = new List<FactionData>();
            if (Quests == null) Quests = new List<QuestData>();
            if (ConquestNodes == null) ConquestNodes = new List<ConquestNodeData>();
            if (Rewards == null) Rewards = new List<RewardData>();
            if (InitialWorldState == null) InitialWorldState = new List<WorldStateFactData>();
            if (Integrations == null) Integrations = new List<IntegrationBindingData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
