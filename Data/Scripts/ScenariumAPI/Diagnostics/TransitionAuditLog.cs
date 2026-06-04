using System.Collections.Generic;
using System.Text;
using ScenariumAPI.Progression;

namespace ScenariumAPI.Diagnostics
{
    public class TransitionAuditLog
    {
        readonly List<NodeTransitionResult> _entries = new List<NodeTransitionResult>();

        public int Count
        {
            get { return _entries.Count; }
        }

        public void Record(NodeTransitionResult result)
        {
            if (result == null)
                return;

            _entries.Add(result);

            if (_entries.Count > 100)
                _entries.RemoveAt(0);
        }

        public string BuildSummary(int max)
        {
            StringBuilder sb = new StringBuilder();

            if (_entries.Count == 0)
            {
                sb.AppendLine("No transition audit entries.");
                return sb.ToString();
            }

            int start = _entries.Count - max;
            if (start < 0)
                start = 0;

            for (int i = start; i < _entries.Count; i++)
            {
                NodeTransitionResult r = _entries[i];
                sb.AppendLine((r.Allowed ? (r.Forced ? "FORCED" : "ALLOW") : "DENY") +
                    " | " + r.NodeId +
                    " | " + r.RequestedTransition +
                    " | " + r.PreviousState + " -> " + r.NewState +
                    " | " + r.Reason);
            }

            return sb.ToString();
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
