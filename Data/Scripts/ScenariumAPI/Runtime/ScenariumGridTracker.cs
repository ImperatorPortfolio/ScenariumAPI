using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using System;
using System.Collections.Generic;

namespace ScenariumAPI.Runtime
{
    public class TrackedNodeGrid
    {
        public long EntityId;
        public string GridName;
        public string NodeId;
        public string FactionTag;
        public bool IsClosed;
        public bool WasMarkedDestroyed;
    }

    public class ScenariumGridTracker
    {
        readonly Dictionary<long, TrackedNodeGrid> _tracked = new Dictionary<long, TrackedNodeGrid>();

        public IEnumerable<TrackedNodeGrid> Tracked
        {
            get { return _tracked.Values; }
        }

        public int Count
        {
            get { return _tracked.Count; }
        }

        public void Clear()
        {
            _tracked.Clear();
        }

        public int ScanForMarkedGrids()
        {
            int found = 0;
            var entities = new HashSet<IMyEntity>();

            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                NodeMarkerData marker;

                if (TryGetMarker(grid, out marker))
                {
                    TrackGrid(grid, marker);
                    found++;
                }
            }

            return found;
        }

        public bool TryGetMarker(IMyCubeGrid grid, out NodeMarkerData marker)
        {
            marker = null;

            if (grid == null)
                return false;

            if (NodeMarkerParser.TryParse(grid.DisplayName, out marker))
                return true;

            if (NodeMarkerParser.TryParse(grid.Name, out marker))
                return true;

            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, slim => slim != null && slim.FatBlock is IMyTerminalBlock);

            foreach (IMySlimBlock slim in blocks)
            {
                if (slim == null || slim.FatBlock == null)
                    continue;

                IMyTerminalBlock terminal = slim.FatBlock as IMyTerminalBlock;

                if (terminal == null)
                    continue;

                NodeMarkerData blockMarker;

                if (NodeMarkerParser.TryParse(terminal.CustomData, out blockMarker))
                {
                    marker = blockMarker;
                    return true;
                }

                if (NodeMarkerParser.TryParse(terminal.CustomName, out blockMarker))
                {
                    marker = blockMarker;
                    return true;
                }
            }

            return false;
        }

        public void TrackGrid(IMyCubeGrid grid, NodeMarkerData marker)
        {
            if (grid == null || marker == null || string.IsNullOrWhiteSpace(marker.NodeId))
                return;

            TrackedNodeGrid tracked;
            if (!_tracked.TryGetValue(grid.EntityId, out tracked))
            {
                tracked = new TrackedNodeGrid();
                tracked.EntityId = grid.EntityId;
                _tracked[grid.EntityId] = tracked;
            }

            tracked.GridName = grid.DisplayName;
            tracked.NodeId = marker.NodeId;
            tracked.FactionTag = marker.FactionTag;
            tracked.IsClosed = grid.Closed;
        }

        public List<TrackedNodeGrid> GetDestroyedTrackedGrids()
        {
            List<TrackedNodeGrid> destroyed = new List<TrackedNodeGrid>();

            foreach (var kv in _tracked)
            {
                TrackedNodeGrid tracked = kv.Value;

                IMyEntity entity;
                bool exists = MyAPIGateway.Entities.TryGetEntityById(tracked.EntityId, out entity);

                if (!exists || entity == null || entity.Closed || entity.MarkedForClose)
                {
                    tracked.IsClosed = true;

                    if (!tracked.WasMarkedDestroyed)
                    {
                        tracked.WasMarkedDestroyed = true;
                        destroyed.Add(tracked);
                    }
                }
            }

            return destroyed;
        }

        public string BuildTrackedSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (_tracked.Count == 0)
            {
                sb.AppendLine("No tracked Scenarium grids.");
                return sb.ToString();
            }

            foreach (TrackedNodeGrid tracked in _tracked.Values)
                sb.AppendLine(tracked.NodeId + " | " + tracked.GridName + " | Entity " + tracked.EntityId + (tracked.IsClosed ? " | CLOSED" : ""));

            return sb.ToString();
        }
    }
}
