using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace ScenariumAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ScenariumSession : MySessionComponentBase
    {
        const string SaveFile = "ScenariumAPI_State.xml";
        bool _initialized;
        int _tick;
        ScenariumSaveData _data;

        public override void LoadData()
        {
            _data = LoadState();
            if (_data == null)
                _data = ScenariumSaveData.CreateDefault();
        }

        protected override void UnloadData()
        {
            SaveState();
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_initialized && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null)
            {
                _initialized = true;
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
                Notify("ScenariumAPI loaded. Type /scen help for commands.");
            }

            _tick++;
            if (_tick % 3600 == 0)
                SaveState();
        }

        void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText)) return;
            if (!messageText.StartsWith("/scen", StringComparison.OrdinalIgnoreCase)) return;

            sendToOthers = false;
            var args = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 1 || Eq(args[1], "help")) { ShowHelp(); return; }
            if (Eq(args[1], "status")) { ShowStatus(); return; }
            if (Eq(args[1], "quest")) { ShowQuestMenu(); return; }
            if (Eq(args[1], "menu")) { ShowQuestMenu(); return; }
            if (Eq(args[1], "complete") && args.Length >= 3) { CompleteQuest(args[2]); return; }
            if (Eq(args[1], "reset")) { _data = ScenariumSaveData.CreateDefault(); SaveState(); Notify("ScenariumAPI state reset."); return; }
            if (Eq(args[1], "save")) { SaveState(); Notify("ScenariumAPI state saved."); return; }
            if (Eq(args[1], "war") && args.Length >= 3) { SetFactionWarState(args[2]); return; }

            Notify("Unknown command. Type /scen help.");
        }

        bool Eq(string a, string b) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }

        void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ScenariumAPI Commands");
            sb.AppendLine("/scen status - show campaign status");
            sb.AppendLine("/scen quest  - show quest menu scaffold");
            sb.AppendLine("/scen complete <questId> - mark quest complete");
            sb.AppendLine("/scen war <factionTag> - set faction to War state");
            sb.AppendLine("/scen save - save state");
            sb.AppendLine("/scen reset - reset demo state");
            Dialog("ScenariumAPI Help", sb.ToString());
        }

        void ShowStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Campaign: " + _data.CampaignId);
            sb.AppendLine("Sector: " + _data.CurrentSector);
            sb.AppendLine("Campaign Stage: " + _data.CampaignStage);
            sb.AppendLine();
            sb.AppendLine("Factions:");
            foreach (var f in _data.Factions)
                sb.AppendLine("- " + f.Tag + ": " + f.State + " | Defeated: " + f.Defeated);
            Dialog("ScenariumAPI Status", sb.ToString());
        }

        void ShowQuestMenu()
        {
            // This is intentionally a vanilla-safe quest menu for first-load testing.
            // Next pass can replace this with RichHudText/RichHudFramework UI calls once dependency wiring is confirmed.
            var sb = new StringBuilder();
            sb.AppendLine("SOLARWAR - QUEST MENU");
            sb.AppendLine("Sector: " + _data.CurrentSector);
            sb.AppendLine("Stage: " + _data.CampaignStage);
            sb.AppendLine();
            sb.AppendLine("Active Objectives:");
            foreach (var q in _data.Quests)
            {
                string mark = q.Completed ? "[X]" : (q.Revealed ? "[ ]" : "[?]");
                if (q.Revealed || q.Completed)
                    sb.AppendLine(mark + " " + q.Id + " - " + q.Title + " :: " + q.Description);
            }
            sb.AppendLine();
            sb.AppendLine("RichHudText integration target: replace this dialog with persistent quest panel.");
            Dialog("ScenariumAPI Quest Menu", sb.ToString());
        }

        void CompleteQuest(string id)
        {
            foreach (var q in _data.Quests)
            {
                if (Eq(q.Id, id))
                {
                    q.Completed = true;
                    q.Revealed = true;
                    Notify("Quest completed: " + q.Title);
                    ApplyDemoQuestChain(id);
                    SaveState();
                    return;
                }
            }
            Notify("Quest not found: " + id);
        }

        void ApplyDemoQuestChain(string id)
        {
            if (Eq(id, "UTD_OUTPOST")) RevealQuest("UTD_REGIONAL_BASE");
            if (Eq(id, "UTD_REGIONAL_BASE")) RevealQuest("UTD_HQ");
            if (Eq(id, "UTD_HQ"))
            {
                foreach (var f in _data.Factions)
                {
                    if (Eq(f.Tag, "UTD")) { f.State = "Defeated"; f.Defeated = true; }
                }
                RevealQuest("GATE_ALPHA_COMPONENT");
            }
        }

        void RevealQuest(string id)
        {
            foreach (var q in _data.Quests)
            {
                if (Eq(q.Id, id))
                {
                    q.Revealed = true;
                    Notify("New objective revealed: " + q.Title);
                    return;
                }
            }
        }

        void SetFactionWarState(string tag)
        {
            foreach (var f in _data.Factions)
            {
                if (Eq(f.Tag, tag))
                {
                    f.State = "War";
                    Notify(tag + " state set to War.");
                    SaveState();
                    return;
                }
            }
            _data.Factions.Add(new ScenariumFactionState { Tag = tag.ToUpperInvariant(), State = "War", Defeated = false });
            Notify(tag + " added and set to War.");
            SaveState();
        }

        ScenariumSaveData LoadState()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFile, typeof(ScenariumSession))) return null;
                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SaveFile, typeof(ScenariumSession));
                var xml = reader.ReadToEnd();
                reader.Close();
                var serializer = new XmlSerializer(typeof(ScenariumSaveData));
                using (var sr = new StringReader(xml)) return serializer.Deserialize(sr) as ScenariumSaveData;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("ScenariumAPI load failed: " + e);
                return null;
            }
        }

        void SaveState()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ScenariumSaveData));
                using (var sw = new StringWriter())
                {
                    serializer.Serialize(sw, _data);
                    TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(SaveFile, typeof(ScenariumSession));
                    writer.Write(sw.ToString());
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("ScenariumAPI save failed: " + e);
            }
        }

        void Notify(string text)
        {
            MyAPIGateway.Utilities.ShowNotification(text, 5000, MyFontEnum.Green);
        }

        void Dialog(string title, string body)
        {
            MyAPIGateway.Utilities.ShowMissionScreen(title, "", "", body, null, "Close");
        }
    }

    public class ScenariumSaveData
    {
        public string CampaignId;
        public string CurrentSector;
        public string CampaignStage;
        public List<ScenariumQuestState> Quests = new List<ScenariumQuestState>();
        public List<ScenariumFactionState> Factions = new List<ScenariumFactionState>();

        public static ScenariumSaveData CreateDefault()
        {
            var d = new ScenariumSaveData();
            d.CampaignId = "SolarWar";
            d.CurrentSector = "Earth";
            d.CampaignStage = "Setup / API Test";
            d.Factions.Add(new ScenariumFactionState { Tag = "UTD", State = "Peacetime", Defeated = false });
            d.Quests.Add(new ScenariumQuestState { Id = "UTD_OUTPOST", Title = "Locate and Neutralize UTD Military Outpost", Description = "Prototype conquest objective. Completing this reveals the regional base.", Revealed = true, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "UTD_REGIONAL_BASE", Title = "Destroy UTD Regional Military Base", Description = "Prototype objective revealed after the outpost is completed.", Revealed = false, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "UTD_HQ", Title = "Destroy UTD Clan HQ", Description = "Prototype final faction-defeat objective.", Revealed = false, Completed = false });
            d.Quests.Add(new ScenariumQuestState { Id = "GATE_ALPHA_COMPONENT", Title = "Recover Jump Gate Alpha Component", Description = "Prototype progression reward after faction defeat.", Revealed = false, Completed = false });
            return d;
        }
    }

    public class ScenariumQuestState
    {
        public string Id;
        public string Title;
        public string Description;
        public bool Revealed;
        public bool Completed;
    }

    public class ScenariumFactionState
    {
        public string Tag;
        public string State;
        public bool Defeated;
    }
}
