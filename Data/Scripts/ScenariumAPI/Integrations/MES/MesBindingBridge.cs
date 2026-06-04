using System;
using System.Text;
using ScenariumAPI.Data;
using ScenariumAPI.Runtime;

namespace ScenariumAPI.Integrations.MES
{
    public class MesBindingBridge
    {
        readonly CampaignRuntime _runtime;
        readonly Action<string> _log;

        MesBindingSnapshot _snapshot = new MesBindingSnapshot();

        public MesBindingSnapshot Snapshot
        {
            get { return _snapshot; }
        }

        public MesBindingBridge(CampaignRuntime runtime, Action<string> log)
        {
            _runtime = runtime;
            _log = log;
        }

        public MesBindingSnapshot Refresh()
        {
            _snapshot = new MesBindingSnapshot();
            _snapshot.EnsureCollections();

            if (_runtime == null || _runtime.Campaign == null || _runtime.State == null)
            {
                _log("MES bridge refresh failed: no campaign runtime loaded.");
                return _snapshot;
            }

            foreach (var nodeDef in _runtime.Campaign.ConquestNodes)
            {
                if (nodeDef == null)
                    continue;

                nodeDef.EnsureCollections();

                var nodeState = _runtime.GetNodeState(nodeDef.NodeId);

                foreach (var binding in nodeDef.Integrations)
                {
                    if (binding == null || !binding.Enabled)
                        continue;

                    if (binding.IntegrationType != ScenariumIntegrationType.MES)
                        continue;

                    if (!IsMesSpawnBinding(binding.BindingKey))
                        continue;

                    MesSpawnPermission permission = BuildPermission(nodeDef, nodeState, binding);
                    _snapshot.Permissions.Add(permission);
                }
            }

            _log("MES bridge refreshed. Permissions: " + _snapshot.Permissions.Count);
            return _snapshot;
        }

        bool IsMesSpawnBinding(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return key.Equals("MES.SpawnGroup", StringComparison.OrdinalIgnoreCase)
                || key.Equals("MES.EncounterTag", StringComparison.OrdinalIgnoreCase)
                || key.Equals("EncounterTag", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SpawnGroup", StringComparison.OrdinalIgnoreCase);
        }

        MesSpawnPermission BuildPermission(ConquestNodeData nodeDef, ConquestNodeRuntimeStateData nodeState, IntegrationBindingData binding)
        {
            MesSpawnPermission permission = new MesSpawnPermission();

            permission.NodeId = nodeDef.NodeId;
            permission.FactionTag = nodeDef.FactionTag;
            permission.NodeState = nodeState != null ? nodeState.State.ToString() : "MissingRuntimeState";

            if (binding.BindingKey != null && binding.BindingKey.IndexOf("SpawnGroup", StringComparison.OrdinalIgnoreCase) >= 0)
                permission.SpawnGroup = binding.BindingValue;
            else
                permission.EncounterTag = binding.BindingValue;

            if (nodeState == null)
            {
                permission.Allowed = false;
                permission.Reason = "Missing runtime node state.";
                return permission;
            }

            if (IsFactionDefeated(nodeDef.FactionTag))
            {
                permission.Allowed = false;
                permission.Reason = "Faction defeated.";
                return permission;
            }

            if (nodeState.State == ScenariumConquestNodeState.Revealed ||
                nodeState.State == ScenariumConquestNodeState.Active ||
                nodeState.State == ScenariumConquestNodeState.Contested)
            {
                permission.Allowed = true;
                permission.Reason = "Node is spawnable.";
                return permission;
            }

            permission.Allowed = false;
            permission.Reason = "Node state is " + nodeState.State + ".";
            return permission;
        }

        bool IsFactionDefeated(string factionTag)
        {
            if (_runtime == null || _runtime.State == null || string.IsNullOrWhiteSpace(factionTag))
                return false;

            foreach (var faction in _runtime.State.Factions)
            {
                if (string.Equals(faction.Tag, factionTag, StringComparison.OrdinalIgnoreCase))
                    return faction.Defeated || faction.State == ScenariumFactionStateType.Defeated;
            }

            return false;
        }

        public bool IsSpawnAllowed(string spawnGroupOrEncounterTag)
        {
            if (string.IsNullOrWhiteSpace(spawnGroupOrEncounterTag))
                return false;

            _snapshot.EnsureCollections();

            foreach (var permission in _snapshot.Permissions)
            {
                if (permission == null)
                    continue;

                if (string.Equals(permission.SpawnGroup, spawnGroupOrEncounterTag, StringComparison.OrdinalIgnoreCase))
                    return permission.Allowed;

                if (string.Equals(permission.EncounterTag, spawnGroupOrEncounterTag, StringComparison.OrdinalIgnoreCase))
                    return permission.Allowed;
            }

            return false;
        }

        public string BuildSummary(bool allowedOnly, bool deniedOnly)
        {
            _snapshot.EnsureCollections();

            StringBuilder sb = new StringBuilder();

            if (_snapshot.Permissions.Count == 0)
            {
                sb.AppendLine("No MES permissions loaded. Run /scen reload then /scen mes refresh.");
                return sb.ToString();
            }

            foreach (var permission in _snapshot.Permissions)
            {
                if (permission == null)
                    continue;

                if (allowedOnly && !permission.Allowed)
                    continue;

                if (deniedOnly && permission.Allowed)
                    continue;

                string binding = !string.IsNullOrWhiteSpace(permission.SpawnGroup) ? permission.SpawnGroup : permission.EncounterTag;

                sb.AppendLine((permission.Allowed ? "ALLOW" : "DENY") + " | " +
                    permission.NodeId + " | " +
                    binding + " | " +
                    permission.NodeState + " | " +
                    permission.Reason);
            }

            if (sb.Length == 0)
                sb.AppendLine("No permissions matched this filter.");

            return sb.ToString();
        }
    }
}
