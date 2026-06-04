using System;
using System.Collections.Generic;

namespace ScenariumAPI.Runtime
{
    public class NodeDetectionRuntime
    {
        readonly ScenariumGridTracker _tracker = new ScenariumGridTracker();
        readonly CampaignRuntime _campaignRuntime;
        readonly Action<string> _log;

        public NodeDetectionRuntime(CampaignRuntime campaignRuntime, Action<string> log)
        {
            _campaignRuntime = campaignRuntime;
            _log = log;
        }

        public int TrackedCount
        {
            get { return _tracker.Count; }
        }

        public int Scan()
        {
            int found = _tracker.ScanForMarkedGrids();
            _log("Scenarium grid scan complete. Marked grids found: " + found);
            return found;
        }

        public void Update()
        {
            List<TrackedNodeGrid> destroyed = _tracker.GetDestroyedTrackedGrids();

            foreach (TrackedNodeGrid grid in destroyed)
            {
                _log("Tracked node grid closed/destroyed: " + grid.NodeId + " (" + grid.GridName + ")");

                if (_campaignRuntime != null && _campaignRuntime.Campaign != null)
                    _campaignRuntime.DestroyNode(grid.NodeId);
                else
                    _log("No campaign runtime loaded. Could not apply node destruction: " + grid.NodeId);
            }
        }

        public string GetTrackedSummary()
        {
            return _tracker.BuildTrackedSummary();
        }

        public void Clear()
        {
            _tracker.Clear();
            _log("Scenarium grid tracker cleared.");
        }
    }
}
