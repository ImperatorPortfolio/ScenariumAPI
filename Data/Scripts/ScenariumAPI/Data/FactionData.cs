using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class FactionData
    {
        public string FactionId;
        public string Tag;
        public string DisplayName;
        public string Description;
        public ScenariumFactionStateType InitialState = ScenariumFactionStateType.Peacetime;
        public bool CanBeDefeated = true;

        public List<string> HomeSectorIds = new List<string>();
        public List<string> StartingConquestNodeIds = new List<string>();
        public List<IntegrationBindingData> Integrations = new List<IntegrationBindingData>();
        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (HomeSectorIds == null) HomeSectorIds = new List<string>();
            if (StartingConquestNodeIds == null) StartingConquestNodeIds = new List<string>();
            if (Integrations == null) Integrations = new List<IntegrationBindingData>();
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
