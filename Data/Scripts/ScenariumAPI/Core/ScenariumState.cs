using System;
using System.Collections.Generic;

namespace ScenariumAPI
{
    public class ScenariumSaveData
    {
        public string CampaignId;
        public string CurrentSector;
        public string CampaignStage;
        public bool PanelVisible;
        public string PanelTab;
        public string SelectedItemId;
        public List<ScenariumQuestState> Quests = new List<ScenariumQuestState>();
        public List<ScenariumFactionState> Factions = new List<ScenariumFactionState>();
        public List<ScenariumEventState> Events = new List<ScenariumEventState>();

        public void EnsureCollections()
        {
            if (Quests == null) Quests = new List<ScenariumQuestState>();
            if (Factions == null) Factions = new List<ScenariumFactionState>();
            if (Events == null) Events = new List<ScenariumEventState>();
        }

        public void ApplyDefaultsIfMissing()
        {
            if (string.IsNullOrWhiteSpace(CampaignId)) CampaignId = "SolarWar";
            if (string.IsNullOrWhiteSpace(CurrentSector)) CurrentSector = "Earth";
            if (string.IsNullOrWhiteSpace(CampaignStage)) CampaignStage = "Setup / API Test";
            if (string.IsNullOrWhiteSpace(PanelTab)) PanelTab = "SCENARIO";
            if (Quests.Count == 0 || Factions.Count == 0)
            {
                ScenariumSaveData d = CreateDefault();
                if (Quests.Count == 0) Quests = d.Quests;
                if (Factions.Count == 0) Factions = d.Factions;
            }
        }

        public static ScenariumSaveData CreateDefault()
        {
            ScenariumSaveData d = new ScenariumSaveData();

            d.CampaignId = "SolarWar";
            d.CurrentSector = "Earth";
            d.CampaignStage = "Setup / API Test";
            d.PanelVisible = true;
            d.PanelTab = "SCENARIO";
            d.SelectedItemId = "OVERVIEW";

            d.Factions.Add(new ScenariumFactionState { Tag = "UTD", State = "Peacetime", Defeated = false });

            d.Quests.Add(new ScenariumQuestState { Id = "UTD_OUTPOST", Title = "Locate and Neutralize UTD Military Outpost", Description = "Prototype conquest objective. Completing this reveals the regional base.", Revealed = true, Active = true, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "UTD_REGIONAL_BASE", Title = "Destroy UTD Regional Military Base", Description = "Prototype objective revealed after the outpost is completed.", Revealed = false, Active = false, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "UTD_HQ", Title = "Destroy UTD Clan HQ", Description = "Prototype final faction-defeat objective.", Revealed = false, Active = false, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "GATE_ALPHA_COMPONENT", Title = "Recover Jump Gate Alpha Component", Description = "Prototype progression reward after faction defeat.", Revealed = false, Active = false, Completed = false });

            d.Events.Add(new ScenariumEventState { Tick = 0, Message = "SolarWar campaign state initialized." });

            return d;
        }
    }

    public class ScenariumQuestState
    {
        public string Id;
        public string Title;
        public string Description;
        public bool Revealed;
        public bool Active;
        public bool Completed;
    }

    public class ScenariumFactionState
    {
        public string Tag;
        public string State;
        public bool Defeated;
    }

    public class ScenariumEventState
    {
        public int Tick;
        public string Message;
    }
}
