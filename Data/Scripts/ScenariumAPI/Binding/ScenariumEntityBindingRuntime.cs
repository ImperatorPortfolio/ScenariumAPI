using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingRuntime
    {
        readonly ScenariumEntityBindingStore _store = new ScenariumEntityBindingStore();
        readonly Action<string, string, bool> _transitionNode;
        readonly Action<string> _log;

        public ScenariumEntityBindingRuntime(Action<string, string, bool> transitionNode, Action<string> log)
        {
            _transitionNode = transitionNode;
            _log = log;
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public ScenariumEntityBindingStore Store
        {
            get { return _store; }
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

                binding.TransitionApplied = true;
            }
        }

        public string BuildSummary()
        {
            return _store.BuildSummary();
        }
    }
}
