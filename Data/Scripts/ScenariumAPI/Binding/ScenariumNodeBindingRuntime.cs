using System;
using System.Text;

namespace ScenariumAPI.Binding
{
    public class ScenariumNodeBindingRuntime
    {
        readonly ScenariumNodeTracker _tracker = new ScenariumNodeTracker();
        readonly Action<string, string, bool> _transitionNode;
        readonly Action<string> _log;

        int _lastScanFound;

        public ScenariumNodeBindingRuntime(Action<string, string, bool> transitionNode, Action<string> log)
        {
            _transitionNode = transitionNode;
            _log = log;
        }

        public int TrackedCount
        {
            get { return _tracker.Count; }
        }

        public int Scan()
        {
            _lastScanFound = _tracker.Scan();

            if (_log != null)
                _log("Gameplay node binding scan complete. Marked grids found: " + _lastScanFound);

            return _lastScanFound;
        }

        public void Update()
        {
            var closed = _tracker.GetClosedUnapplied();

            foreach (var tracked in closed)
            {
                string transition = "destroyed";

                if (!string.IsNullOrWhiteSpace(tracked.CaptureMode) &&
                    (tracked.CaptureMode.Equals("Capture", StringComparison.OrdinalIgnoreCase) ||
                     tracked.CaptureMode.Equals("Captured", StringComparison.OrdinalIgnoreCase)))
                    transition = "captured";

                if (_log != null)
                    _log("Gameplay node grid closed: " + tracked.NodeId + " -> " + transition);

                if (_transitionNode != null)
                    _transitionNode(tracked.NodeId, transition, false);

                tracked.TransitionApplied = true;
            }
        }

        public void Clear()
        {
            _tracker.Clear();

            if (_log != null)
                _log("Gameplay node bindings cleared.");
        }

        public string BuildTrackedSummary()
        {
            return _tracker.BuildSummary();
        }

        public string BuildDiagnostics()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Gameplay Node Binding Runtime");
            sb.AppendLine("Tracked grids: " + _tracker.Count);
            sb.AppendLine("Last scan found: " + _lastScanFound);
            sb.AppendLine("");
            sb.Append(_tracker.BuildSummary());
            return sb.ToString();
        }
    }
}
