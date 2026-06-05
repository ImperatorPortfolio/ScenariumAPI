using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Integrations.MES;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingRuntime
    {
        readonly ScenariumEntityBindingStore _store = new ScenariumEntityBindingStore();
        readonly Action<string, string, bool> _transitionNode;
        readonly Action<string> _log;

        CampaignRuntime _runtime;
        MesBindingBridge _mesBridge;
        ScenariumBindingInspectionTools _inspection;

        int _lastScanMatched;
        int _lastScanEntities;

        public ScenariumEntityBindingRuntime(Action<string, string, bool> transitionNode, Action<string> log)
        {
            _transitionNode = transitionNode;
            _log = log;
        }

        public ScenariumEntityBindingRuntime(CampaignRuntime runtime, Action<string> log, Action<string, string, bool> transitionNode)
        {
            _runtime = runtime;
            _log = log;
            _transitionNode = transitionNode;
        }

        public ScenariumEntityBindingRuntime(CampaignRuntime runtime, MesBindingBridge mesBridge, Action<string> log)
        {
            _runtime = runtime;
            _mesBridge = mesBridge;
            _log = log;
            _transitionNode = null;
            _inspection = new ScenariumBindingInspectionTools(_runtime, _mesBridge);
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public ScenariumEntityBindingStore Store
        {
            get { return _store; }
        }

        public void SetContext(CampaignRuntime runtime, MesBindingBridge mesBridge)
        {
            _runtime = runtime;
            _mesBridge = mesBridge;
            _inspection = new ScenariumBindingInspectionTools(_runtime, _mesBridge);
        }

        public void Bind(long entityId, string nodeId, string spawnGroup, string gridName)
        {
            _store.Bind(entityId, nodeId, spawnGroup, gridName);

            if (_log != null)
                _log("Entity bound: " + entityId + " -> " + nodeId + " (" + spawnGroup + ")");
        }

        public bool Unbind(long entityId)
        {
            bool removed = _store.Unbind(entityId);

            if (_log != null)
                _log(removed ? "Entity binding removed: " + entityId : "Entity binding not found: " + entityId);

            return removed;
        }

        public void Clear()
        {
            _store.Clear();

            if (_log != null)
                _log("Persistent entity bindings cleared.");
        }

        public int ScanByCampaignBindings()
        {
            if (_inspection == null)
                _inspection = new ScenariumBindingInspectionTools(_runtime, _mesBridge);

            List<long> candidates = _inspection.GetCandidateEntityIds();
            _lastScanEntities = candidates.Count;
            _lastScanMatched = 0;

            foreach (long entityId in candidates)
            {
                if (_store.Contains(entityId))
                    continue;

                string nodeId;
                string spawnGroup;
                string gridName;

                if (_inspection.TryMatchEntity(entityId, out nodeId, out spawnGroup, out gridName))
                {
                    Bind(entityId, nodeId, spawnGroup, gridName);
                    _lastScanMatched++;
                }
            }

            if (_log != null)
                _log("Entity binding scan complete. Candidates: " + candidates.Count + " Newly bound: " + _lastScanMatched);

            return _lastScanMatched;
        }

        public void Update()
        {
            List<ScenariumEntityBindingData> closed = new List<ScenariumEntityBindingData>();

            foreach (ScenariumEntityBindingData binding in _store.Bindings)
            {
                if (binding.TransitionApplied)
                    continue;

                IMyEntity entity;
                bool exists = MyAPIGateway.Entities.TryGetEntityById(binding.EntityId, out entity);

                if (!exists || entity == null || entity.Closed || entity.MarkedForClose)
                {
                    binding.Closed = true;
                    closed.Add(binding);
                }
            }

            foreach (ScenariumEntityBindingData binding in closed)
            {
                if (_log != null)
                    _log("Bound entity closed: " + binding.EntityId + " -> " + binding.NodeId);

                if (_transitionNode != null)
                    _transitionNode(binding.NodeId, "destroyed", false);
                else if (_log != null)
                    _log("Bound entity transition requires command runtime callback. Node: " + binding.NodeId);

                binding.TransitionApplied = true;
            }
        }

        public string BuildSummary()
        {
            return _store.BuildSummary();
        }

        public string BuildDiagnostics()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Persistent Entity Binding Runtime");
            sb.AppendLine("Bindings: " + _store.Count);
            sb.AppendLine("Last scan candidates: " + _lastScanEntities);
            sb.AppendLine("Last scan newly bound: " + _lastScanMatched);
            sb.AppendLine("");
            sb.Append(_store.BuildSummary());
            return sb.ToString();
        }
    }
}
