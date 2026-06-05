# Scenarium Stage Guide

1. Core cleanup: keep ScenariumAPI generic and keep HUD updates isolated.
2. Campaign authority: campaign packs define nodes, spawn bindings, unlocks, objectives, and rewards.
3. NPC assets: NPC packs provide MES spawn groups, prefabs, factions, behaviours, and spawn compatibility only.
4. Automatic objective spawning: Scenarium reads campaign state and asks MES to spawn the next allowed objective.
5. Automatic spawn binding: Scenarium binds MES successful-spawn grids to campaign nodes using pending campaign requests.
6. Gameplay loop: destroy/capture objective -> Scenarium validates transition -> campaign consequences reveal next objective.
7. Quest/objective runtime: campaign data drives quest progress from node/world events.
8. Reward/world-fact runtime: data-driven rewards, facts, faction state, and unlocks.
9. Persistence hardening: persist campaign state, bindings, facts, quests, rewards, and migration metadata.
10. HUD v2: improve UI after backend data is stable; HUD packages stay UI-only.
