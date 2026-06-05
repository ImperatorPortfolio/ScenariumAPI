using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;

namespace ScenariumAPI.Integrations.MES
{
    public class MesSpawnCommandBridge
    {
        readonly MesSpawnRequestRuntime _requests;
        readonly MesApiClient _mesApi;
        readonly Action<string> _log;

        public MesSpawnCommandBridge(MesSpawnRequestRuntime requests, MesApiClient mesApi, Action<string> log)
        {
            _requests = requests;
            _mesApi = mesApi;
            _log = log;
        }

        public bool RequestNext()
        {
            if (_requests == null || _requests.Store == null)
            {
                Log("MES spawn bridge failed: request runtime unavailable.");
                return false;
            }

            _requests.RefreshAndExport();

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

            if (_requests == null)
            {
                Log("MES spawn bridge failed: request runtime unavailable.");
                return false;
            }

            _requests.RefreshAndExport();

            if (!_requests.IsAllowed(spawnGroup))
            {
                Log("MES spawn request denied by Scenarium state: " + spawnGroup);
                return false;
            }

            if (_mesApi == null || !_mesApi.Ready)
            {
                Log("MES API is not ready. Spawn request not sent: " + spawnGroup);
                return false;
            }

            Vector3D coords = GetSpawnCoords();
            List<string> groups = new List<string>();
            groups.Add(spawnGroup);

            bool result = _mesApi.SpawnPlanetaryInstallation(coords, groups);

            Log("MES planetary installation spawn request for " + spawnGroup + ": " + result);
            return result;
        }

        Vector3D GetSpawnCoords()
        {
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
                return MyAPIGateway.Session.Player.GetPosition();

            return Vector3D.Zero;
        }

        void Log(string message)
        {
            if (_log != null)
                _log(message);
        }
    }
}
