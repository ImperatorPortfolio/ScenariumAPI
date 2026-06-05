using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace ScenariumAPI.Integrations.MES
{
    // Minimal MES API client based on Modular Encounters Systems API/MESApi.cs.
    // MES API mod id: 1521905890
    public class MesApiClient
    {
        const long MesModId = 1521905890;

        public bool Ready;
        public int RequestAttempts;
        public string LastStatus;

        Func<Vector3D, List<string>, bool> _spawnPlanetaryInstallation;
        Func<List<string>, MatrixD, Vector3, bool, string, string, bool> _customSpawnRequest;
        Action<Action<IMyCubeGrid>, bool> _registerSuccessfulSpawnAction;

        readonly Action<string> _log;
        bool _registered;

        public MesApiClient(Action<string> log)
        {
            _log = log;
            Register();
            RequestApi();
        }

        public void Register()
        {
            if (_registered)
                return;

            if (MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(MesModId, ApiListener);
            _registered = true;
            LastStatus = "MES API listener registered.";
        }

        public void RequestApi()
        {
            RequestAttempts++;

            if (Ready)
                return;

            if (MyAPIGateway.Utilities == null)
            {
                LastStatus = "Utilities unavailable; cannot request MES API yet.";
                return;
            }

            try
            {
                MyAPIGateway.Utilities.SendModMessage(MesModId, "MESApiRequest");
                LastStatus = "MES API request sent. Attempt " + RequestAttempts;
            }
            catch (Exception e)
            {
                LastStatus = "MES API request failed: " + e.Message;
                Log(LastStatus);
            }
        }

        public void UpdateHandshake()
        {
            if (Ready)
                return;

            Register();
            RequestApi();
        }

        public void Close()
        {
            if (MyAPIGateway.Utilities != null && _registered)
                MyAPIGateway.Utilities.UnregisterMessageHandler(MesModId, ApiListener);

            _registered = false;
        }

        public bool SpawnPlanetaryInstallation(Vector3D coords, List<string> spawnGroups)
        {
            if (!Ready || _spawnPlanetaryInstallation == null)
            {
                Log("MES API is not ready. Cannot request planetary installation spawn.");
                return false;
            }

            return _spawnPlanetaryInstallation.Invoke(coords, spawnGroups);
        }

        public bool CustomSpawnRequest(List<string> spawnGroups, MatrixD spawningMatrix, Vector3 velocity, bool ignoreSafetyCheck, string factionOverride, string spawnProfileId)
        {
            if (!Ready || _customSpawnRequest == null)
            {
                Log("MES API is not ready. Cannot request custom spawn.");
                return false;
            }

            return _customSpawnRequest.Invoke(spawnGroups, spawningMatrix, velocity, ignoreSafetyCheck, factionOverride, spawnProfileId);
        }

        public void RegisterSuccessfulSpawnAction(Action<IMyCubeGrid> action, bool register)
        {
            if (!Ready || _registerSuccessfulSpawnAction == null)
                return;

            _registerSuccessfulSpawnAction.Invoke(action, register);
        }

        public string BuildStatus()
        {
            return "MES API ready: " + Ready + " | Attempts: " + RequestAttempts + " | " + (LastStatus ?? "No status");
        }

        void ApiListener(object data)
        {
            try
            {
                Dictionary<string, Delegate> dict = data as Dictionary<string, Delegate>;

                if (dict == null)
                    return;

                Ready = true;

                if (dict.ContainsKey("SpawnPlanetaryInstallation"))
                    _spawnPlanetaryInstallation = (Func<Vector3D, List<string>, bool>)dict["SpawnPlanetaryInstallation"];

                if (dict.ContainsKey("CustomSpawnRequest"))
                    _customSpawnRequest = (Func<List<string>, MatrixD, Vector3, bool, string, string, bool>)dict["CustomSpawnRequest"];

                if (dict.ContainsKey("RegisterSuccessfulSpawnAction"))
                    _registerSuccessfulSpawnAction = (Action<Action<IMyCubeGrid>, bool>)dict["RegisterSuccessfulSpawnAction"];

                LastStatus = "MES API ready for Scenarium.";
                Log(LastStatus);
            }
            catch (Exception e)
            {
                LastStatus = "MES API failed to load for Scenarium: " + e.Message;
                Log(LastStatus);
            }
        }

        void Log(string text)
        {
            if (_log != null)
                _log(text);
        }
    }
}
