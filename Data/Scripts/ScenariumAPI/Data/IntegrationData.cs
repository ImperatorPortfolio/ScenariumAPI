using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class IntegrationBindingData
    {
        public string IntegrationId;
        public ScenariumIntegrationType IntegrationType = ScenariumIntegrationType.None;
        public string BindingKey;
        public string BindingValue;
        public string TargetId;
        public bool Enabled = true;

        public List<TagData> Tags = new List<TagData>();

        public void EnsureCollections()
        {
            if (Tags == null) Tags = new List<TagData>();
        }
    }
}
