using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Integrations.MES
{
    public class MesSpawnRequestStore
    {
        readonly List<MesSpawnRequestData> _requests = new List<MesSpawnRequestData>();

        public IList<MesSpawnRequestData> Requests
        {
            get { return _requests; }
        }

        public int Count
        {
            get { return _requests.Count; }
        }

        public void Clear()
        {
            _requests.Clear();
        }

        public void Add(MesSpawnRequestData request)
        {
            if (request != null)
                _requests.Add(request);
        }

        public bool IsAllowed(string spawnGroupOrEncounterTag)
        {
            if (string.IsNullOrWhiteSpace(spawnGroupOrEncounterTag))
                return false;

            foreach (MesSpawnRequestData request in _requests)
            {
                if (request == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(request.SpawnGroup) &&
                    string.Equals(request.SpawnGroup, spawnGroupOrEncounterTag, System.StringComparison.OrdinalIgnoreCase))
                    return request.Allowed;

                if (!string.IsNullOrWhiteSpace(request.EncounterTag) &&
                    string.Equals(request.EncounterTag, spawnGroupOrEncounterTag, System.StringComparison.OrdinalIgnoreCase))
                    return request.Allowed;
            }

            return false;
        }

        public string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();

            if (_requests.Count == 0)
            {
                sb.AppendLine("No MES spawn requests loaded.");
                return sb.ToString();
            }

            foreach (MesSpawnRequestData request in _requests)
            {
                string binding = !string.IsNullOrWhiteSpace(request.SpawnGroup) ? request.SpawnGroup : request.EncounterTag;
                sb.AppendLine((request.Allowed ? "ALLOW" : "DENY") +
                    " | " + request.NodeId +
                    " | " + binding +
                    " | " + request.NodeState +
                    " | " + request.Reason);
            }

            return sb.ToString();
        }
    }
}
