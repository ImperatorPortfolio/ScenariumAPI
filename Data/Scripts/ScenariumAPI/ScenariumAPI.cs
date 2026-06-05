using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using VRage.Game.ModAPI;
using System;
using System.IO;
using System.Collections.Generic;
using ScenariumAPI.Loading;
using ScenariumAPI.Validation;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;
using ScenariumAPI.Persistence;
using ScenariumAPI.Api;
using ScenariumAPI.Integrations.MES;
using ScenariumAPI.Events;
using ScenariumAPI.Diagnostics;
using ScenariumAPI.Progression;
using ScenariumAPI.Binding;
using ScenariumAPI.UI;
using ScenariumAPI.Objectives;

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
        NodeDetectionRuntime _nodeDetection;
        MesBindingBridge _mesBridge;
        MesPermissionExporter _mesExporter;
        MesSpawnRequestRuntime _mesSpawnRequests;
        MesSpawnCommandBridge _mesSpawnBridge;
        MesApiClient _mesApi;
        ScenariumDataValidationResult _lastValidation;
        ScenariumEventBus _eventBus;
        ScenariumDiagnostics _diagnostics;
        ConquestConsequenceRuntime _consequences;
        CampaignBindingValidator _bindingValidator;
        NodeTransitionValidator _transitionValidator;
        TransitionAuditLog _transitionAudit;
        bool _ignorePersistedRuntimeStateOnce;
        ScenariumEntityBindingRuntime _entityBinding;
        ObjectiveRuntime _objectives;
        bool _mesSpawnCallbackRegistered;
        int _autoSpawnCooldownTicks;
        const int AutoSpawnRetryCooldownTicks = 10800;
        string _autoSpawnPendingNodeId;
        string _autoSpawnPendingSpawnGroup;
        int _autoSpawnPendingTicks;
        bool _autoSpawnSessionLock;
        int _autoSpawnSessionLockTicks;
        const int AutoSpawnSessionLockTimeoutTicks = 18000;
        const int AutoSpawnPendingTimeoutTicks = 18000;

        public override void LoadData()
        {
            _data = LoadState();
            if (_data == null)
                _data = ScenariumSaveData.CreateDefault();

            _data.EnsureCollections();

            _loader = new CampaignPackLoader();
            _validator = new CampaignValidator();
            _eventBus = new ScenariumEventBus(AddEvent);
            _diagnostics = new ScenariumDiagnostics();
            _runtime = new CampaignRuntime(AddEvent);
            _runtimePersistence = new ScenariumPersistence();
            _queryApi = new ScenariumQueryApi(_runtime);
            _nodeDetection = new NodeDetectionRuntime(_runtime, AddEvent);
            _entityBinding = new ScenariumEntityBindingRuntime(_runtime, AddEvent, RunValidatedNodeTransition);
            _objectives = new ObjectiveRuntime(RunValidatedNodeTransition, AddEvent);
            _mesBridge = new MesBindingBridge(_runtime, AddEvent);
            _mesExporter = new MesPermissionExporter(AddEvent);
            _mesSpawnRequests = new MesSpawnRequestRuntime(_runtime, _mesBridge, AddEvent);
            _mesApi = new MesApiClient(AddEvent);
            _mesSpawnBridge = new MesSpawnCommandBridge(_mesSpawnRequests, _mesApi, AddEvent);
            RegisterMesSpawnCallback();
            _consequences = new ConquestConsequenceRuntime(_runtime, _eventBus);
            _bindingValidator = new CampaignBindingValidator();
            _transitionValidator = new NodeTransitionValidator(_runtime);
            _transitionAudit = new TransitionAuditLog();
            _transitionValidator = new NodeTransitionValidator(_runtime);
            _transitionAudit = new TransitionAuditLog();

            _hud = new ScenariumHUD(_data, AddEvent, SaveState);
        }

        protected override void UnloadData()
        {
            SaveState();

            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;

            if (_runtime != null && _runtime.State != null && _runtimePersistence != null)
                _runtimePersistence.SaveRuntimeState(_runtime.State);

            if (_mesApi != null)
                _mesApi.Close();

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

                RefreshObjectiveBindingsFromCampaign();
                TryAutoSpawnNextObjective("campaign load");
            }

            _tick++;

            HandleKeyboardInput();

            if (_autoSpawnCooldownTicks > 0)
                _autoSpawnCooldownTicks--;

            if (_autoSpawnSessionLock)
            {
                _autoSpawnSessionLockTicks++;

                if (_autoSpawnSessionLockTicks > AutoSpawnSessionLockTimeoutTicks)
                {
                    AddEvent("Auto objective spawn session lock timed out. Spawn retry cooldown started.");
                    _autoSpawnSessionLock = false;
                    _autoSpawnSessionLockTicks = 0;
                    ClearAutoSpawnPending();
                    _autoSpawnCooldownTicks = AutoSpawnRetryCooldownTicks;
                }
            }

            if (!string.IsNullOrWhiteSpace(_autoSpawnPendingNodeId))
            {
                _autoSpawnPendingTicks++;

                if (_autoSpawnPendingTicks > AutoSpawnPendingTimeoutTicks)
                {
                    AddEvent("Auto objective spawn pending timed out for " + _autoSpawnPendingNodeId + ". Retry cooldown started.");
                    _autoSpawnPendingNodeId = null;
                    _autoSpawnPendingSpawnGroup = null;
                    _autoSpawnPendingTicks = 0;
                    _autoSpawnCooldownTicks = AutoSpawnRetryCooldownTicks;
                }
            }

            if (_mesApi != null && !_mesApi.Ready && _tick % 120 == 0)
                _mesApi.UpdateHandshake();

            if (_mesApi != null && _mesApi.Ready && !_mesSpawnCallbackRegistered)
                RegisterMesSpawnCallback();

            if (_entityBinding != null && _tick % 120 == 0)
            {
                _entityBinding.Update();
                UpdateHudViewModel();
            }

            if (_objectives != null && _tick % 120 == 0)
                _objectives.Update();

            if (_nodeDetection != null && _tick % 120 == 0)
            {
                _nodeDetection.Update();
                UpdateHudViewModel();
            }

            if (_mesApi != null && _mesApi.Ready && _tick % 7200 == 0)
                TryAutoSpawnNextObjective("periodic update");

            if (_hud != null && _tick % 30 == 0)
                _hud.Refresh(false);

            if (_tick % 7200 == 0)
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

            if (Eq(args[1], "version"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddEvent("ScenariumAPI version: 0.7.3c");
                AddEvent("ScenariumHUD version: " + ScenariumHudService.HudVersion);
                AddEvent("Module boundary: Core/UI split boundary active");

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "bind"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                HandlePersistentBindCommand(args);

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "audit"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddMultilineEvent(_transitionAudit != null ? _transitionAudit.BuildSummary(20) : "Transition audit log is not initialized.");
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "events"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                if (args.Length >= 3 && Eq(args[2], "clear"))
                {
                    if (_eventBus != null)
                    {
                        _eventBus.Clear();
                        _eventBus.Publish(ScenariumEventType.EventsCleared, "Events", "Event log cleared.", "", "Cleared");
                    }

                    AddEvent("Scenarium event log cleared.");
                }
                else
                {
                    AddMultilineEvent(_eventBus != null ? _eventBus.BuildRecentSummary(20) : "Event bus is not initialized.");
                }

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "facts"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddMultilineEvent(_diagnostics != null ? _diagnostics.BuildFactsReport(_runtime) : "Diagnostics are not initialized.");
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "diagnose"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddMultilineEvent(_diagnostics != null ? _diagnostics.BuildRuntimeReport(_runtime, _mesBridge, _eventBus, _transitionAudit) : "Diagnostics are not initialized.");
                AddEvent("API Version: 0.7.3c");
                AddEvent("HUD Version: " + ScenariumHudService.HudVersion);
                AddEvent("Module Boundary: Core/UI split boundary active");
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "bindings") && args.Length >= 3 && Eq(args[2], "validate"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                AddMultilineEvent(_bindingValidator != null ? _bindingValidator.ValidateMesBindings(_runtime) : "Binding validator is not initialized.");
                if (_eventBus != null)
                    _eventBus.Publish(ScenariumEventType.ValidationCompleted, "MESBindings", "MES binding validation completed.", "", "Complete");
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "transition") && args.Length >= 5 && Eq(args[2], "node"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                bool force = args.Length >= 6 && Eq(args[5], "force");
                RunValidatedNodeTransition(args[3], args[4], force);

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "mes"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                HandleMesCommand(args);
                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "scan"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                if (_nodeDetection == null)
                    AddEvent("Node detection runtime is not initialized.");
                else
                    _nodeDetection.Scan();

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "tracked"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                if (_nodeDetection == null)
                    AddEvent("Node detection runtime is not initialized.");
                else
                    AddMultilineEvent(_nodeDetection.GetTrackedSummary());

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "trackclear"))
            {
                _data.PanelVisible = true;
                _data.PanelTab = "INTEL";
                _data.SelectedItemId = "OVERVIEW";
                _hud.Open();

                if (_nodeDetection != null)
                    _nodeDetection.Clear();

                SaveState();
                _hud.Refresh(true);
                return;
            }

            if (Eq(args[1], "runtime") && args.Length >= 3 && Eq(args[2], "reset"))
            {
                ResetState();
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

            CampaignRuntimeStateData restoredState = _ignorePersistedRuntimeStateOnce ? null : _runtimePersistence.LoadRuntimeState();
            _ignorePersistedRuntimeStateOnce = false;

            if (restoredState != null && string.Equals(restoredState.CampaignId, campaign.CampaignId, StringComparison.OrdinalIgnoreCase))
            {
                _runtime.RestoreState(restoredState);
                AddEvent("Existing runtime state restored for: " + campaign.DisplayName);
            }

            _queryApi.SetRuntime(_runtime);
            if (_mesBridge != null)
            {
                _mesBridge.Refresh();

                if (_mesExporter != null)
                    _mesExporter.Export(_mesBridge.Snapshot);

                if (_mesSpawnRequests != null)
                    _mesSpawnRequests.RefreshAndExport();
            }

            if (_eventBus != null)
                _eventBus.Publish(ScenariumEventType.CampaignLoaded, campaign.CampaignId, "Campaign loaded.", "", campaign.InitialState.ToString());

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

            if (_consequences != null)
                _consequences.DestroyNode(nodeId);
            else
                _runtime.DestroyNode(nodeId);
        }

        void CaptureNode(string nodeId)
        {
            if (_runtime == null || _runtime.Campaign == null)
            {
                AddEvent("No runtime campaign loaded. Run /scen reload.");
                return;
            }

            if (_consequences != null)
                _consequences.CaptureNode(nodeId);
            else
                _runtime.CaptureNode(nodeId);
        }



        void RefreshHudRuntimeView()
        {
            UpdateHudViewModel();

            if (_hud != null)
                _hud.Refresh(true);
        }

        void UpdateHudViewModel()
        {
            if (_hud != null)
                _hud.SetViewModel(ScenariumViewModel.FromRuntime(_runtime));
        }





        bool HasOpenBindingForNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            if (_entityBinding == null)
                return false;

            string summary = _entityBinding.BuildSummary();

            if (string.IsNullOrWhiteSpace(summary))
                return false;

            return summary.IndexOf("Node=" + nodeId, StringComparison.OrdinalIgnoreCase) >= 0 &&
                   summary.IndexOf("Open", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        bool HasAnyPendingAutoSpawn()
        {
            return _autoSpawnSessionLock || !string.IsNullOrWhiteSpace(_autoSpawnPendingNodeId);
        }

        void MarkAutoSpawnPending(MesSpawnRequestData request)
        {
            if (request == null)
                return;

            string spawnGroup = !string.IsNullOrWhiteSpace(request.SpawnGroup) ? request.SpawnGroup : request.EncounterTag;

            _autoSpawnPendingNodeId = request.NodeId;
            _autoSpawnPendingSpawnGroup = spawnGroup;
            _autoSpawnPendingTicks = 0;
            _autoSpawnSessionLock = true;
            _autoSpawnSessionLockTicks = 0;
        }

        void ClearAutoSpawnPending()
        {
            _autoSpawnPendingNodeId = null;
            _autoSpawnPendingSpawnGroup = null;
            _autoSpawnPendingTicks = 0;
            _autoSpawnSessionLock = false;
            _autoSpawnSessionLockTicks = 0;
        }

        bool LiveGridAlreadyExistsForRequest(MesSpawnRequestData request)
        {
            if (request == null)
                return false;

            string spawnGroup = !string.IsNullOrWhiteSpace(request.SpawnGroup) ? request.SpawnGroup : request.EncounterTag;

            if (string.IsNullOrWhiteSpace(spawnGroup))
                return false;

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;

                if (grid == null)
                    continue;

                string name = grid.DisplayName ?? "";

                if (name.IndexOf(spawnGroup, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (!string.IsNullOrWhiteSpace(request.NodeId) &&
                    name.IndexOf(request.NodeId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (name.IndexOf("Military Outpost", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    spawnGroup.IndexOf("Outpost", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (name.IndexOf("Regional Base", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    spawnGroup.IndexOf("Regional", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (name.IndexOf("Headquarters", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    spawnGroup.IndexOf("Headquarters", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }


        void RefreshObjectiveBindingsFromCampaign()
        {
            if (_objectives == null)
                _objectives = new ObjectiveRuntime(RunValidatedNodeTransition, AddEvent);

            _objectives.Clear();

            if (_mesSpawnRequests == null)
                return;

            _mesSpawnRequests.Refresh();

            foreach (MesSpawnRequestData request in _mesSpawnRequests.Store.Requests)
            {
                if (request == null || string.IsNullOrWhiteSpace(request.NodeId))
                    continue;

                ScenariumAPI.Objectives.ObjectiveData objective = new ScenariumAPI.Objectives.ObjectiveData();
                objective.NodeId = request.NodeId;
                objective.ObjectiveId = request.NodeId + "_CONTROL";
                objective.ObjectiveType = "ControlBlockDestroyed";
                objective.TargetBlockName = "SCENARIUM_OBJECTIVE_CONTROL";
                objective.OnCompleteTransition = "destroyed";
                objective.Required = true;
                _objectives.AddObjective(objective);
            }

            AddEvent("Objective bindings refreshed from campaign MES nodes.");
        }

        void TryAutoSpawnNextObjective(string reason)
        {
            if (_runtime == null || _runtime.Campaign == null)
                return;

            if (_mesSpawnRequests == null)
                _mesSpawnRequests = new MesSpawnRequestRuntime(_runtime, _mesBridge, AddEvent);

            if (_mesApi == null)
                _mesApi = new MesApiClient(AddEvent);

            if (_mesSpawnBridge == null)
                _mesSpawnBridge = new MesSpawnCommandBridge(_mesSpawnRequests, _mesApi, AddEvent);

            if (_entityBinding == null)
                _entityBinding = new ScenariumEntityBindingRuntime(_runtime, AddEvent, RunValidatedNodeTransition);

            RegisterMesSpawnCallback();

            if (HasAnyPendingAutoSpawn())
                return;

            if (_mesApi == null || !_mesApi.Ready)
            {
                AddEvent("Auto objective spawn waiting for MES API. Reason: " + reason);
                return;
            }

            if (_autoSpawnCooldownTicks > 0)
                return;

            _mesSpawnRequests.RefreshAndExport();

            foreach (MesSpawnRequestData request in _mesSpawnRequests.Store.Requests)
            {
                if (request == null || !request.Allowed)
                    continue;

                if (string.IsNullOrWhiteSpace(request.NodeId))
                    continue;

                if (HasOpenBindingForNode(request.NodeId))
                    return;

                if (_mesSpawnBridge.HasPendingForNode(request.NodeId))
                    return;

                if (LiveGridAlreadyExistsForRequest(request))
                {
                    AddEvent("Auto objective spawn skipped; live grid already exists for " + request.NodeId);
                    _autoSpawnCooldownTicks = AutoSpawnRetryCooldownTicks;
                    return;
                }

                MarkAutoSpawnPending(request);
                bool spawned = _mesSpawnBridge.Request(request);

                if (!spawned)
                {
                    ClearAutoSpawnPending();
                    _autoSpawnCooldownTicks = AutoSpawnRetryCooldownTicks;
                    AddEvent("Auto objective spawn request failed for " + request.NodeId + ". Retry cooldown started.");
                    return;
                }

                AddEvent("Auto objective spawn requested for " + request.NodeId + ". Reason: " + reason);
                return;
            }
        }

        void RegisterMesSpawnCallback()
        {
            if (_mesSpawnCallbackRegistered)
                return;

            if (_mesApi == null || !_mesApi.Ready)
                return;

            _mesApi.RegisterSuccessfulSpawnAction(OnMesSuccessfulSpawn, true);
            _mesSpawnCallbackRegistered = true;
            AddEvent("MES successful-spawn callback registered.");
        }

        void OnMesSuccessfulSpawn(VRage.Game.ModAPI.IMyCubeGrid grid)
        {
            if (grid == null)
            {
                AddEvent("MES successful spawn callback received null grid.");
                return;
            }

            if (_mesSpawnBridge == null || _mesSpawnBridge.Pending == null)
            {
                AddEvent("MES successful spawn received but no Scenarium pending request exists: " + grid.DisplayName);
                return;
            }

            MesPendingSpawnRequest pending = _mesSpawnBridge.Pending;

            if (pending.Consumed)
            {
                AddEvent("MES successful spawn received but pending request was already consumed: " + grid.DisplayName);
                return;
            }

            if (_entityBinding == null)
                _entityBinding = new ScenariumEntityBindingRuntime(_runtime, AddEvent, RunValidatedNodeTransition);

            _entityBinding.BindFromMesSpawn(grid.EntityId, pending.NodeId, pending.SpawnGroup, grid.DisplayName);

            if (_objectives != null)
                _objectives.BindSpawnedGrid(pending.NodeId, grid);

            pending.Consumed = true;
            ClearAutoSpawnPending();
            _autoSpawnCooldownTicks = 0;

            AddEvent("MES successful spawn bound: " + grid.DisplayName + " -> " + pending.NodeId);
            RefreshHudRuntimeView();
            SaveState();
        }

        void HandleMesCommand(string[] args)
        {
            if (_mesBridge == null)
            {
                AddEvent("MES bridge is not initialized.");
                return;
            }

            if (args.Length == 2 || Eq(args[2], "refresh"))
            {
                _mesBridge.Refresh();
                if (_eventBus != null)
                    _eventBus.Publish(ScenariumEventType.MesPermissionsRefreshed, "MES", "MES permissions refreshed.", "", "Refreshed");
                if (_mesExporter != null)
                    _mesExporter.Export(_mesBridge.Snapshot);
                AddMultilineEvent(_mesBridge.BuildSummary(false, false));
                return;
            }

            if (Eq(args[2], "nodes") || Eq(args[2], "all"))
            {
                AddMultilineEvent(_mesBridge.BuildSummary(false, false));
                return;
            }

            if (Eq(args[2], "allowed"))
            {
                AddMultilineEvent(_mesBridge.BuildSummary(true, false));
                return;
            }

            if (Eq(args[2], "denied"))
            {
                AddMultilineEvent(_mesBridge.BuildSummary(false, true));
                return;
            }

            if (Eq(args[2], "api"))
            {
                if (_mesApi == null)
                    _mesApi = new MesApiClient(AddEvent);

                _mesApi.UpdateHandshake();
                RegisterMesSpawnCallback();
                AddEvent(_mesApi.BuildStatus());
                return;
            }

            if (Eq(args[2], "spawn") && args.Length >= 4)
            {
                if (_mesSpawnRequests == null)
                    _mesSpawnRequests = new MesSpawnRequestRuntime(_runtime, _mesBridge, AddEvent);

                if (_mesSpawnBridge == null)
                    _mesApi = new MesApiClient(AddEvent);
            _mesSpawnBridge = new MesSpawnCommandBridge(_mesSpawnRequests, _mesApi, AddEvent);
            RegisterMesSpawnCallback();

                if (Eq(args[3], "next"))
                    _mesSpawnBridge.RequestNext();
                else
                    _mesSpawnBridge.Request(args[3]);

                return;
            }

            if (Eq(args[2], "requests"))
            {
                if (_mesSpawnRequests == null)
                    _mesSpawnRequests = new MesSpawnRequestRuntime(_runtime, _mesBridge, AddEvent);

                if (_mesSpawnBridge == null)
                    _mesApi = new MesApiClient(AddEvent);
            _mesSpawnBridge = new MesSpawnCommandBridge(_mesSpawnRequests, _mesApi, AddEvent);
            RegisterMesSpawnCallback();

                _mesSpawnRequests.RefreshAndExport();
                AddMultilineEvent(_mesSpawnRequests.BuildSummary());
                return;
            }

            if (Eq(args[2], "can") && args.Length >= 4)
            {
                bool allowed = _mesSpawnRequests != null ? _mesSpawnRequests.IsAllowed(args[3]) : _mesBridge.IsSpawnAllowed(args[3]);
                AddEvent("MES spawn allowed for " + args[3] + ": " + allowed);
                return;
            }

            AddEvent("MES commands: /scen mes refresh | nodes | allowed | denied | requests | api | spawn next | spawn <spawnGroup> | can <spawnGroup>");
        }



        void HandlePersistentBindCommand(string[] args)
        {
            if (_entityBinding == null)
                _entityBinding = new ScenariumEntityBindingRuntime(_runtime, AddEvent, RunValidatedNodeTransition);

            if (args.Length == 2 || Eq(args[2], "diagnose"))
            {
                AddMultilineEvent(_entityBinding.BuildDiagnostics());
                return;
            }

            if (Eq(args[2], "scan"))
            {
                _entityBinding.ScanByCampaignBindings();
                AddMultilineEvent(_entityBinding.BuildSummary());
                return;
            }

            if (Eq(args[2], "entities") || Eq(args[2], "tracked"))
            {
                AddMultilineEvent(_entityBinding.BuildSummary());
                return;
            }

            if (Eq(args[2], "clear"))
            {
                _entityBinding.Clear();
                return;
            }

            if (Eq(args[2], "unbind") && args.Length >= 4)
            {
                long entityId;
                if (long.TryParse(args[3], out entityId))
                    _entityBinding.Unbind(entityId);
                else
                    AddEvent("Invalid entity id: " + args[3]);
                return;
            }

            AddEvent("Bind commands: /scen bind scan | entities | diagnose | clear | unbind <entityId>");
        }

        void RunValidatedNodeTransition(string nodeId, string transition, bool force)
        {
            if (_transitionValidator == null)
                _transitionValidator = new NodeTransitionValidator(_runtime);

            NodeTransitionResult result = _transitionValidator.Validate(nodeId, transition, force);

            if (_transitionAudit != null)
                _transitionAudit.Record(result);

            if (!result.Allowed)
            {
                AddEvent("DENIED | " + nodeId + " | " + result.Reason);

                if (_eventBus != null)
                    _eventBus.Publish(ScenariumEventType.TransitionDenied, nodeId, result.Reason, result.PreviousState, result.NewState);

                return;
            }

            if (Eq(transition, "destroy") || Eq(transition, "destroyed"))
                DestroyNode(nodeId);
            else if (Eq(transition, "capture") || Eq(transition, "captured"))
                CaptureNode(nodeId);
            else
                AddEvent("Transition usage: /scen transition node <nodeId> destroyed|captured [force]");

            var after = _runtime != null ? _runtime.GetNodeState(nodeId) : null;
            result.NewState = after != null ? after.State.ToString() : result.PreviousState;

            if (_transitionAudit != null)
                _transitionAudit.Record(result);

            if (_eventBus != null && result.Forced)
                _eventBus.Publish(ScenariumEventType.TransitionForced, nodeId, "Admin force transition applied.", result.PreviousState, result.NewState);

            if (_mesSpawnRequests != null)
                _mesSpawnRequests.RefreshAndExport();

            TryAutoSpawnNextObjective("node transition");

            AddEvent((result.Forced ? "FORCED" : "ALLOW") + " | " + nodeId + " | " + result.PreviousState + " -> " + result.NewState);
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
            AddEvent("Reset command received.");

            _data.EnsureCollections();
            _data.PanelVisible = true;
            _data.PanelTab = "SCENARIO";
            _data.SelectedItemId = "OVERVIEW";

            _eventBus = new ScenariumEventBus(AddEvent);
            _diagnostics = new ScenariumDiagnostics();
            if (_eventBus != null)
                _eventBus.Clear();

            _runtime = new CampaignRuntime(AddEvent);
            _queryApi = new ScenariumQueryApi(_runtime);
            _nodeDetection = new NodeDetectionRuntime(_runtime, AddEvent);
            _entityBinding = new ScenariumEntityBindingRuntime(_runtime, AddEvent, RunValidatedNodeTransition);
            _mesBridge = new MesBindingBridge(_runtime, AddEvent);
            _mesExporter = new MesPermissionExporter(AddEvent);
            _mesSpawnRequests = new MesSpawnRequestRuntime(_runtime, _mesBridge, AddEvent);
            _mesApi = new MesApiClient(AddEvent);
            _mesSpawnBridge = new MesSpawnCommandBridge(_mesSpawnRequests, _mesApi, AddEvent);
            RegisterMesSpawnCallback();
            _consequences = new ConquestConsequenceRuntime(_runtime, _eventBus);
            _bindingValidator = new CampaignBindingValidator();
            _transitionValidator = new NodeTransitionValidator(_runtime);
            _transitionAudit = new TransitionAuditLog();

            _ignorePersistedRuntimeStateOnce = true;

            AddEvent("Runtime state cleared.");
            if (_eventBus != null)
                _eventBus.Publish(ScenariumEventType.RuntimeReset, "Runtime", "Runtime state reset.", "", "Reset");

            TryReloadCampaign();

            if (_mesBridge != null)
            {
                _mesBridge.Refresh();

                if (_mesExporter != null)
                    _mesExporter.Export(_mesBridge.Snapshot);

                if (_mesSpawnRequests != null)
                    _mesSpawnRequests.RefreshAndExport();
            }

            UpdateHudViewModel();

            if (_hud != null)
            {
                _hud.Open();
                _hud.Refresh(true);
            }

            RefreshObjectiveBindingsFromCampaign();
            TryAutoSpawnNextObjective("reset");

            SaveState();
            AddEvent("Scenarium campaign reset complete.");
        }

        void AddMultilineEvent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
                AddEvent(line.Trim());
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
