using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class ScenarioData
    {
        public string ScenarioId;
        public string DisplayName;
        public string Description;
        public string SectorId;
        public ScenariumScenarioState InitialState = ScenariumScenarioState.Locked;

        public List<string> RequiredWorldFacts = new List<string>();
        public List<RequirementData> Requirements = new List<RequirementData>();
        public List<string> StartingQuestIds = new List<string>();
        public List<string> StartingConquestNodeIds = new List<string>();
        public List<string> UnlocksScenarioIds = new List<string>();
        public List<RewardData> CompletionRewards = new List<RewardData>();
        public List<IntegrationBindingData> Integrations = new List<IntegrationBindingData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (RequiredWorldFacts == null) RequiredWorldFacts = new List<string>();
            if (Requirements == null) Requirements = new List<RequirementData>();
            if (StartingQuestIds == null) StartingQuestIds = new List<string>();
            if (StartingConquestNodeIds == null) StartingConquestNodeIds = new List<string>();
            if (UnlocksScenarioIds == null) UnlocksScenarioIds = new List<string>();
            if (CompletionRewards == null) CompletionRewards = new List<RewardData>();
            if (Integrations == null) Integrations = new List<IntegrationBindingData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
