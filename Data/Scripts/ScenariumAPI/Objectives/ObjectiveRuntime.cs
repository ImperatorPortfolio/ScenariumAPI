using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace ScenariumAPI.Objectives
{
    public class ObjectiveRuntime
    {
        readonly ObjectiveBindingStore _store = new ObjectiveBindingStore();
        readonly Action<string, string, bool> _transitionNode;
        readonly Action<string> _log;

        readonly Dictionary<long, TrackedObjectiveBlock> _tracked = new Dictionary<long, TrackedObjectiveBlock>();

        public ObjectiveRuntime(Action<string, string, bool> transitionNode, Action<string> log)
        {
            _transitionNode = transitionNode;
            _log = log;
        }

        public ObjectiveBindingStore Store
        {
            get { return _store; }
        }

        public void Clear()
        {
            _store.Clear();
            _tracked.Clear();
        }

        public void AddObjective(ObjectiveData data)
        {
            _store.Add(data);
        }

        public void BindSpawnedGrid(string nodeId, IMyCubeGrid grid)
        {
            if (grid == null || string.IsNullOrWhiteSpace(nodeId))
                return;

            ObjectiveData objective = _store.GetForNode(nodeId);

            if (objective == null || !objective.IsControlBlockDestroyed)
                return;

            if (string.IsNullOrWhiteSpace(objective.TargetBlockName))
                return;

            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, slim => slim != null && slim.FatBlock != null);

            foreach (IMySlimBlock slim in blocks)
            {
                Sandbox.ModAPI.IMyTerminalBlock terminal = slim.FatBlock as Sandbox.ModAPI.IMyTerminalBlock;
                if (terminal == null)
                    continue;

                string name = terminal.CustomName ?? terminal.DisplayNameText ?? "";

                if (name.IndexOf(objective.TargetBlockName, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                TrackedObjectiveBlock tracked = new TrackedObjectiveBlock();
                tracked.EntityId = terminal.EntityId;
                tracked.NodeId = nodeId;
                tracked.ObjectiveId = objective.ObjectiveId;
                tracked.ObjectiveType = objective.ObjectiveType;
                tracked.TargetBlockName = objective.TargetBlockName;
                tracked.Transition = string.IsNullOrWhiteSpace(objective.OnCompleteTransition) ? "destroyed" : objective.OnCompleteTransition;
                tracked.Completed = false;
                _tracked[tracked.EntityId] = tracked;

                Log("Objective control block bound: " + tracked.TargetBlockName + " -> " + nodeId + " EntityId=" + tracked.EntityId);
                return;
            }

            Log("Objective control block not found on spawned grid for node " + nodeId + ": " + objective.TargetBlockName);
        }

        public void Update()
        {
            List<TrackedObjectiveBlock> completed = new List<TrackedObjectiveBlock>();

            foreach (TrackedObjectiveBlock tracked in _tracked.Values)
            {
                if (tracked.Completed)
                    continue;

                IMyEntity entity;
                bool exists = MyAPIGateway.Entities.TryGetEntityById(tracked.EntityId, out entity);

                if (!exists || entity == null || entity.Closed || entity.MarkedForClose)
                    completed.Add(tracked);
            }

            foreach (TrackedObjectiveBlock tracked in completed)
            {
                tracked.Completed = true;
                Log("Objective completed by control block destruction: " + tracked.NodeId + " / " + tracked.ObjectiveId);

                if (_transitionNode != null)
                    _transitionNode(tracked.NodeId, tracked.Transition, false);
            }
        }

        public string BuildSummary()
        {
            return _store.BuildSummary();
        }

        void Log(string text)
        {
            if (_log != null)
                _log(text);
        }

        class TrackedObjectiveBlock
        {
            public long EntityId;
            public string NodeId;
            public string ObjectiveId;
            public string ObjectiveType;
            public string TargetBlockName;
            public string Transition;
            public bool Completed;
        }
    }
}
