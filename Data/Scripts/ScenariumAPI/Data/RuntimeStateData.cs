using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class CampaignRuntimeStateData
    {
        public string CampaignId;
        public ScenariumCampaignState State = ScenariumCampaignState.NotLoaded;
        public string CurrentScenarioId;
        public string CurrentSectorId;

        public List<ScenarioRuntimeStateData> Scenarios = new List<ScenarioRuntimeStateData>();
        public List<FactionRuntimeStateData> Factions = new List<FactionRuntimeStateData>();
        public List<QuestRuntimeStateData> Quests = new List<QuestRuntimeStateData>();
        public List<ObjectiveRuntimeStateData> Objectives = new List<ObjectiveRuntimeStateData>();
        public List<ConquestNodeRuntimeStateData> ConquestNodes = new List<ConquestNodeRuntimeStateData>();
        public WorldStateData WorldState = new WorldStateData();

        public void EnsureCollections()
        {
            if (Scenarios == null) Scenarios = new List<ScenarioRuntimeStateData>();
            if (Factions == null) Factions = new List<FactionRuntimeStateData>();
            if (Quests == null) Quests = new List<QuestRuntimeStateData>();
            if (Objectives == null) Objectives = new List<ObjectiveRuntimeStateData>();
            if (ConquestNodes == null) ConquestNodes = new List<ConquestNodeRuntimeStateData>();
            if (WorldState == null) WorldState = new WorldStateData();
            WorldState.EnsureCollections();
        }
    }

    public class ScenarioRuntimeStateData
    {
        public string ScenarioId;
        public ScenariumScenarioState State;
    }

    public class FactionRuntimeStateData
    {
        public string FactionId;
        public string Tag;
        public ScenariumFactionStateType State;
        public bool Defeated;
    }

    public class QuestRuntimeStateData
    {
        public string QuestId;
        public ScenariumQuestStateType State;
    }

    public class ObjectiveRuntimeStateData
    {
        public string ObjectiveId;
        public ScenariumObjectiveState State;
        public int CurrentCount;
    }

    public class ConquestNodeRuntimeStateData
    {
        public string NodeId;
        public ScenariumConquestNodeState State;
        public string OwningFactionTag;
        public bool Discovered;
    }
}
