SCENARIUMAPI STARTER PACKAGE

Purpose:
This is the first-load scaffold for ScenariumAPI, intended to verify that a Space Engineers session component loads, persists simple campaign data, accepts chat commands, and displays a quest menu scaffold.

Install:
Place this folder or ZIP contents into your Space Engineers Mods folder, or upload it as a local mod.

Current Commands:
/scen help
/scen status
/scen quest
/scen menu
/scen complete UTD_OUTPOST
/scen complete UTD_REGIONAL_BASE
/scen complete UTD_HQ
/scen war UTD
/scen save
/scen reset

Current Behavior:
- Creates default SolarWar campaign state.
- Tracks UTD state.
- Tracks a simple conquest chain: Outpost -> Regional Base -> HQ -> Gate Component.
- Saves state to world storage as ScenariumAPI_State.xml.
- Displays quest information using the vanilla mission screen as a safe fallback.

RichHudText Note:
This package is RichHud-ready in design but uses a vanilla-safe quest screen for the first load test.
The next package should replace ShowQuestMenu() with RichHud/RichHudText calls once dependency loading is confirmed in your local SE install.

Next Development Pass:
1. Add RichHudText dependency wrapper.
2. Add external scenario registration API.
3. Add objective condition types.
4. Add GPS reveal rewards.
5. Add MES spawn-state bridge.
6. Add UTD conquest module.
