using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace ScenariumAPI.Integrations.MES
{
    public class MesSpawnCommandBridge
    {
        readonly MesSpawnRequestRuntime _requests;
        readonly Action<string> _log;

        public MesSpawnCommandBridge(MesSpawnRequestRuntime requests, Action<string> log)
        {
            _requests = requests;
            _log = log;
        }

        public bool RequestNext()
        {
            if (_requests == null || _requests.Store == null)
            {
                Log("MES spawn bridge failed: request runtime unavailable.");
                return false;
            }

            foreach (MesSpawnRequestData request in _requests.Store.Requests)
            {
                if (request == null || !request.Allowed)
                    continue;

                string spawnGroup = !string.IsNullOrWhiteSpace(request.SpawnGroup) ? request.SpawnGroup : request.EncounterTag;

                if (string.IsNullOrWhiteSpace(spawnGroup))
                    continue;

                return Request(spawnGroup);
            }

            Log("MES spawn bridge found no allowed spawn request.");
            return false;
        }

        public bool Request(string spawnGroup)
        {
            if (string.IsNullOrWhiteSpace(spawnGroup))
            {
                Log("MES spawn bridge failed: spawn group is empty.");
                return false;
            }

            if (_requests != null && !_requests.IsAllowed(spawnGroup))
            {
                Log("MES spawn request denied by Scenarium state: " + spawnGroup);
                return false;
            }

            // MES does not expose a stable public compile-time API in this mod context.
            // The production bridge is therefore state-driven:
            // 1. Scenarium writes ScenariumAPI_MESSpawnRequests.xml.
            // 2. MES/NPC data consumes that allowed request state.
            // 3. This command marks/refreshes the active request and provides the handoff point.
            _requests.RefreshAndExport();

            BroadcastSpawnRequest(spawnGroup);

            Log("MES spawn requested through Scenarium bridge: " + spawnGroup);
            Log("Spawn request exported for MES/NPC data consumption.");
            return true;
        }

        void BroadcastSpawnRequest(string spawnGroup)
        {
            // Kept intentionally as a local-state/export bridge.
            // Do not directly spawn prefabs here; MES remains the spawning authority.
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMessage("Scenarium", "MES spawn requested: " + spawnGroup);
        }

        void Log(string message)
        {
            if (_log != null)
                _log(message);
        }
    }
}
