using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class QuestData
    {
        public string QuestId;
        public string DisplayName;
        public string Description;
        public string ScenarioId;
        public ScenariumQuestStateType InitialState = ScenariumQuestStateType.Hidden;

        public List<ObjectiveData> Objectives = new List<ObjectiveData>();
        public List<string> PrerequisiteQuestIds = new List<string>();
        public List<string> RevealsQuestIds = new List<string>();
        public List<RewardData> Rewards = new List<RewardData>();
        public List<IntegrationBindingData> Integrations = new List<IntegrationBindingData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (Objectives == null) Objectives = new List<ObjectiveData>();
            if (PrerequisiteQuestIds == null) PrerequisiteQuestIds = new List<string>();
            if (RevealsQuestIds == null) RevealsQuestIds = new List<string>();
            if (Rewards == null) Rewards = new List<RewardData>();
            if (Integrations == null) Integrations = new List<IntegrationBindingData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
