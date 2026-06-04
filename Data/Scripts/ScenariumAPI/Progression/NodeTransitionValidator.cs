using System;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;

namespace ScenariumAPI.Progression
{
    public class NodeTransitionValidator
    {
        readonly CampaignRuntime _runtime;

        public NodeTransitionValidator(CampaignRuntime runtime)
        {
            _runtime = runtime;
        }

        public NodeTransitionResult Validate(string nodeId, string transition, bool force)
        {
            if (_runtime == null || _runtime.Campaign == null || _runtime.State == null)
                return NodeTransitionResult.Denied(nodeId, transition, "Unknown", "Campaign runtime is not loaded.");

            if (string.IsNullOrWhiteSpace(nodeId))
                return NodeTransitionResult.Denied(nodeId, transition, "Unknown", "Node id is empty.");

            ConquestNodeData definition = _runtime.GetNodeDefinition(nodeId);
            if (definition == null)
                return NodeTransitionResult.Denied(nodeId, transition, "Missing", "Node is not defined by the campaign pack.");

            ConquestNodeRuntimeStateData state = _runtime.GetNodeState(nodeId);
            if (state == null)
                return NodeTransitionResult.Denied(nodeId, transition, "Missing", "Node runtime state is missing.");

            string previous = state.State.ToString();

            if (force)
                return NodeTransitionResult.Approved(nodeId, transition, previous, true);

            if (IsFactionDefeated(definition.FactionTag))
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Faction is already defeated.");

            if (state.State == ScenariumConquestNodeState.Hidden)
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Hidden nodes cannot transition without force.");

            if (state.State == ScenariumConquestNodeState.Disabled)
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Disabled nodes cannot transition without force.");

            if (state.State == ScenariumConquestNodeState.Destroyed && IsDestroy(transition))
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Node is already destroyed.");

            if (state.State == ScenariumConquestNodeState.Captured && IsCapture(transition))
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Node is already captured.");

            if (state.State == ScenariumConquestNodeState.Destroyed || state.State == ScenariumConquestNodeState.Captured)
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Completed nodes cannot transition again without force.");

            if (!IsDestroy(transition) && !IsCapture(transition))
                return NodeTransitionResult.Denied(nodeId, transition, previous, "Unknown transition. Use destroyed or captured.");

            return NodeTransitionResult.Approved(nodeId, transition, previous, false);
        }

        bool IsDestroy(string transition)
        {
            return transition != null &&
                (transition.Equals("destroy", StringComparison.OrdinalIgnoreCase) ||
                 transition.Equals("destroyed", StringComparison.OrdinalIgnoreCase));
        }

        bool IsCapture(string transition)
        {
            return transition != null &&
                (transition.Equals("capture", StringComparison.OrdinalIgnoreCase) ||
                 transition.Equals("captured", StringComparison.OrdinalIgnoreCase));
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
    }
}
