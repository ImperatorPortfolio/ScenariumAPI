using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingStore
    {
        readonly Dictionary<long, ScenariumEntityBindingData> _bindings = new Dictionary<long, ScenariumEntityBindingData>();

        public int Count { get { return _bindings.Count; } }
        public IEnumerable<ScenariumEntityBindingData> Bindings { get { return _bindings.Values; } }
        public bool Contains(long entityId) { return _bindings.ContainsKey(entityId); }

        public bool HasOpenBindingForNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            foreach (ScenariumEntityBindingData binding in _bindings.Values)
            {
                if (binding == null)
                    continue;

                if (binding.Closed || binding.TransitionApplied)
                    continue;

                if (string.Equals(binding.NodeId, nodeId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
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

        public bool Unbind(long entityId) { return _bindings.Remove(entityId); }
        public void Clear() { _bindings.Clear(); }
        public string BuildSummary() { return "Bindings: " + _bindings.Count; }
    }
}
