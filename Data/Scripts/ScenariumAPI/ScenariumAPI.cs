using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using System;
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
        ScenariumHUD _hud;

        public override void LoadData()
        {
            _data = LoadState();
            if (_data == null)
                _data = ScenariumSaveData.CreateDefault();

            _data.EnsureCollections();
            _hud = new ScenariumHUD(_data, AddEvent, SaveState);
        }

        protected override void UnloadData()
        {
            SaveState();

            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;

            if (_hud != null)
                _hud.CloseAndDispose();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_initialized && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null)
            {
                _initialized = true;
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

                AddEvent("ScenariumAPI interactive RichHud UI initialized.");
                _hud.Create();
            }

            _tick++;

            HandleKeyboardInput();

            if (_hud != null && _tick % 30 == 0)
                _hud.Refresh(false);

            if (_tick % 3600 == 0)
                SaveState();
        }

        void HandleKeyboardInput()
        {
            if (MyAPIGateway.Input == null)
                return;

            bool shift = MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) || MyAPIGateway.Input.IsKeyPress(MyKeys.RightShift);
            bool q = MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Q);

            if (shift && q)
                TogglePanel();
        }

        void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            if (!messageText.StartsWith("/scen", StringComparison.OrdinalIgnoreCase))
                return;

            sendToOthers = false;

            string[] args = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length == 1 || Eq(args[1], "menu") || Eq(args[1], "panel"))
            {
                TogglePanel();
                return;
            }

            if (Eq(args[1], "scenario")) { SetTab("SCENARIO"); return; }
            if (Eq(args[1], "quest") || Eq(args[1], "quests")) { SetTab("QUESTS"); return; }
            if (Eq(args[1], "factions")) { SetTab("FACTIONS"); return; }
            if (Eq(args[1], "events") || Eq(args[1], "intel") || Eq(args[1], "log")) { SetTab("INTEL"); return; }

            if (Eq(args[1], "complete") && args.Length >= 3)
            {
                CompleteQuest(args[2]);
                return;
            }

            if (Eq(args[1], "war") && args.Length >= 3)
            {
                SetFactionWarState(args[2]);
                return;
            }

            if (Eq(args[1], "save"))
            {
                SaveState();
                AddEvent("Campaign state saved.");
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "reset"))
            {
                ResetState();
                return;
            }

            AddEvent("Unknown command.");
            _hud.Refresh(true);
        }

        bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        void TogglePanel()
        {
            _data.PanelVisible = !_data.PanelVisible;

            if (string.IsNullOrWhiteSpace(_data.PanelTab))
                _data.PanelTab = "SCENARIO";

            if (_data.PanelVisible)
                _hud.Open();
            else
                _hud.Close();

            AddEvent("Scenarium panel " + (_data.PanelVisible ? "opened." : "closed."));
            _hud.Refresh(true);
            SaveState();
        }

        void SetTab(string tab)
        {
            _data.PanelVisible = true;
            _data.PanelTab = tab;
            _hud.Open();
            _hud.Refresh(true);
            SaveState();
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
                    _hud.Refresh(true);
                    return;
                }
            }

            AddEvent("Quest not found: " + id);
            _hud.Refresh(true);
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
                    AddEvent("New objective: " + q.Title);
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
                    _hud.Refresh(true);
                    return;
                }
            }

            _data.Factions.Add(new ScenariumFactionState { Tag = tag.ToUpperInvariant(), State = "War", Defeated = false });
            AddEvent(tag.ToUpperInvariant() + " added and set to WAR.");

            SaveState();
            _hud.Refresh(true);
        }

        void ResetState()
        {
            _data = ScenariumSaveData.CreateDefault();

            if (_hud != null)
                _hud.CloseAndDispose();

            _hud = new ScenariumHUD(_data, AddEvent, SaveState);
            _hud.Create();
            _hud.Open();

            AddEvent("Scenarium state reset.");

            SaveState();
            _hud.Refresh(true);
        }

        void AddEvent(string message)
        {
            if (_data == null)
                return;

            _data.EnsureCollections();

            _data.Events.Add(new ScenariumEventState { Tick = _tick, Message = message });

            while (_data.Events.Count > 16)
                _data.Events.RemoveAt(0);
        }

        ScenariumSaveData LoadState()
        {
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return null;

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFile, typeof(ScenariumSession)))
                    return null;

                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SaveFile, typeof(ScenariumSession));
                string xml = reader.ReadToEnd();
                reader.Close();

                if (string.IsNullOrWhiteSpace(xml))
                    return null;

                ScenariumSaveData data = MyAPIGateway.Utilities.SerializeFromXML<ScenariumSaveData>(xml);

                if (data == null)
                    return null;

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
                if (MyAPIGateway.Utilities == null || _data == null)
                    return;

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
    }
}
