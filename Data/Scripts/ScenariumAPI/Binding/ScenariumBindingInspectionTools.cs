using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Integrations.MES;

namespace ScenariumAPI.Binding
{
    public class ScenariumBindingInspectionTools
    {
        readonly CampaignRuntime _runtime;
        readonly MesBindingBridge _mesBridge;

        public ScenariumBindingInspectionTools(CampaignRuntime runtime, MesBindingBridge mesBridge)
        {
            _runtime = runtime;
            _mesBridge = mesBridge;
        }

        public string BuildMesPermissionSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MES ALLOWED SPAWN GROUPS");

            if (_mesBridge == null || _mesBridge.Snapshot == null || _mesBridge.Snapshot.Permissions == null)
            {
                sb.AppendLine("MES bridge snapshot unavailable.");
                return sb.ToString();
            }

            foreach (var permission in _mesBridge.Snapshot.Permissions)
            {
                if (permission == null)
                    continue;

                string binding = !string.IsNullOrWhiteSpace(permission.SpawnGroup)
                    ? permission.SpawnGroup
                    : permission.EncounterTag;

                sb.AppendLine(binding + " = " + permission.Allowed + " | Node=" + permission.NodeId + " | State=" + permission.NodeState);
            }

            return sb.ToString();
        }

        public string BuildLiveCandidateSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("LIVE CANDIDATE GRIDS");

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            int count = 0;

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                string nodeId;
                string spawnGroup;
                bool matched = TryMatchGrid(grid, out nodeId, out spawnGroup);

                if (!matched)
                    continue;

                count++;

                sb.AppendLine(grid.EntityId + " | " + grid.DisplayName + " | SpawnGroup=" + spawnGroup + " | Node=" + nodeId);
            }

            if (count == 0)
                sb.AppendLine("No live candidate grids matched known MES binding identities.");

            return sb.ToString();
        }

        public string BuildVerifySummary()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(BuildMesPermissionSummary());
            sb.AppendLine("");
            sb.AppendLine(BuildLiveCandidateSummary());

            return sb.ToString();
        }

        public bool TryMatchEntity(long entityId, out string nodeId, out string spawnGroup, out string gridName)
        {
            nodeId = null;
            spawnGroup = null;
            gridName = null;

            IMyEntity entity;
            if (!MyAPIGateway.Entities.TryGetEntityById(entityId, out entity))
                return false;

            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid == null)
                return false;

            gridName = grid.DisplayName;
            return TryMatchGrid(grid, out nodeId, out spawnGroup);
        }

        public List<long> GetCandidateEntityIds()
        {
            List<long> ids = new List<long>();
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                string nodeId;
                string spawnGroup;
                if (TryMatchGrid(grid, out nodeId, out spawnGroup))
                    ids.Add(grid.EntityId);
            }

            return ids;
        }

        bool TryMatchGrid(IMyCubeGrid grid, out string nodeId, out string spawnGroup)
        {
            nodeId = null;
            spawnGroup = null;

            if (grid == null || _mesBridge == null || _mesBridge.Snapshot == null || _mesBridge.Snapshot.Permissions == null)
                return false;

            string name = grid.DisplayName ?? "";

            foreach (var permission in _mesBridge.Snapshot.Permissions)
            {
                if (permission == null)
                    continue;

                string binding = !string.IsNullOrWhiteSpace(permission.SpawnGroup)
                    ? permission.SpawnGroup
                    : permission.EncounterTag;

                if (string.IsNullOrWhiteSpace(binding))
                    continue;

                if (name.IndexOf(binding, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf(permission.NodeId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nodeId = permission.NodeId;
                    spawnGroup = binding;
                    return true;
                }
            }

            return false;
        }
    }
}
