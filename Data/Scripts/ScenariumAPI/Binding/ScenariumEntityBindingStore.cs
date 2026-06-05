using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingStore
    {
        readonly Dictionary<long, ScenariumEntityBindingData> _bindings = new Dictionary<long, ScenariumEntityBindingData>();

        public int Count
        {
            get { return _bindings.Count; }
        }

        public IEnumerable<ScenariumEntityBindingData> Bindings
        {
            get { return _bindings.Values; }
        }

        public bool Contains(long entityId)
        {
            return _bindings.ContainsKey(entityId);
        }

        public void Bind(long entityId, string nodeId, string spawnGroup, string gridName)
        {
            ScenariumEntityBindingData data;
            if (!_bindings.TryGetValue(entityId, out data))
            {
                data = new ScenariumEntityBindingData();
                data.EntityId = entityId;
                _bindings[entityId] = data;
            }

            data.NodeId = nodeId;
            data.SpawnGroup = spawnGroup;
            data.GridName = gridName;
            data.Closed = false;
            data.TransitionApplied = false;
        }

        public bool Unbind(long entityId)
        {
            return _bindings.Remove(entityId);
        }

        public ScenariumEntityBindingData Get(long entityId)
        {
            ScenariumEntityBindingData data;
            _bindings.TryGetValue(entityId, out data);
            return data;
        }

        public void Clear()
        {
            _bindings.Clear();
        }

        public string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();

            if (_bindings.Count == 0)
            {
                sb.AppendLine("No persisted Scenarium entity bindings.");
                return sb.ToString();
            }

            foreach (ScenariumEntityBindingData binding in _bindings.Values)
            {
                sb.AppendLine(binding.EntityId +
                    " | Node=" + binding.NodeId +
                    " | SpawnGroup=" + binding.SpawnGroup +
                    " | Grid=" + binding.GridName +
                    " | " + (binding.Closed ? "Closed" : "Open") +
                    " | " + (binding.TransitionApplied ? "Applied" : "Pending"));
            }

            return sb.ToString();
        }
    }
}
