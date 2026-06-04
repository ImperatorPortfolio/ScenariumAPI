using System;
using System.Collections.Generic;
using ScenariumAPI.Data;

namespace ScenariumAPI.Runtime
{
    public partial class CampaignRuntime
    {
        public CampaignData Campaign { get; private set; }
        public CampaignRuntimeStateData State { get; private set; }

        readonly Action<string> _log;

        public CampaignRuntime(Action<string> log)
        {
            _log = log;
        }

        public void LoadCampaign(CampaignData campaign)
        {
            Campaign = campaign;
            Campaign.EnsureCollections();

            State = new CampaignRuntimeStateData();
            State.CampaignId = campaign.CampaignId;
            State.State = ScenariumCampaignState.Active;
            State.CurrentScenarioId = campaign.StartScenarioId;
            State.CurrentSectorId = campaign.StartSectorId;
            State.EnsureCollections();

            foreach (var scenario in campaign.Scenarios)
            {
                State.Scenarios.Add(new ScenarioRuntimeStateData
                {
                    ScenarioId = scenario.ScenarioId,
                    State = scenario.InitialState
                });
            }

            foreach (var faction in campaign.Factions)
            {
                State.Factions.Add(new FactionRuntimeStateData
                {
                    FactionId = faction.FactionId,
                    Tag = faction.Tag,
                    State = faction.InitialState,
                    Defeated = faction.InitialState == ScenariumFactionStateType.Defeated
                });
            }

            foreach (var quest in campaign.Quests)
            {
                State.Quests.Add(new QuestRuntimeStateData
                {
                    QuestId = quest.QuestId,
                    State = quest.InitialState
                });

                foreach (var objective in quest.Objectives)
                {
                    State.Objectives.Add(new ObjectiveRuntimeStateData
                    {
                        ObjectiveId = objective.ObjectiveId,
                        State = objective.InitialState,
                        CurrentCount = 0
                    });
                }
            }

            foreach (var node in campaign.ConquestNodes)
            {
                State.ConquestNodes.Add(new ConquestNodeRuntimeStateData
                {
                    NodeId = node.NodeId,
                    State = node.InitialState,
                    OwningFactionTag = node.FactionTag,
                    Discovered = node.InitialState != ScenariumConquestNodeState.Hidden
                });
            }

            State.WorldState = new WorldStateData();
            State.WorldState.EnsureCollections();

            foreach (var fact in campaign.InitialWorldState)
                State.WorldState.Facts.Add(fact);

            _log("Campaign loaded: " + campaign.DisplayName);
        }

        public ConquestNodeData GetNodeDefinition(string nodeId)
        {
            if (Campaign == null || Campaign.ConquestNodes == null) return null;

            foreach (var node in Campaign.ConquestNodes)
            {
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                    return node;
            }

            return null;
        }

        public ConquestNodeRuntimeStateData GetNodeState(string nodeId)
        {
            if (State == null || State.ConquestNodes == null) return null;

            foreach (var node in State.ConquestNodes)
            {
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                    return node;
            }

            return null;
        }

        public bool DestroyNode(string nodeId)
        {
            var definition = GetNodeDefinition(nodeId);
            var state = GetNodeState(nodeId);

            if (definition == null || state == null)
            {
                _log("Destroy failed. Node not found: " + nodeId);
                return false;
            }

            state.State = ScenariumConquestNodeState.Destroyed;
            state.Discovered = true;
            _log("Node destroyed: " + definition.DisplayName);

            definition.EnsureCollections();

            foreach (string revealId in definition.RevealsOnDestroy)
                RevealNode(revealId);

            foreach (string disableId in definition.DisablesOnDestroy)
                DisableNode(disableId);

            if (definition.IsDefeatCritical)
                EvaluateFactionDefeat(definition.FactionTag);

            return true;
        }

        public bool CaptureNode(string nodeId)
        {
            var definition = GetNodeDefinition(nodeId);
            var state = GetNodeState(nodeId);

            if (definition == null || state == null)
            {
                _log("Capture failed. Node not found: " + nodeId);
                return false;
            }

            state.State = ScenariumConquestNodeState.Captured;
            state.Discovered = true;
            _log("Node captured: " + definition.DisplayName);

            definition.EnsureCollections();

            foreach (string revealId in definition.RevealsOnCapture)
                RevealNode(revealId);

            if (definition.IsDefeatCritical)
                EvaluateFactionDefeat(definition.FactionTag);

            return true;
        }

        public bool RevealNode(string nodeId)
        {
            var definition = GetNodeDefinition(nodeId);
            var state = GetNodeState(nodeId);

            if (definition == null || state == null)
            {
                _log("Reveal failed. Node not found: " + nodeId);
                return false;
            }

            if (state.State == ScenariumConquestNodeState.Hidden)
                state.State = ScenariumConquestNodeState.Revealed;

            state.Discovered = true;
            _log("Node revealed: " + definition.DisplayName);
            return true;
        }

        public bool DisableNode(string nodeId)
        {
            var definition = GetNodeDefinition(nodeId);
            var state = GetNodeState(nodeId);

            if (definition == null || state == null)
            {
                _log("Disable failed. Node not found: " + nodeId);
                return false;
            }

            state.State = ScenariumConquestNodeState.Disabled;
            _log("Node disabled: " + definition.DisplayName);
            return true;
        }

        void EvaluateFactionDefeat(string factionTag)
        {
            if (Campaign == null || State == null) return;

            bool hasRemainingCritical = false;

            foreach (var def in Campaign.ConquestNodes)
            {
                if (def == null || !def.IsDefeatCritical) continue;
                if (!string.Equals(def.FactionTag, factionTag, StringComparison.OrdinalIgnoreCase)) continue;

                var state = GetNodeState(def.NodeId);
                if (state == null) continue;

                if (state.State != ScenariumConquestNodeState.Destroyed &&
                    state.State != ScenariumConquestNodeState.Captured &&
                    state.State != ScenariumConquestNodeState.Disabled)
                {
                    hasRemainingCritical = true;
                    break;
                }
            }

            if (!hasRemainingCritical)
            {
                foreach (var faction in State.Factions)
                {
                    if (string.Equals(faction.Tag, factionTag, StringComparison.OrdinalIgnoreCase))
                    {
                        faction.State = ScenariumFactionStateType.Defeated;
                        faction.Defeated = true;
                        _log("Faction defeated: " + factionTag);
                        return;
                    }
                }
            }
        }
    }
}
