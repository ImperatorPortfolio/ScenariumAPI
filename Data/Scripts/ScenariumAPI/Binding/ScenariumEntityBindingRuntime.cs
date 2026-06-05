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

        public ScenariumEntityBindingRuntime(CampaignRuntime runtime, Action<string> log, Action<string, string, bool> transitionNode)
        {
            _log = log;
            _transitionNode = transitionNode;
        }

        public void BindFromMesSpawn(long entityId, string nodeId, string spawnGroup, string gridName)
        {
            Bind(entityId, nodeId, spawnGroup, gridName);
        }

        public void Bind(long entityId, string nodeId, string spawnGroup, string gridName)
        {
            _store.Bind(entityId, nodeId, spawnGroup, gridName);
            if (_log != null)
                _log("Entity bound: " + entityId + " -> " + nodeId + " (" + spawnGroup + ")");
        }

        public bool Unbind(long entityId) { return _store.Unbind(entityId); }
        public void Clear() { _store.Clear(); }
        public string BuildSummary() { return _store.BuildSummary(); }
        public string BuildDiagnostics() { return _store.BuildSummary(); }
        public int ScanByCampaignBindings() { return 0; }

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
                if (_transitionNode != null)
                    _transitionNode(binding.NodeId, "destroyed", false);

                binding.TransitionApplied = true;
            }
        }
    }
}
