using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingRuntime
    {
        readonly CampaignRuntime _runtime;
        readonly Action<string, string, bool> _transitionNode;
        readonly Action<string> _log;
        readonly ScenariumEntityBindingStore _store;
        readonly ScenariumEntityBindingSaveData _data;

        int _lastScanGrids;
        int _lastScanNew;
        int _lastScanMatched;

        public ScenariumEntityBindingRuntime(CampaignRuntime runtime, Action<string, string, bool> transitionNode, Action<string> log)
        {
            _runtime = runtime;
            _transitionNode = transitionNode;
            _log = log;
            _store = new ScenariumEntityBindingStore(log);
            _data = _store.Load();
            _data.EnsureCollections();
        }

        public int Count
        {
            get { return _data.Bindings.Count; }
        }

        public int ScanByCampaignBindings()
        {
            _lastScanGrids = 0;
            _lastScanNew = 0;
            _lastScanMatched = 0;

            if (_runtime == null || _runtime.Campaign == null)
            {
                Log("Entity bind scan failed: no campaign loaded.");
                return 0;
            }

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                _lastScanGrids++;

                string gridText = (grid.DisplayName ?? "") + " " + (grid.Name ?? "");

                foreach (ConquestNodeData node in _runtime.Campaign.ConquestNodes)
                {
                    if (node == null)
                        continue;

                    node.EnsureCollections();

                    foreach (IntegrationBindingData binding in node.Integrations)
                    {
                        if (binding == null || !binding.Enabled)
                            continue;

                        if (binding.IntegrationType != ScenariumIntegrationType.MES)
                            continue;

                        if (string.IsNullOrWhiteSpace(binding.BindingValue))
                            continue;

                        if (ContainsIgnoreCase(gridText, binding.BindingValue))
                        {
                            _lastScanMatched++;
                            if (AddOrUpdate(grid, node, binding))
                                _lastScanNew++;
                        }
                    }
                }
            }

            _store.Save(_data);
            Log("Entity bind scan complete. Grids: " + _lastScanGrids + " Matches: " + _lastScanMatched + " New bindings: " + _lastScanNew);
            return _lastScanNew;
        }

        bool AddOrUpdate(IMyCubeGrid grid, ConquestNodeData node, IntegrationBindingData binding)
        {
            ScenariumEntityBindingData existing = Find(grid.EntityId);
            bool isNew = existing == null;

            if (existing == null)
            {
                existing = new ScenariumEntityBindingData();
                existing.EntityId = grid.EntityId;
                _data.Bindings.Add(existing);
            }

            existing.GridName = grid.DisplayName;
            existing.NodeId = node.NodeId;
            existing.FactionTag = node.FactionTag;
            existing.BindingKey = binding.BindingKey;
            existing.BindingValue = binding.BindingValue;
            existing.CaptureMode = "Destroy";

            return isNew;
        }

        public void Update()
        {
            foreach (ScenariumEntityBindingData binding in _data.Bindings)
            {
                if (binding == null || binding.TransitionApplied)
                    continue;

                IMyEntity entity;
                bool exists = MyAPIGateway.Entities.TryGetEntityById(binding.EntityId, out entity);

                if (!exists || entity == null || entity.Closed || entity.MarkedForClose)
                {
                    string transition = IsCapture(binding.CaptureMode) ? "captured" : "destroyed";
                    Log("Bound Scenarium entity closed: " + binding.NodeId + " -> " + transition);

                    if (_transitionNode != null)
                        _transitionNode(binding.NodeId, transition, false);

                    binding.TransitionApplied = true;
                    _store.Save(_data);
                }
            }
        }

        public void Clear()
        {
            _data.Bindings.Clear();
            _store.Save(_data);
            Log("Persistent entity bindings cleared.");
        }

        public bool Unbind(long entityId)
        {
            for (int i = _data.Bindings.Count - 1; i >= 0; i--)
            {
                if (_data.Bindings[i].EntityId == entityId)
                {
                    _data.Bindings.RemoveAt(i);
                    _store.Save(_data);
                    Log("Entity binding removed: " + entityId);
                    return true;
                }
            }

            Log("Entity binding not found: " + entityId);
            return false;
        }

        public string BuildSummary()
        {
            StringBuilder sb = new StringBuilder();

            if (_data.Bindings.Count == 0)
            {
                sb.AppendLine("No persistent entity bindings.");
                return sb.ToString();
            }

            foreach (ScenariumEntityBindingData binding in _data.Bindings)
            {
                sb.AppendLine(binding.NodeId + " | Entity " + binding.EntityId + " | " + binding.GridName + " | " + binding.BindingValue + " | " + (binding.TransitionApplied ? "Applied" : "Pending"));
            }

            return sb.ToString();
        }

        public string BuildDiagnostics()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Persistent Entity Binding Runtime");
            sb.AppendLine("Stored bindings: " + _data.Bindings.Count);
            sb.AppendLine("Last scan grids: " + _lastScanGrids);
            sb.AppendLine("Last scan matches: " + _lastScanMatched);
            sb.AppendLine("Last scan new bindings: " + _lastScanNew);
            sb.AppendLine("");
            sb.Append(BuildSummary());
            return sb.ToString();
        }

        ScenariumEntityBindingData Find(long entityId)
        {
            foreach (ScenariumEntityBindingData binding in _data.Bindings)
            {
                if (binding != null && binding.EntityId == entityId)
                    return binding;
            }

            return null;
        }

        bool ContainsIgnoreCase(string text, string value)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(value))
                return false;

            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool IsCapture(string mode)
        {
            return mode != null && (mode.Equals("Capture", StringComparison.OrdinalIgnoreCase) || mode.Equals("Captured", StringComparison.OrdinalIgnoreCase));
        }

        void Log(string message)
        {
            if (_log != null)
                _log(message);
        }
    }
}
