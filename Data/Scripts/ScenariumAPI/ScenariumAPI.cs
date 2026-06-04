using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using System;
using System.IO;
using ScenariumAPI.Loading;
using ScenariumAPI.Validation;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;
using ScenariumAPI.Persistence;
using ScenariumAPI.Api;

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

        CampaignPackLoader _loader;
        CampaignValidator _validator;
        CampaignRuntime _runtime;
        ScenariumPersistence _runtimePersistence;
        ScenariumQueryApi _queryApi;
        ScenariumDataValidationResult _lastValidation;

        public override void LoadData()
        {
            _data = LoadState();
            if (_data == null)
                _data = ScenariumSaveData.CreateDefault();

            _data.EnsureCollections();

            _loader = new CampaignPackLoader();
            _validator = new CampaignValidator();
            _runtime = new CampaignRuntime(AddEvent);
            _runtimePersistence = new ScenariumPersistence();
            _queryApi = new ScenariumQueryApi(_runtime);

            _hud = new ScenariumHUD(_data, AddEvent, SaveState);
        }

        protected override void UnloadData()
        {
            SaveState();

            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;

            if (_runtime != null && _runtime.State != null && _runtimePersistence != null)
                _runtimePersistence.SaveRuntimeState(_runtime.State);

            if (_hud != null)
                _hud.CloseAndDispose();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_initialized && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null)
            {
                _initialized = true;
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

                AddEvent("ScenariumAPI campaign loader initialized.");
                _hud.Create();
                TryReloadCampaign();

                CampaignRuntimeStateData restoredState = _runtimePersistence.LoadRuntimeState();
                if (restoredState != null && _runtime != null && _runtime.Campaign != null)
                {
                    _runtime.RestoreState(restoredState);
                    AddEvent("Persisted runtime state restored.");
                }
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

            if (Eq(args[1], "scenario") || Eq(args[1], "campaign")) { SetTab("SCENARIO"); return; }
            if (Eq(args[1], "quest") || Eq(args[1], "quests")) { SetTab("QUESTS"); return; }
            if (Eq(args[1], "factions")) { SetTab("FACTIONS"); return; }
            if (Eq(args[1], "events") || Eq(args[1], "intel") || Eq(args[1], "log")) { SetTab("INTEL"); return; }

            if (Eq(args[1], "reload"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddEvent("Reload command received.");
                bool loaded = TryReloadCampaign();
                AddEvent(loaded ? "Reload complete." : "Reload failed. Check Intel Log.");

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "validate"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                ValidateCampaign();
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "runtime"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddEvent(_runtime != null ? _runtime.GetRuntimeSummary() : "No runtime available.");
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "query") && args.Length >= 3)
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                RunQueryCommand(args);
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "nodes"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                ListNodes();
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "destroy") && args.Length >= 3)
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                DestroyNode(args[2]);
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "capture") && args.Length >= 3)
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                CaptureNode(args[2]);
                SaveState();
                _hud.Refresh(true);
                return;
            }

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

        bool TryReloadCampaign()
        {
            CampaignData campaign;
            if (!_loader.TryLoad(out campaign))
            {
                AddEvent("Campaign reload failed: " + _loader.LastError);
                return false;
            }

            _lastValidation = _validator.Validate(campaign);

            if (!_lastValidation.IsValid)
            {
                AddEvent("Campaign validation failed. Errors: " + _lastValidation.Errors.Count);
                foreach (string error in _lastValidation.Errors)
                    AddEvent("ERROR: " + error);
                return false;
            }

            foreach (string warning in _lastValidation.Warnings)
                AddEvent("WARN: " + warning);

            _runtime.LoadCampaign(campaign);

            CampaignRuntimeStateData restoredState = _runtimePersistence.LoadRuntimeState();
            if (restoredState != null && string.Equals(restoredState.CampaignId, campaign.CampaignId, StringComparison.OrdinalIgnoreCase))
            {
                _runtime.RestoreState(restoredState);
                AddEvent("Existing runtime state restored for: " + campaign.DisplayName);
            }

            _queryApi.SetRuntime(_runtime);
            AddEvent("Campaign reloaded: " + campaign.DisplayName);
            AddEvent("Scenarios: " + campaign.Scenarios.Count + " | Factions: " + campaign.Factions.Count + " | Nodes: " + campaign.ConquestNodes.Count);
            if (_lastValidation != null)
                AddEvent("Validation warnings: " + _lastValidation.Warnings.Count);
            return true;
        }

        void ValidateCampaign()
        {
            if (_loader.LoadedCampaign == null)
            {
                AddEvent("No campaign loaded. Run /scen reload.");
                return;
            }

            _lastValidation = _validator.Validate(_loader.LoadedCampaign);

            AddEvent("Validation: " + (_lastValidation.IsValid ? "VALID" : "INVALID") +
                " | Errors: " + _lastValidation.Errors.Count +
                " | Warnings: " + _lastValidation.Warnings.Count);

            foreach (string error in _lastValidation.Errors)
                AddEvent("ERROR: " + error);

            foreach (string warning in _lastValidation.Warnings)
                AddEvent("WARN: " + warning);
        }

        void ListNodes()
        {
            if (_runtime == null || _runtime.Campaign == null || _runtime.State == null)
            {
                AddEvent("No runtime campaign loaded. Run /scen reload.");
                return;
            }

            AddEvent("Conquest nodes:");

            foreach (var state in _runtime.State.ConquestNodes)
            {
                var def = _runtime.GetNodeDefinition(state.NodeId);
                string name = def != null ? def.DisplayName : state.NodeId;
                AddEvent(state.NodeId + " | " + name + " | " + state.State);
            }
        }

        void DestroyNode(string nodeId)
        {
            if (_runtime == null || _runtime.Campaign == null)
            {
                AddEvent("No runtime campaign loaded. Run /scen reload.");
                return;
            }

            _runtime.DestroyNode(nodeId);
        }

        void CaptureNode(string nodeId)
        {
            if (_runtime == null || _runtime.Campaign == null)
            {
                AddEvent("No runtime campaign loaded. Run /scen reload.");
                return;
            }

            _runtime.CaptureNode(nodeId);
        }


        void RunQueryCommand(string[] args)
        {
            if (_queryApi == null)
            {
                AddEvent("Query API is not initialized.");
                return;
            }

            if (Eq(args[2], "campaign"))
            {
                AddEvent("Campaign loaded: " + _queryApi.IsCampaignLoaded());
                AddEvent("CampaignId: " + (_queryApi.GetCampaignId() ?? "none"));
                return;
            }

            if (Eq(args[2], "faction") && args.Length >= 4)
            {
                AddEvent("Faction " + args[3] + " state: " + _queryApi.GetFactionState(args[3]));
                AddEvent("Faction " + args[3] + " defeated: " + _queryApi.IsFactionDefeated(args[3]));
                return;
            }

            if (Eq(args[2], "node") && args.Length >= 4)
            {
                AddEvent("Node " + args[3] + " state: " + _queryApi.GetNodeState(args[3]));
                return;
            }

            if (Eq(args[2], "spawn") && args.Length >= 5)
            {
                AddEvent("Can faction " + args[3] + " spawn in " + args[4] + ": " + _queryApi.CanFactionSpawn(args[3], args[4]));
                return;
            }

            AddEvent("Query usage: /scen query campaign | faction <tag> | node <id> | spawn <tag> <sector>");
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

            if (_runtime != null && _runtime.State != null && _runtimePersistence != null)
                _runtimePersistence.SaveRuntimeState(_runtime.State);

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

                if (_runtimePersistence != null && _runtime != null && _runtime.State != null)
                    _runtimePersistence.SaveRuntimeState(_runtime.State);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("ScenariumAPI save failed: " + e);
            }
        }
    }
}
