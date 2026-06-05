# ScenariumAPI Stage Guide

## 1. Core cleanup
Clean `ScenariumAPI.cs`, remove duplicated initialization, normalize constructors, and keep HUD files out of backend updates.

## 2. Functional MES spawn request runtime
Scenarium creates real spawn permission/request state from campaign data automatically on load, reset, and node transition.

## 3. SolarFrontier campaign data pass
Campaign pack defines objective spawn policies: node -> MES spawn group -> allowed state.

## 4. UTD NPC pack spawn wiring
Add valid MES objective spawn groups using real UTD prefabs: `UTD_MilitaryOutpost`, `UTD_RegionalBase`, `UTD_Headquarters`.

## 5. MES-controlled spawning
MES naturally spawns only currently allowed objective groups. Manual spawning is not the main workflow.

## 6. Entity binding after spawn
Scenarium binds spawned grids by EntityId/spawn identity, not CustomData.

## 7. Gameplay transition loop
Destroy/capture spawned objective -> Scenarium validates transition -> applies consequences -> reveals next node -> refreshes MES permissions.

## 8. Quest/objective runtime
Node events complete objectives, advance quests, and unlock next campaign steps.

## 9. Reward/world-fact runtime
Rewards, world facts, unlocks, faction state changes, and MES enable/disable effects become data-driven.

## 10. Persistence hardening
Persist campaign state, quest state, entity bindings, world facts, rewards, and migration/version info safely.

## 11. Admin tools after functionality
Add diagnostics, help, and reporting after the actual gameplay loop works.

## 12. HUD v2 last
Improve UI once backend data is stable. HUD updates should be HUD-only packages unless explicitly combined.
