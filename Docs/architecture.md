# ScenariumAPI Architecture Document

**Repository:** `ImperatorPortfolio/ScenariumAPI`  
**Document status:** Architecture baseline  
**Primary purpose:** Generic campaign, scenario, quest, and conquest framework for Space Engineers server gameplay.  
**Primary consumer campaign:** Solar Frontier Campaign  
**Prepared for:** Imperator  
**Target quality:** Build-candidate architecture specification  

---

## 1. Executive Summary

ScenariumAPI is the strategic campaign framework for a Space Engineers server. It exists to provide persistent campaign state, scenario progression, quest orchestration, conquest ownership, faction status, world-state facts, and integration points for systems such as Torch and Modular Encounters Systems.

ScenariumAPI should not become a single hardcoded campaign. The API must remain generic, while campaign packs define specific factions, scenarios, quest chains, conquest nodes, objectives, rewards, escalation rules, and encounter bindings.

The governing architectural rule is:

> Scenarium owns campaign state. Other systems consume, query, or react to it.

MES, NPC encounter packs, faction content packs, admin tooling, and Solar Frontier content should not become the authority for campaign progress. They should be driven by Scenarium state.

The first production target is not a full campaign editor or a complete Solar Frontier campaign. The first target is a reliable vertical slice:

```text
Load campaign pack
Load faction
Load conquest node
Destroy or capture node
Apply consequence
Persist state
Expose query to integration
```

That loop proves Scenarium can act as the campaign brain.

---

## 2. Product Goals

### 2.1 Primary Goals

ScenariumAPI must provide:

1. A generic campaign runtime.
2. Scenario activation and progression.
3. Quest and objective tracking.
4. Persistent conquest-node ownership.
5. Persistent faction state.
6. A queryable world-state store.
7. Server-side event publication.
8. Torch command and admin tooling.
9. Integration boundaries for MES and other systems.
10. A campaign-pack loading and validation pipeline.
11. Safe persistence across server restarts.
12. Diagnostics suitable for live server administration.

### 2.2 Non-Goals

ScenariumAPI should not directly become:

1. A hardcoded Solar Frontier campaign.
2. A replacement for MES encounter spawning.
3. A replacement for Space Engineers faction ownership.
4. A grid physics or damage simulation system.
5. A full player economy system.
6. A client UI framework.
7. A production content pack.
8. A random event spawner with no campaign state.

It can integrate with those systems, but it should not absorb their responsibilities.

---

## 3. Core Design Principles

### 3.1 API First, Campaign Second

ScenariumAPI is the framework. Solar Frontier is content. That boundary must remain strict.

Correct:

```text
ScenariumAPI
  Generic campaign/conquest/quest runtime.

SolarFrontier-Campaign
  Campaign definitions, faction data, quest chains, node data, MES bindings.

MES Packs
  Spawn groups, encounter definitions, faction behavior content.
```

Incorrect:

```text
ScenariumAPI
  Hardcoded pirate faction.
  Hardcoded Mars HQ.
  Hardcoded Solar Frontier scenario order.
  Hardcoded MES spawn group names.
```

### 3.2 State Authority Belongs to Scenarium

Scenarium is responsible for the strategic state machine.

It owns:

- campaign status
- scenario status
- quest status
- objective status
- conquest node ownership/status
- campaign faction state
- world flags
- persisted campaign facts

Other systems should ask Scenarium questions such as:

```text
Can faction X spawn here?
Is scenario Y active?
Is node Z destroyed?
Is faction A defeated?
What escalation level applies in this region?
```

### 3.3 Data-Driven Content

Campaign packs should be authored primarily with JSON or equivalent structured data.

Code defines the runtime.  
Data defines the campaign.

This allows multiple campaign packs to exist without recompiling ScenariumAPI.

### 3.4 Deterministic Server Behavior

Scenarium must behave predictably on a multiplayer server. It must avoid hidden one-off state, random irreversible behavior without logging, and untracked consequences.

Every important state transition should be:

1. Validated.
2. Logged.
3. Evented.
4. Persisted.
5. Recoverable or inspectable by admin command.

---

## 4. Repository Model

### 4.1 ScenariumAPI Repository

`ScenariumAPI` contains reusable framework code, contracts, runtime services, persistence providers, integrations, diagnostics, and documentation.

Recommended structure:

```text
ScenariumAPI/
  README.md

  docs/
    architecture.md
    campaign-pack-format.md
    conquest-runtime.md
    quest-runtime.md
    objective-runtime.md
    mes-integration.md
    persistence.md
    torch-commands.md
    testing-strategy.md

  src/
    Scenarium.Api/
      Campaigns/
      Scenarios/
      Quests/
      Objectives/
      Conquest/
      Factions/
      WorldState/
      Events/
      Persistence/
      Loading/
      Validation/
      Diagnostics/
      Integrations/
      Common/

    Scenarium.Torch/
      Plugin/
      Commands/
      Config/
      Storage/
      Logging/
      Lifecycle/

    Scenarium.MES/
      Adapter/
      Queries/
      EventBridge/

    Scenarium.Tests/
      Loading/
      Runtime/
      Persistence/
      Conquest/
      Objectives/
      Validation/
      Integration/
```

### 4.2 SolarFrontier-Campaign Repository

The Solar Frontier campaign repository should contain content only.

Recommended structure:

```text
SolarFrontier-Campaign/
  Campaign.json

  Factions/
    SolarFrontier.Factions.json

  Scenarios/
    001_MarsFrontier.json
    002_LunarRelay.json
    003_BeltEscalation.json
    004_CeresSiege.json

  Quests/
    Mars/
      mars_recon_chain.json
      mars_hq_assault.json
    Luna/
      lunar_relay_chain.json
    Belt/
      belt_interdiction_chain.json

  Conquest/
    Mars.ConquestNodes.json
    Luna.ConquestNodes.json
    Belt.ConquestNodes.json

  Objectives/
    Common.ObjectiveTemplates.json

  Rewards/
    RewardTables.json

  Integrations/
    MES/
      MesEncounterBindings.json
      MesFactionSpawnRules.json

  Localization/
    en.json
```

### 4.3 Other Future Campaign Packs

Scenarium should eventually support multiple campaign packs:

```text
Scenarium-Campaigns/
  SolarFrontier/
  CorporateWar/
  PirateWarlords/
  SystemCollapse/
  NomadExodus/
```

The server may eventually support:

- one active campaign
- multiple installed inactive campaigns
- optional scenario packs
- optional quest packs
- optional conquest packs
- optional integration packs

Milestone 1 should support one installed and active campaign to keep the foundation stable.

---

## 5. Runtime Vocabulary

The vocabulary below must be consistent across code, JSON, admin commands, logs, documentation, and user-facing diagnostics.

### 5.1 Campaign

A campaign is the highest-level playable arc.

Example:

```text
Solar Frontier
```

A campaign owns:

- campaign ID
- display name
- version
- author
- schema version
- scenario list
- faction definitions
- global world-state defaults
- starting scenario
- victory conditions
- failure conditions
- dependencies
- integration bindings

Suggested lifecycle:

```text
Installed
Loaded
Validated
Inactive
Starting
Active
Paused
Completed
Failed
Unloaded
```

### 5.2 Scenario

A scenario is a bounded operational chapter inside a campaign.

Examples:

```text
Mars Frontier
Lunar Relay Crisis
Ceres Siege
Outer Belt Interdiction
```

A scenario owns:

- scenario ID
- campaign ID
- prerequisites
- activation conditions
- quest chains
- conquest nodes
- encounter permissions
- escalation rules
- completion conditions
- failure conditions

Suggested lifecycle:

```text
Locked
Available
Active
Suspended
Completed
Failed
Expired
```

Milestone 1 should assume one active scenario.

### 5.3 Quest

A quest is a player-facing task chain.

A quest may be:

- server-wide
- faction-wide
- player-specific
- party-specific
- alliance-specific
- hidden
- repeatable
- one-shot

Suggested lifecycle:

```text
Unavailable
Available
Offered
Accepted
Active
ReadyToComplete
Completed
Failed
Abandoned
Expired
```

### 5.4 Objective

An objective is a measurable condition.

Examples:

```text
Destroy grid tagged SF_MARS_HQ
Enter zone Mars_North_Approach
Deliver 10000 Iron Ingots
Scan beacon Lunar_Relay_01
Capture node Ceres_Dockyard
Survive 15 minutes
```

Objectives should be composable. A quest should be able to require:

- one objective
- all objectives
- any objective
- at least N objectives
- weighted objective completion
- ordered objective completion

### 5.5 Conquest Node

A conquest node is a persistent strategic entity.

Examples:

- faction HQ
- forward operating base
- planetary region
- asteroid relay
- orbital station
- shipyard
- mining outpost
- communications array
- logistics hub

A conquest node has:

1. Stable ID.
2. Display name.
3. Type.
4. Location.
5. Owner faction.
6. Status.
7. Optional linked grid IDs.
8. Optional linked GPS/zone definitions.
9. Optional objective bindings.
10. Optional MES spawn bindings.
11. Optional rewards.
12. Optional consequences.

Suggested lifecycle:

```text
Unknown
Discovered
Contested
Captured
Fortified
Destroyed
Disabled
Abandoned
```

### 5.6 Faction State

Faction state is Scenarium's campaign-level view of a faction. It is not just the vanilla Space Engineers faction record.

Suggested states:

```text
Hidden
Dormant
Active
Alerted
Escalating
Weakened
Defeated
Allied
Hostile
Neutral
```

### 5.7 World State

World state is the persistent fact store used by campaigns and integrations.

Examples:

```text
campaign.active = solar_frontier
scenario.active = mars_frontier
faction.pirate_clan_red.status = weakened
node.mars_pirate_hq.status = destroyed
node.mars_pirate_hq.owner = player_alliance
quest.mars_hq_assault.completed = true
```

---

## 6. Architectural Layers

### 6.1 Contracts Layer

The contracts layer defines public interfaces, identifiers, DTOs, enums, and API shapes. It must remain generic and stable.

Core contracts:

```csharp
public interface IScenariumCampaign { }
public interface IScenariumScenario { }
public interface IScenariumQuest { }
public interface IScenariumObjective { }
public interface IConquestNode { }
public interface IFactionState { }
public interface IWorldStateStore { }
public interface IScenariumEventBus { }
public interface IScenariumIntegration { }
```

Contracts should not reference Solar Frontier.

### 6.2 Runtime Layer

The runtime layer owns execution state.

Primary services:

```text
CampaignRuntime
ScenarioRuntime
QuestRuntime
ObjectiveRuntime
ConquestRuntime
FactionRuntime
WorldStateRuntime
```

### 6.3 Persistence Layer

The persistence layer serializes and restores campaign state.

Primary services:

```text
IWorldStateStore
IPersistenceProvider
JsonPersistenceProvider
TorchStoragePersistenceProvider
StateMigrationService
```

Persistence must be versioned, deterministic, and recoverable.

### 6.4 Loading and Validation Layer

The loading layer reads campaign packs. The validation layer rejects broken content before runtime activation.

Validation must catch:

1. Duplicate IDs.
2. Missing references.
3. Invalid scenario prerequisites.
4. Invalid quest objective references.
5. Invalid faction IDs.
6. Invalid conquest node references.
7. Invalid consequence references.
8. Unsupported schema versions.
9. Invalid MES binding references where statically knowable.
10. Circular prerequisite chains.

### 6.5 Integration Layer

Integrations bridge Scenarium to external systems.

Initial integrations:

```text
Torch integration
MES integration
Space Engineers world/grid events
Admin command integration
```

The integration layer must not become the campaign authority.

---

## 7. Runtime Lifecycle

### 7.1 Server Startup

Startup sequence:

1. Torch plugin loads.
2. Scenarium services are created.
3. Configuration is read.
4. Campaign pack directories are scanned.
5. Campaign definitions are loaded.
6. Campaign data is validated.
7. Persistent world state is loaded.
8. Runtime state is reconciled with campaign definitions.
9. Integrations are initialized.
10. Commands are registered.
11. Scenarium enters ready state.

### 7.2 Campaign Activation

Campaign activation sequence:

1. Admin starts campaign or autostart selects campaign.
2. Campaign prerequisites are checked.
3. Initial world-state keys are created.
4. Starting scenario is selected.
5. Scenario prerequisites are checked.
6. Scenario runtime activates.
7. Starting quests, objectives, and nodes are activated.
8. Events are published.
9. State is saved.

### 7.3 Runtime Tick Policy

Scenarium should avoid heavy per-tick work.

Preferred model:

1. Event-driven updates wherever possible.
2. Low-frequency reconciliation ticks for safety.
3. Explicit admin-triggered diagnostics.
4. Scheduled saves with dirty-state tracking.

Suggested timing model:

```text
Fast events: handled immediately.
Objective polling: every 1-10 seconds depending on objective type.
Persistence save: dirty-state debounce, for example 30-120 seconds.
Diagnostics: admin triggered.
```

---

## 8. Campaign Pack Format

### 8.1 Campaign Definition

Example:

```json
{
  "schema": "scenarium.campaign.v1",
  "id": "solar_frontier",
  "name": "Solar Frontier",
  "version": "0.1.0",
  "author": "ImperatorPortfolio",
  "startingScenario": "mars_frontier",
  "factions": [
    "Factions/SolarFrontier.Factions.json"
  ],
  "scenarios": [
    "Scenarios/001_MarsFrontier.json"
  ],
  "conquest": [
    "Conquest/Mars.ConquestNodes.json"
  ],
  "integrations": {
    "mes": "Integrations/MES/MesEncounterBindings.json"
  }
}
```

### 8.2 Scenario Definition

Example:

```json
{
  "schema": "scenarium.scenario.v1",
  "id": "mars_frontier",
  "campaignId": "solar_frontier",
  "name": "Mars Frontier",
  "activation": {
    "requires": []
  },
  "quests": [
    "mars_recon_chain",
    "mars_hq_assault"
  ],
  "conquestNodes": [
    "mars_pirate_hq"
  ],
  "completion": {
    "all": [
      "node.mars_pirate_hq.destroyed"
    ]
  }
}
```

### 8.3 Faction Definition

Example:

```json
{
  "schema": "scenarium.factions.v1",
  "factions": [
    {
      "id": "pirate_clan_red",
      "name": "Red Clan Pirates",
      "initialState": "Active",
      "vanillaFactionTag": "RCP",
      "defeatPolicy": {
        "majorSpawnsAllowed": false,
        "remnantSpawnsAllowed": true
      }
    }
  ]
}
```

### 8.4 Conquest Node Definition

Example:

```json
{
  "schema": "scenarium.conquest-node.v1",
  "id": "mars_pirate_hq",
  "name": "Mars Pirate HQ",
  "type": "FactionHQ",
  "initialOwner": "pirate_clan_red",
  "initialStatus": "Hidden",
  "location": {
    "planet": "Mars",
    "position": {
      "x": 0,
      "y": 0,
      "z": 0
    },
    "radiusMeters": 5000
  },
  "objectives": {
    "destroy": "objective.destroy_mars_pirate_hq"
  },
  "consequences": {
    "onDestroyed": [
      {
        "setFactionState": {
          "factionId": "pirate_clan_red",
          "state": "Defeated"
        }
      },
      {
        "setWorldFlag": {
          "key": "node.mars_pirate_hq.destroyed",
          "value": true
        }
      }
    ]
  }
}
```

---

## 9. Objective System

### 9.1 Objective Types

Initial objective types:

1. DestroyGridObjective.
2. CaptureNodeObjective.
3. EnterZoneObjective.
4. DeliverCargoObjective.
5. ScanEntityObjective.
6. SurviveDurationObjective.
7. KillCountObjective.
8. MaintainOwnershipObjective.
9. ManualObjective.
10. CompositeObjective.

### 9.2 Objective Completion

Objective completion must be idempotent. Completing an already completed objective must not duplicate rewards, events, or state transitions.

Completion flow:

```text
Objective condition met
  -> validate objective is active
  -> mark completed
  -> publish ObjectiveCompleted event
  -> update quest progress
  -> execute consequences
  -> mark world state dirty
  -> persist state
```

### 9.3 Composite Objectives

Composite modes:

```text
All
Any
CountAtLeast
WeightedScore
OrderedSequence
```

---

## 10. Conquest System

### 10.1 Conquest Runtime Responsibilities

The conquest runtime owns:

1. Node registration.
2. Node discovery.
3. Node ownership.
4. Node status.
5. Node capture/destroy/disable transitions.
6. Node-linked objective activation.
7. Node-linked consequence execution.
8. Node persistence.
9. Node query APIs for integrations.

### 10.2 Ownership Model

A node can be owned by:

1. NPC faction.
2. Player faction.
3. Alliance.
4. Server/system.
5. Neutral.
6. Unknown.

Ownership changes must emit events:

```text
ConquestNodeOwnerChanged
ConquestNodeCaptured
ConquestNodeDestroyed
ConquestNodeContested
ConquestNodeFortified
```

### 10.3 HQ Defeat Rule

The first vertical slice should support this rule:

> When a faction HQ node is destroyed, Scenarium marks the owning faction as defeated and suppresses major encounter spawns for that faction.

This rule must be data-driven, not hardcoded.

---

## 11. Faction System

### 11.1 Faction Runtime Responsibilities

The faction runtime owns campaign-level faction status, not vanilla SE faction membership.

Responsibilities:

1. Load campaign faction definitions.
2. Track faction state.
3. Apply state transitions.
4. Publish faction events.
5. Expose faction queries.
6. Persist faction state.

### 11.2 Faction State Transitions

Suggested transition examples:

```text
Dormant -> Active
Active -> Alerted
Alerted -> Escalating
Escalating -> Weakened
Weakened -> Defeated
Hidden -> Active
Neutral -> Hostile
Hostile -> Allied
```

Invalid transitions should be logged and rejected unless forced by an admin command.

---

## 12. World State Store

### 12.1 Purpose

The world-state store is the durable truth source for campaign state.

It should support:

1. Boolean flags.
2. Strings.
3. Numbers.
4. Timestamps.
5. Ownership values.
6. Objective states.
7. Quest states.
8. Scenario states.
9. Faction states.
10. Conquest node states.

### 12.2 Requirements

World state must be:

1. Versioned.
2. Serializable.
3. Human-inspectable where possible.
4. Resilient to missing campaign content.
5. Migratable across schema versions.
6. Safe against duplicate event application.

---

## 13. Event Bus

### 13.1 Purpose

The event bus decouples runtime systems. Objectives, quests, conquest, factions, persistence, MES integration, and admin diagnostics should communicate through structured events where practical.

### 13.2 Core Events

Initial events:

```text
CampaignStarted
CampaignCompleted
CampaignFailed
ScenarioStarted
ScenarioCompleted
ScenarioFailed
QuestStarted
QuestCompleted
QuestFailed
ObjectiveStarted
ObjectiveCompleted
ObjectiveFailed
ConquestNodeDiscovered
ConquestNodeContested
ConquestNodeCaptured
ConquestNodeDestroyed
FactionStateChanged
WorldFlagChanged
PersistenceSaved
PersistenceLoaded
IntegrationQueryReceived
```

### 13.3 Event Rules

Events should be:

1. Typed.
2. Timestamped.
3. Idempotent where they mutate state.
4. Loggable.
5. Safe for integrations.
6. Free from raw persisted game-object references.

---

## 14. Torch Integration

### 14.1 Responsibilities

The Torch integration owns:

1. Plugin startup and shutdown.
2. Config loading.
3. Command registration.
4. Server logging.
5. Save path resolution.
6. Lifecycle hooks.
7. Permission checks.

### 14.2 Required Admin Commands

Minimum commands:

```text
/scenarium status
/scenarium reload
/scenarium save
/scenarium campaign list
/scenarium campaign start <campaignId>
/scenarium scenario list
/scenarium scenario start <scenarioId>
/scenarium quest list
/scenarium quest start <questId>
/scenarium objective complete <objectiveId>
/scenarium node list
/scenarium node status <nodeId>
/scenarium node capture <nodeId> <ownerId>
/scenarium node destroy <nodeId>
/scenarium faction list
/scenarium faction status <factionId>
/scenarium faction set-state <factionId> <state>
/scenarium world get <key>
/scenarium world set <key> <value>
/scenarium validate
/scenarium diagnostics
```

Admin commands are required for testing and live-server recovery.

---

## 15. MES Integration

### 15.1 Integration Rule

MES should be a consumer of Scenarium state, not the campaign authority.

Correct model:

```text
Scenarium owns campaign state.
MES asks what can spawn.
MES encounters react to Scenarium state.
```

Incorrect model:

```text
MES spawns define campaign progress.
Scenarium guesses what happened afterward.
```

### 15.2 MES Query Examples

The MES adapter should support queries such as:

```text
CanFactionSpawn(factionId, location)
CanEncounterSpawn(encounterId, location)
IsScenarioActive(scenarioId)
IsConquestNodeActive(nodeId)
IsFactionDefeated(factionId)
GetThreatLevel(regionId)
GetEscalationLevel(factionId)
```

### 15.3 Spawn Suppression

When a faction is defeated, Scenarium should be able to suppress major spawns for that faction while optionally allowing remnants, salvage, deserters, or revenge encounters.

This must be campaign-data-driven.

---

## 16. Persistence

### 16.1 Save Data Categories

Save state should include:

1. Active campaign ID.
2. Campaign lifecycle state.
3. Active scenario IDs.
4. Scenario states.
5. Quest states.
6. Objective states.
7. Conquest node states.
8. Faction states.
9. World flags.
10. Applied event IDs where required for idempotency.
11. Schema version.
12. Last save timestamp.

### 16.2 Save Strategy

Use dirty-state tracking.

Recommended policy:

1. Mark dirty after campaign-state mutation.
2. Save after debounce interval.
3. Save immediately for high-impact admin commands.
4. Save on clean shutdown.
5. Write atomically where possible.

### 16.3 Migration

Save files must include schema version.

Migration service should support:

```text
v1 -> v2
v2 -> v3
```

Unsupported future versions should fail safely and refuse to load unless forced.

---

## 17. Diagnostics and Logging

Scenarium must be diagnosable on a live server.

Diagnostics should report:

1. Loaded campaigns.
2. Active campaign.
3. Active scenarios.
4. Quest counts by state.
5. Objective counts by state.
6. Conquest node counts by state.
7. Faction states.
8. Last save time.
9. Validation warnings.
10. Integration status.
11. Recent events.

Logs should distinguish:

```text
Info
Warning
Error
Fatal
AdminAction
Validation
Persistence
Integration
```

---

## 18. Security and Server Safety

Scenarium must be safe to run on a live multiplayer server.

Required controls:

1. Admin-only mutation commands.
2. Read-only commands for lower permission roles where desired.
3. Validation before activation.
4. Safe failure on invalid campaign data.
5. No uncontrolled reflection/plugin loading for campaign packs in Milestone 1.
6. No unbounded per-tick scans.
7. No direct trust in player-provided names or strings.
8. Defensive handling of deleted grids, renamed factions, and missing entities.
9. Clear recovery commands for invalid state.

---

## 19. Testing Strategy

### 19.1 Unit Tests

Required unit test areas:

1. Campaign file loading.
2. Duplicate ID validation.
3. Missing reference validation.
4. Faction state transitions.
5. Conquest node transitions.
6. Objective completion idempotency.
7. Consequence execution.
8. Persistence round-trip.
9. Save migration.
10. Event publication.

### 19.2 Integration Tests

Required integration tests:

1. Load test campaign pack.
2. Start test campaign.
3. Activate scenario.
4. Destroy HQ node via runtime call.
5. Verify faction defeat.
6. Verify world flag update.
7. Verify persistence.
8. Reload state.
9. Verify MES query returns spawn suppression.

### 19.3 Manual Server Acceptance

Manual acceptance should verify:

1. Server starts with Scenarium installed.
2. `/scenarium status` works.
3. Campaign pack validates.
4. Admin can start campaign.
5. Admin can list nodes.
6. Admin can destroy node.
7. Node state persists after restart.
8. Faction state persists after restart.
9. MES adapter query reflects faction defeat.

---

## 20. Milestone Plan

### Milestone 1: Conquest Node Runtime

Scope:

1. Campaign loading skeleton.
2. Conquest node definitions.
3. Faction definitions.
4. World-state store.
5. Persistence provider.
6. Admin commands for node/faction/world state.
7. Node capture/destroy state transitions.
8. Faction defeat consequence.
9. MES query stub.

Acceptance criteria:

1. Server loads Scenarium without campaign-specific hardcoding.
2. Campaign pack validates.
3. Admin can list conquest nodes.
4. Admin can destroy a test HQ node.
5. Destroying HQ updates world state.
6. Destroying HQ marks faction defeated via data-driven consequence.
7. Faction state persists across restart.
8. Node state persists across restart.
9. MES adapter can query whether the defeated faction may spawn.
10. No Solar Frontier-specific logic exists in the API runtime.

Status target: **Build-candidate**.

### Milestone 2: Quest and Objective Runtime

Scope:

1. Quest definitions.
2. Objective definitions.
3. Objective completion.
4. Quest completion.
5. Composite objectives.
6. Quest admin commands.
7. Persistence for quest/objective state.

Status target: **Build-candidate**.

### Milestone 3: Scenario Runtime

Scope:

1. Scenario definitions.
2. Scenario prerequisites.
3. Scenario activation.
4. Scenario completion conditions.
5. Scenario-driven quest/node activation.
6. Scenario admin commands.

Status target: **Build-candidate**.

### Milestone 4: MES Integration

Scope:

1. Spawn permission queries.
2. Faction suppression.
3. Encounter gating by scenario.
4. Escalation-state queries.
5. Campaign-defined MES binding files.

Status target: **Build-candidate**.

### Milestone 5: Solar Frontier Vertical Slice

Scope:

1. One campaign.
2. One scenario.
3. One enemy faction.
4. One HQ conquest node.
5. One quest chain.
6. One destroy-HQ objective.
7. One faction defeat consequence.
8. One MES spawn suppression rule.

Acceptance criteria:

1. A player or admin can start Solar Frontier.
2. Mars scenario becomes active.
3. Enemy HQ node exists.
4. HQ objective can be completed.
5. Faction state changes to defeated.
6. MES major spawns are suppressed for that faction.
7. State survives restart.

Status target: **Build-candidate**.

---

## 21. Implementation Order

Recommended build order:

```text
1. Core IDs, enums, result types, and contracts.
2. Campaign pack file models.
3. Loader and validator.
4. World-state store.
5. Faction runtime.
6. Conquest node runtime.
7. Consequence executor.
8. Persistence provider.
9. Torch commands.
10. MES query adapter stub.
11. Solar Frontier vertical-slice campaign pack.
```

Do not begin with the full quest system.  
Do not begin with a campaign editor.  
Do not begin with broad MES content integration.  
Do not hardcode Solar Frontier inside ScenariumAPI.

---

## 22. First Implementation Target

The next implementation target should be:

```text
Milestone 1: Conquest Node Runtime
```

The smallest useful strategic loop is:

```text
Load campaign pack
Load faction
Load conquest node
Admin destroys/captures node
Consequence updates faction state
World state persists
MES adapter can query result
```

This proves that Scenarium is the campaign brain.

---

## 23. Product Quality Bar

Scenarium should be treated as production server infrastructure.

Required quality properties:

1. Deterministic state transitions.
2. Defensive validation.
3. Clear logs.
4. Safe persistence.
5. Explicit admin recovery commands.
6. No hardcoded campaign content in the API.
7. Idempotent objective and consequence handling.
8. Integration boundaries that do not leak campaign authority.
9. Versioned data schemas.
10. Stable public contracts.

---

## 24. Known Limitations of This Architecture Baseline

This document does not yet define:

1. Final C# file/class implementation.
2. Final JSON Schema files.
3. Torch-specific command syntax implementation.
4. Actual MES API call mechanics.
5. Full Solar Frontier campaign content.
6. Player UI design.
7. Multiplayer party/alliance quest semantics.
8. Reward economy balancing.
9. Full test project layout.
10. Deployment packaging.

Those should be defined in follow-up documents and implementation milestones.

---

## 25. Status

**Status:** Build-candidate architecture baseline.

This document is suitable as the foundation for ScenariumAPI Milestone 1 planning and implementation. It is not yet a complete implementation specification.
