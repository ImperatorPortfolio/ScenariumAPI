namespace ScenariumAPI.Data
{
    public enum ScenariumScenarioState
    {
        Hidden,
        Locked,
        Available,
        Active,
        Completed,
        Failed
    }

    public enum ScenariumFactionStateType
    {
        Unknown,
        Peacetime,
        Alert,
        War,
        Defeated,
        Disabled
    }

    public enum ScenariumQuestStateType
    {
        Hidden,
        Locked,
        Revealed,
        Active,
        Completed,
        Failed
    }

    public enum ScenariumObjectiveState
    {
        Hidden,
        Active,
        Completed,
        Failed
    }

    public enum ScenariumObjectiveType
    {
        None,
        DiscoverLocation,
        DestroyGrid,
        CaptureNode,
        DestroyNode,
        DefeatFaction,
        CollectItem,
        DeliverItem,
        BuildBlock,
        BuildGrid,
        SurviveWave,
        ReachSector,
        SetWorldFact
    }

    public enum ScenariumConquestNodeType
    {
        Unknown,
        CivilianSite,
        MiningSite,
        TradeStation,
        LogisticsHub,
        ListeningPost,
        MilitaryOutpost,
        RegionalBase,
        Headquarters,
        Shipyard,
        Relay,
        ResearchSite,
        JumpGate
    }

    public enum ScenariumConquestNodeState
    {
        Hidden,
        Revealed,
        Active,
        Contested,
        Captured,
        Destroyed,
        Disabled
    }

    public enum ScenariumRewardType
    {
        None,
        RevealQuest,
        RevealNode,
        SetWorldFact,
        AddGPS,
        UnlockBlueprint,
        GrantItem,
        ChangeFactionState,
        CompleteScenario,
        UnlockScenario
    }

    public enum ScenariumIntegrationType
    {
        None,
        MES,
        AIEnabled,
        RichHud,
        Torch,
        GPS,
        Custom
    }

    public enum ScenariumCampaignState
    {
        NotLoaded,
        Loaded,
        Active,
        Completed,
        Failed,
        Disabled
    }

    public enum ScenariumLocationType
    {
        Unknown,
        Planet,
        Moon,
        Asteroid,
        DeepSpace,
        Sector,
        Coordinate
    }
}
