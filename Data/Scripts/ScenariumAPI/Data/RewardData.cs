using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class RewardData
    {
        public string RewardId;
        public string DisplayName;
        public string Description;
        public ScenariumRewardType RewardType = ScenariumRewardType.None;

        public string TargetId;
        public string TargetFactionTag;
        public string Value;
        public int Amount;

        public List<WorldStateFactData> WorldFactsToSet = new List<WorldStateFactData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (WorldFactsToSet == null) WorldFactsToSet = new List<WorldStateFactData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
