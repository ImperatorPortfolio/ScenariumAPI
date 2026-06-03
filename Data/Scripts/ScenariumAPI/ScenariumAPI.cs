using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRage.Input;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Text;
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
        ScenariumPanelController _panel;

        public override void LoadData()
        {
            _data = LoadState();
            if (_data == null)
                _data = ScenariumSaveData.CreateDefault();

            _data.EnsureCollections();
            _panel = new ScenariumPanelController(_data);
        }

        protected override void UnloadData()
        {
            SaveState();

            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;

            if (_panel != null)
                _panel.Close();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_initialized && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null)
            {
                _initialized = true;
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
                AddEvent("ScenariumAPI v0.3.0 loaded. Press Shift+Q or type /scen menu.");
                _panel.Refresh(true);
            }

            _tick++;

            HandleKeyboardInput();

            if (_panel != null && _tick % 30 == 0)
                _panel.Refresh(false);

            if (_tick % 3600 == 0)
                SaveState();
        }

        void HandleKeyboardInput()
        {
            if (MyAPIGateway.Input == null) return;

            bool shiftHeld = MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) || MyAPIGateway.Input.IsKeyPress(MyKeys.RightShift);
            bool qPressed = MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Q);

            if (shiftHeld && qPressed)
            {
                TogglePanel();
            }
        }

        void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText)) return;
            if (!messageText.StartsWith("/scen", StringComparison.OrdinalIgnoreCase)) return;

            sendToOthers = false;

            string[] args = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length == 1 || Eq(args[1], "help")) { ShowHelp(); return; }
            if (Eq(args[1], "menu") || Eq(args[1], "panel") || Eq(args[1], "quest")) { TogglePanel(); return; }
            if (Eq(args[1], "tracker")) { ToggleTracker(); return; }
            if (Eq(args[1], "factions")) { ToggleFactionPanel(); return; }
            if (Eq(args[1], "events")) { ToggleEventsPanel(); return; }
            if (Eq(args[1], "status")) { ShowStatus(); return; }
            if (Eq(args[1], "complete") && args.Length >= 3) { CompleteQuest(args[2]); return; }
            if (Eq(args[1], "war") && args.Length >= 3) { SetFactionWarState(args[2]); return; }
            if (Eq(args[1], "save")) { SaveState(); AddEvent("Campaign state saved."); return; }
            if (Eq(args[1], "reset")) { ResetState(); return; }
            if (Eq(args[1], "debug")) { ShowDebug(); return; }

            AddEvent("Unknown command. Type /scen help.");
        }

        bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        void ShowHelp()
        {
            _data.PanelVisible = true;
            _data.PanelTab = "HELP";
            AddEvent("Help panel opened.");
            _panel.Refresh(true);
            SaveState();
        }

        void TogglePanel()
        {
            _data.PanelVisible = !_data.PanelVisible;
            if (_data.PanelVisible && string.IsNullOrWhiteSpace(_data.PanelTab))
                _data.PanelTab = "QUESTS";

            AddEvent("Scenarium panel " + (_data.PanelVisible ? "opened." : "closed."));
            _panel.Refresh(true);
            SaveState();
        }

        void ToggleTracker()
        {
            _data.TrackerVisible = !_data.TrackerVisible;
            AddEvent("Objective tracker " + (_data.TrackerVisible ? "enabled." : "disabled.") );
            _panel.Refresh(true);
            SaveState();
        }

        void ToggleFactionPanel()
        {
            _data.PanelVisible = true;
            _data.PanelTab = "FACTIONS";
            AddEvent("Faction panel opened.");
            _panel.Refresh(true);
            SaveState();
        }

        void ToggleEventsPanel()
        {
            _data.PanelVisible = true;
            _data.PanelTab = "EVENTS";
            AddEvent("Event log opened.");
            _panel.Refresh(true);
            SaveState();
        }

        void ShowStatus()
        {
            AddEvent("Campaign: " + _data.CampaignId + " | Sector: " + _data.CurrentSector + " | Stage: " + _data.CampaignStage);
            foreach (ScenariumFactionState f in _data.Factions)
                AddEvent("Faction " + f.Tag + ": " + f.State + (f.Defeated ? " / DEFEATED" : ""));

            _panel.Refresh(true);
        }

        void CompleteQuest(string id)
        {
            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (Eq(q.Id, id))
                {
                    q.Completed = true;
                    q.Revealed = true;
                    q.Active = false;
                    AddEvent("Quest completed: " + q.Title);
                    ApplyDemoQuestChain(id);
                    SaveState();
                    _panel.Refresh(true);
                    return;
                }
            }

            AddEvent("Quest not found: " + id);
            _panel.Refresh(true);
        }

        void ApplyDemoQuestChain(string id)
        {
            if (Eq(id, "UTD_OUTPOST"))
                RevealQuest("UTD_REGIONAL_BASE");

            if (Eq(id, "UTD_REGIONAL_BASE"))
                RevealQuest("UTD_HQ");

            if (Eq(id, "UTD_HQ"))
            {
                foreach (ScenariumFactionState f in _data.Factions)
                {
                    if (Eq(f.Tag, "UTD"))
                    {
                        f.State = "Defeated";
                        f.Defeated = true;
                    }
                }

                RevealQuest("GATE_ALPHA_COMPONENT");
                AddEvent("UTD conquest chain completed. Faction marked defeated.");
            }
        }

        void RevealQuest(string id)
        {
            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (Eq(q.Id, id))
                {
                    q.Revealed = true;
                    q.Active = true;
                    AddEvent("New objective revealed: " + q.Title);
                    return;
                }
            }
        }

        void SetFactionWarState(string tag)
        {
            foreach (ScenariumFactionState f in _data.Factions)
            {
                if (Eq(f.Tag, tag))
                {
                    f.State = "War";
                    AddEvent(tag.ToUpperInvariant() + " state changed to WAR.");
                    SaveState();
                    _panel.Refresh(true);
                    return;
                }
            }

            _data.Factions.Add(new ScenariumFactionState { Tag = tag.ToUpperInvariant(), State = "War", Defeated = false });
            AddEvent(tag.ToUpperInvariant() + " added and set to WAR.");
            SaveState();
            _panel.Refresh(true);
        }

        void ResetState()
        {
            _data = ScenariumSaveData.CreateDefault();
            _panel = new ScenariumPanelController(_data);
            AddEvent("Scenarium state reset.");
            SaveState();
            _panel.Refresh(true);
        }

        void AddEvent(string message)
        {
            if (_data == null) return;

            _data.EnsureCollections();
            _data.Events.Add(new ScenariumEventState { Tick = _tick, Message = message });

            while (_data.Events.Count > 16)
                _data.Events.RemoveAt(0);
        }

        void ShowDebug()
        {
            bool exists = false;

            try
            {
                exists = MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFile, typeof(ScenariumSession));
            }
            catch {}

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ScenariumAPI Debug");
            sb.AppendLine("Version: 0.3.0 Shift+Q Panel");
            sb.AppendLine("SaveFile: " + SaveFile);
            sb.AppendLine("SaveExists: " + exists);
            sb.AppendLine("PanelVisible: " + _data.PanelVisible);
            sb.AppendLine("PanelTab: " + _data.PanelTab);
            sb.AppendLine("TrackerVisible: " + _data.TrackerVisible);
            sb.AppendLine("QuestCount: " + _data.Quests.Count);
            sb.AppendLine("FactionCount: " + _data.Factions.Count);
            sb.AppendLine();
            sb.AppendLine("RichHudFramework:");
            sb.AppendLine("This package contains the Scenarium panel/controller and keyboard shortcut.");
            sb.AppendLine("The RichHudFramework.Client adapter is intentionally isolated so bad API binding cannot break first-load testing.");
            Dialog("ScenariumAPI Debug", sb.ToString());
        }

        ScenariumSaveData LoadState()
        {
            try
            {
                if (MyAPIGateway.Utilities == null) return null;
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFile, typeof(ScenariumSession))) return null;

                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SaveFile, typeof(ScenariumSession));
                string xml = reader.ReadToEnd();
                reader.Close();

                if (string.IsNullOrWhiteSpace(xml)) return null;

                ScenariumSaveData data = MyAPIGateway.Utilities.SerializeFromXML<ScenariumSaveData>(xml);
                if (data == null) return null;

                data.EnsureCollections();
                data.ApplyDefaultsIfMissing();

                return data;
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
                if (MyAPIGateway.Utilities == null || _data == null) return;

                _data.EnsureCollections();
                string xml = MyAPIGateway.Utilities.SerializeToXML(_data);

                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(SaveFile, typeof(ScenariumSession));
                writer.Write(xml);
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("ScenariumAPI save failed: " + e);
            }
        }

        void Dialog(string title, string body)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen(title, "", "", body, null, "Close");
        }
    }

    public class ScenariumPanelController
    {
        readonly ScenariumSaveData _data;
        int _lastHash;

        public ScenariumPanelController(ScenariumSaveData data)
        {
            _data = data;
        }

        public void Refresh(bool force)
        {
            if (MyAPIGateway.Utilities == null || _data == null) return;

            string output = BuildOutput();
            int hash = output.GetHashCode();

            if (!force && hash == _lastHash) return;

            _lastHash = hash;

            // RichHudFramework.Client adapter insertion point:
            // Replace WriteFallbackPanel(output) with real RichHud panel update once the client module is embedded.
            WriteFallbackPanel(output);
        }

        public void Close()
        {
        }

        string BuildOutput()
        {
            StringBuilder sb = new StringBuilder();

            if (_data.TrackerVisible)
                BuildTracker(sb);

            if (_data.PanelVisible)
                BuildPanel(sb);

            return sb.ToString();
        }

        void BuildTracker(StringBuilder sb)
        {
            sb.AppendLine("SCENARIUM // SOLARFRONTIER");
            sb.AppendLine("Sector: " + _data.CurrentSector);
            ScenariumQuestState active = GetActiveQuest();

            if (active != null)
                sb.AppendLine("Objective: " + active.Title);
            else
                sb.AppendLine("Objective: None");

            sb.AppendLine("UTD: " + GetFactionState("UTD"));
        }

        void BuildPanel(StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("==================================================");
            sb.AppendLine("SCENARIUM COMMAND PANEL    [Shift+Q]");
            sb.AppendLine("Campaign: " + _data.CampaignId);
            sb.AppendLine("Sector: " + _data.CurrentSector + " | Stage: " + _data.CampaignStage);
            sb.AppendLine("Tab: " + _data.PanelTab);
            sb.AppendLine("==================================================");

            if (_data.PanelTab == "HELP")
                BuildHelp(sb);
            else if (_data.PanelTab == "FACTIONS")
                BuildFactionPanel(sb);
            else if (_data.PanelTab == "EVENTS")
                BuildEvents(sb, 12);
            else
                BuildQuestPanel(sb);

            sb.AppendLine("==================================================");
            sb.AppendLine("Commands: /scen menu | /scen factions | /scen events | /scen tracker");
        }

        void BuildHelp(StringBuilder sb)
        {
            sb.AppendLine("Keyboard:");
            sb.AppendLine("Shift+Q - open/close Scenarium panel");
            sb.AppendLine("");
            sb.AppendLine("Commands:");
            sb.AppendLine("/scen menu - open/close panel");
            sb.AppendLine("/scen tracker - show/hide compact tracker");
            sb.AppendLine("/scen factions - open conquest/faction tab");
            sb.AppendLine("/scen events - open event log tab");
            sb.AppendLine("/scen complete <questId> - DEV complete objective");
            sb.AppendLine("/scen war <factionTag> - DEV set faction war state");
            sb.AppendLine("/scen debug - diagnostic screen");
        }

        void BuildQuestPanel(StringBuilder sb)
        {
            sb.AppendLine("ACTIVE OBJECTIVES");
            bool anyActive = false;

            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (q.Revealed && !q.Completed)
                {
                    anyActive = true;
                    sb.AppendLine("[ ] " + q.Id + " - " + q.Title);
                    sb.AppendLine("    " + q.Description);
                }
            }

            if (!anyActive)
                sb.AppendLine("No active objectives.");

            sb.AppendLine("");
            sb.AppendLine("COMPLETED OBJECTIVES");

            bool anyCompleted = false;

            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (q.Completed)
                {
                    anyCompleted = true;
                    sb.AppendLine("[X] " + q.Id + " - " + q.Title);
                }
            }

            if (!anyCompleted)
                sb.AppendLine("No completed objectives.");

            sb.AppendLine("");
            sb.AppendLine("LOCKED OBJECTIVES");

            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (!q.Revealed && !q.Completed)
                    sb.AppendLine("[?] " + q.Id);
            }

            sb.AppendLine("");
            BuildEvents(sb, 5);
        }

        void BuildFactionPanel(StringBuilder sb)
        {
            sb.AppendLine("FACTION / CONQUEST STATUS");

            foreach (ScenariumFactionState f in _data.Factions)
            {
                sb.AppendLine("");
                sb.AppendLine(f.Tag + " - " + f.State + (f.Defeated ? " - DEFEATED" : ""));

                if (f.Tag == "UTD")
                {
                    sb.AppendLine("Conquest Chain:");
                    sb.AppendLine(GetQuestMark("UTD_OUTPOST") + " Military Outpost");
                    sb.AppendLine(GetQuestMark("UTD_REGIONAL_BASE") + " Regional Military Base");
                    sb.AppendLine(GetQuestMark("UTD_HQ") + " Clan HQ");
                    sb.AppendLine(GetQuestMark("GATE_ALPHA_COMPONENT") + " Jump Gate Component Reward");
                }
            }
        }

        void BuildEvents(StringBuilder sb, int max)
        {
            sb.AppendLine("RECENT EVENTS");

            int start = Math.Max(0, _data.Events.Count - max);

            for (int i = start; i < _data.Events.Count; i++)
                sb.AppendLine("> " + _data.Events[i].Message);
        }

        ScenariumQuestState GetActiveQuest()
        {
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed && q.Active)
                    return q;

            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    return q;

            return null;
        }

        string GetFactionState(string tag)
        {
            foreach (ScenariumFactionState f in _data.Factions)
            {
                if (string.Equals(f.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return f.State + (f.Defeated ? " / DEFEATED" : "");
            }

            return "Unknown";
        }

        string GetQuestMark(string id)
        {
            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    if (q.Completed) return "[X]";
                    if (q.Revealed) return "[ ]";
                    return "[?]";
                }
            }

            return "[?]";
        }

        void WriteFallbackPanel(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return;

            // This is a safe first-load panel fallback. It avoids green notifications and mission popups.
            // It is deliberately event-driven and only writes when state changes.
            string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.None);
            int max = Math.Min(lines.Length, _data.PanelVisible ? 28 : 6);

            MyAPIGateway.Utilities.ShowMessage("Scenarium", "----------------------------------------");

            for (int i = 0; i < max; i++)
            {
                string line = lines[i].TrimEnd();
                if (!string.IsNullOrWhiteSpace(line))
                    MyAPIGateway.Utilities.ShowMessage("Scenarium", line);
            }
        }
    }

    public class ScenariumSaveData
    {
        public string CampaignId;
        public string CurrentSector;
        public string CampaignStage;

        public bool TrackerVisible;
        public bool PanelVisible;
        public string PanelTab;

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
            if (string.IsNullOrWhiteSpace(PanelTab)) PanelTab = "QUESTS";

            if (Quests.Count == 0 || Factions.Count == 0)
            {
                ScenariumSaveData defaults = CreateDefault();

                if (Quests.Count == 0)
                    Quests = defaults.Quests;

                if (Factions.Count == 0)
                    Factions = defaults.Factions;
            }
        }

        public static ScenariumSaveData CreateDefault()
        {
            ScenariumSaveData d = new ScenariumSaveData();

            d.CampaignId = "SolarWar";
            d.CurrentSector = "Earth";
            d.CampaignStage = "Setup / API Test";
            d.TrackerVisible = true;
            d.PanelVisible = false;
            d.PanelTab = "QUESTS";

            d.Factions.Add(new ScenariumFactionState { Tag = "UTD", State = "Peacetime", Defeated = false });

            d.Quests.Add(new ScenariumQuestState
            {
                Id = "UTD_OUTPOST",
                Title = "Locate and Neutralize UTD Military Outpost",
                Description = "Prototype conquest objective. Completing this reveals the regional base.",
                Revealed = true,
                Active = true,
                Completed = false
            });

            d.Quests.Add(new ScenariumQuestState
            {
                Id = "UTD_REGIONAL_BASE",
                Title = "Destroy UTD Regional Military Base",
                Description = "Prototype objective revealed after the outpost is completed.",
                Revealed = false,
                Active = false,
                Completed = false
            });

            d.Quests.Add(new ScenariumQuestState
            {
                Id = "UTD_HQ",
                Title = "Destroy UTD Clan HQ",
                Description = "Prototype final faction-defeat objective.",
                Revealed = false,
                Active = false,
                Completed = false
            });

            d.Quests.Add(new ScenariumQuestState
            {
                Id = "GATE_ALPHA_COMPONENT",
                Title = "Recover Jump Gate Alpha Component",
                Description = "Prototype progression reward after faction defeat.",
                Revealed = false,
                Active = false,
                Completed = false
            });

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
