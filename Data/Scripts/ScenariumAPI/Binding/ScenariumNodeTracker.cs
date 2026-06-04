using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Binding
{
    public class TrackedScenariumNodeGrid
    {
        public long EntityId;
        public string GridName;
        public string NodeId;
        public string FactionTag;
        public string NodeType;
        public string CaptureMode;
        public bool Closed;
        public bool TransitionApplied;
    }

    public class ScenariumNodeTracker
    {
        readonly Dictionary<long, TrackedScenariumNodeGrid> _tracked = new Dictionary<long, TrackedScenariumNodeGrid>();

        public int Count
        {
            get { return _tracked.Count; }
        }

        public IEnumerable<TrackedScenariumNodeGrid> Tracked
        {
            get { return _tracked.Values; }
        }

        public void Clear()
        {
            _tracked.Clear();
        }

        public int Scan()
        {
            int found = 0;
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();

            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                ScenariumNodeMarker marker;

                if (TryGetMarker(grid, out marker))
                {
                    Track(grid, marker);
                    found++;
                }
            }

            return found;
        }

        public bool TryGetMarker(IMyCubeGrid grid, out ScenariumNodeMarker marker)
        {
            marker = null;

            if (grid == null)
                return false;

            if (ScenariumNodeMarker.TryParse(grid.DisplayName, out marker))
                return true;

            if (ScenariumNodeMarker.TryParse(grid.Name, out marker))
                return true;

            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, slim => slim != null && slim.FatBlock is IMyTerminalBlock);

            foreach (IMySlimBlock slim in blocks)
            {
                IMyTerminalBlock terminal = slim.FatBlock as IMyTerminalBlock;
                if (terminal == null)
                    continue;

                ScenariumNodeMarker blockMarker;

                if (ScenariumNodeMarker.TryParse(terminal.CustomData, out blockMarker))
                {
                    marker = blockMarker;
                    return true;
                }

                if (ScenariumNodeMarker.TryParse(terminal.CustomName, out blockMarker))
                {
                    marker = blockMarker;
                    return true;
                }
            }

            return false;
        }

        public void Track(IMyCubeGrid grid, ScenariumNodeMarker marker)
        {
            if (grid == null || marker == null || string.IsNullOrWhiteSpace(marker.NodeId))
                return;

            TrackedScenariumNodeGrid tracked;
            if (!_tracked.TryGetValue(grid.EntityId, out tracked))
            {
                tracked = new TrackedScenariumNodeGrid();
                tracked.EntityId = grid.EntityId;
                _tracked[grid.EntityId] = tracked;
            }

            tracked.GridName = grid.DisplayName;
            tracked.NodeId = marker.NodeId;
            tracked.FactionTag = marker.FactionTag;
            tracked.NodeType = marker.NodeType;
            tracked.CaptureMode = marker.CaptureMode;
            tracked.Closed = grid.Closed || grid.MarkedForClose;
        }

        public List<TrackedScenariumNodeGrid> GetClosedUnapplied()
        {
            List<TrackedScenariumNodeGrid> closed = new List<TrackedScenariumNodeGrid>();

            foreach (TrackedScenariumNodeGrid tracked in _tracked.Values)
            {
                if (tracked.TransitionApplied)
                    continue;

                IMyEntity entity;
                bool exists = MyAPIGateway.Entities.TryGetEntityById(tracked.EntityId, out entity);

                if (!exists || entity == null || entity.Closed || entity.MarkedForClose)
                {
                    tracked.Closed = true;
                    closed.Add(tracked);
                }
            }

            return closed;
        }

        public string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();

            if (_tracked.Count == 0)
            {
                sb.AppendLine("No bound Scenarium node grids.");
                return sb.ToString();
            }

            foreach (TrackedScenariumNodeGrid tracked in _tracked.Values)
            {
                sb.AppendLine(tracked.NodeId +
                    " | " + tracked.GridName +
                    " | Entity " + tracked.EntityId +
                    " | " + (tracked.Closed ? "Closed" : "Open") +
                    " | " + (tracked.TransitionApplied ? "Applied" : "Pending"));
            }

            return sb.ToString();
        }
    }
}
