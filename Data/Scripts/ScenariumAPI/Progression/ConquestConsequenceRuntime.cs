using System;
using ScenariumAPI.Runtime;
using ScenariumAPI.Events;
using ScenariumAPI.Data;

namespace ScenariumAPI.Progression
{
    public class ConquestConsequenceRuntime
    {
        readonly CampaignRuntime _runtime;
        readonly ScenariumEventBus _events;

        public ConquestConsequenceRuntime(CampaignRuntime runtime, ScenariumEventBus events)
        {
            _runtime = runtime;
            _events = events;
        }

        public bool DestroyNode(string nodeId)
        {
            if (_runtime == null || _runtime.Campaign == null || _runtime.State == null)
            {
                _events.Publish(ScenariumEventType.Unknown, nodeId, "Destroy node failed: runtime not loaded.", "", "");
                return false;
            }

            var before = _runtime.GetNodeState(nodeId);
            string previous = before != null ? before.State.ToString() : "Missing";

            _runtime.DestroyNode(nodeId);

            var after = _runtime.GetNodeState(nodeId);
            string next = after != null ? after.State.ToString() : "Missing";

            _events.Publish(ScenariumEventType.NodeDestroyed, nodeId, "Node destroyed.", previous, next);

            ConquestNodeData nodeDef = _runtime.GetNodeDefinition(nodeId);
            if (nodeDef != null)
            {
                nodeDef.EnsureCollections();
                foreach (string revealedId in nodeDef.RevealsOnDestroy)
                {
                    var revealed = _runtime.GetNodeState(revealedId);
                    if (revealed != null)
                        _events.Publish(ScenariumEventType.NodeRevealed, revealedId, "Node reveal consequence applied.", "Hidden", revealed.State.ToString());
                }
            }

            return true;
        }

        public bool CaptureNode(string nodeId)
        {
            if (_runtime == null || _runtime.Campaign == null || _runtime.State == null)
            {
                _events.Publish(ScenariumEventType.Unknown, nodeId, "Capture node failed: runtime not loaded.", "", "");
                return false;
            }

            var before = _runtime.GetNodeState(nodeId);
            string previous = before != null ? before.State.ToString() : "Missing";

            _runtime.CaptureNode(nodeId);

            var after = _runtime.GetNodeState(nodeId);
            string next = after != null ? after.State.ToString() : "Missing";

            _events.Publish(ScenariumEventType.NodeCaptured, nodeId, "Node captured.", previous, next);
            return true;
        }
    }
}
