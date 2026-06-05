using System;
using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Objectives
{
    public class ObjectiveBindingStore
    {
        readonly Dictionary<string, ObjectiveData> _byNode = new Dictionary<string, ObjectiveData>(StringComparer.OrdinalIgnoreCase);

        public void Clear()
        {
            _byNode.Clear();
        }

        public void Add(ObjectiveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.NodeId))
                return;

            _byNode[data.NodeId] = data;
        }

        public ObjectiveData GetForNode(string nodeId)
        {
            ObjectiveData data;
            _byNode.TryGetValue(nodeId, out data);
            return data;
        }

        public string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in _byNode)
            {
                ObjectiveData objective = pair.Value;
                sb.AppendLine(objective.NodeId + " | " + objective.ObjectiveType + " | " + objective.TargetBlockName + " | " + objective.OnCompleteTransition);
            }

            if (sb.Length == 0)
                sb.AppendLine("No campaign objective bindings loaded.");

            return sb.ToString();
        }
    }
}
