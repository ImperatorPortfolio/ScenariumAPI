using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class WorldStateFactData
    {
        public string Key;
        public string Value;
        public string Scope;
        public bool BoolValue;
        public int IntValue;
        public double DoubleValue;
    }

    public class WorldStateData
    {
        public List<WorldStateFactData> Facts = new List<WorldStateFactData>();

        public void EnsureCollections()
        {
            if (Facts == null) Facts = new List<WorldStateFactData>();
        }
    }
}
