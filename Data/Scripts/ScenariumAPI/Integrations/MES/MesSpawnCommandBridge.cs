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

        MesPendingSpawnRequest _pending;

        public MesPendingSpawnRequest Pending
        {
            get { return _pending; }
        }

        public bool HasPendingForNode(string nodeId)
        {
            if (_pending == null || _pending.Consumed)
                return false;

            return string.Equals(_pending.NodeId, nodeId, StringComparison.OrdinalIgnoreCase);
        }

        public void ClearPending()
        {
            _pending = null;
        }

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

                return Request(request);
            }

            Log("MES spawn bridge found no allowed spawn request.");
            return false;
        }


        public bool Request(MesSpawnRequestData request)
        {
            if (request == null)
            {
                Log("MES spawn bridge failed: request data is null.");
                return false;
            }

            string spawnGroup = !string.IsNullOrWhiteSpace(request.SpawnGroup) ? request.SpawnGroup : request.EncounterTag;

            if (string.IsNullOrWhiteSpace(spawnGroup))
            {
                Log("MES spawn bridge failed: request has no spawn group.");
                return false;
            }

            bool result = Request(spawnGroup);

            if (result)
            {
                _pending = new MesPendingSpawnRequest();
                _pending.SpawnGroup = spawnGroup;
                _pending.NodeId = request.NodeId;
                _pending.EncounterTag = request.EncounterTag;
                _pending.Consumed = false;

                Log("Scenarium pending MES spawn: " + spawnGroup + " -> " + request.NodeId);
            }

            return result;
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

            MatrixD matrix = GetSpawnMatrix();
            List<string> groups = new List<string>();
            groups.Add(spawnGroup);

            bool result = _mesApi.CustomSpawnRequest(
                groups,
                matrix,
                Vector3.Zero,
                true,
                "UTD",
                "ScenariumAPI"
            );

            Log("MES custom spawn request for " + spawnGroup + ": " + result);
            return result;
        }

        MatrixD GetSpawnMatrix()
        {
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
            {
                Vector3D pos = MyAPIGateway.Session.Player.GetPosition();
                Vector3D forward = MyAPIGateway.Session.Player.Character != null ? MyAPIGateway.Session.Player.Character.WorldMatrix.Forward : Vector3D.Forward;
                Vector3D up = MyAPIGateway.Session.Player.Character != null ? MyAPIGateway.Session.Player.Character.WorldMatrix.Up : Vector3D.Up;

                Vector3D spawnPos = pos + forward * 1500;
                return MatrixD.CreateWorld(spawnPos, forward, up);
            }

            return MatrixD.CreateWorld(Vector3D.Zero, Vector3D.Forward, Vector3D.Up);
        }

        void Log(string message)
        {
            if (_log != null)
                _log(message);
        }
    }
}
