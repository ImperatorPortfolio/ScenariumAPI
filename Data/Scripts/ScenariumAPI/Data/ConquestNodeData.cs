using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class ConquestNodeData
    {
        public string NodeId;
        public string DisplayName;
        public string Description;
        public string FactionTag;
        public string ScenarioId;
        public string SectorId;
        public ScenariumConquestNodeType NodeType = ScenariumConquestNodeType.Unknown;
        public ScenariumConquestNodeState InitialState = ScenariumConquestNodeState.Hidden;

        public Vector3DData Position = new Vector3DData();
        public double DiscoveryRadiusMeters = 5000;
        public bool IsDefeatCritical;

        public List<string> RevealsOnCapture = new List<string>();
        public List<string> RevealsOnDestroy = new List<string>();
        public List<string> DisablesOnDestroy = new List<string>();
        public List<RewardData> Rewards = new List<RewardData>();
        public List<IntegrationBindingData> Integrations = new List<IntegrationBindingData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (Position == null) Position = new Vector3DData();
            if (RevealsOnCapture == null) RevealsOnCapture = new List<string>();
            if (RevealsOnDestroy == null) RevealsOnDestroy = new List<string>();
            if (DisablesOnDestroy == null) DisablesOnDestroy = new List<string>();
            if (Rewards == null) Rewards = new List<RewardData>();
            if (Integrations == null) Integrations = new List<IntegrationBindingData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
