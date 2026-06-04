using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class ObjectiveData
    {
        public string ObjectiveId;
        public string DisplayName;
        public string Description;
        public ScenariumObjectiveType ObjectiveType = ScenariumObjectiveType.None;
        public ScenariumObjectiveState InitialState = ScenariumObjectiveState.Hidden;

        public string TargetId;
        public string TargetFactionTag;
        public string TargetSectorId;
        public int RequiredCount = 1;

        public List<RewardData> Rewards = new List<RewardData>();
        public List<RequirementData> Requirements = new List<RequirementData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (Rewards == null) Rewards = new List<RewardData>();
            if (Requirements == null) Requirements = new List<RequirementData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
