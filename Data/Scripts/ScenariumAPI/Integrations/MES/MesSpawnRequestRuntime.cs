using Sandbox.ModAPI;
using System;
using System.IO;
using ScenariumAPI.Runtime;

namespace ScenariumAPI.Integrations.MES
{
    public class MesSpawnRequestRuntime
    {
        const string ExportFile = "ScenariumAPI_MESSpawnRequests.xml";

        readonly CampaignRuntime _runtime;
        readonly MesBindingBridge _bridge;
        readonly Action<string> _log;
        readonly MesSpawnRequestStore _store = new MesSpawnRequestStore();

        public MesSpawnRequestRuntime(CampaignRuntime runtime, MesBindingBridge bridge, Action<string> log)
        {
            _runtime = runtime;
            _bridge = bridge;
            _log = log;
        }

        public MesSpawnRequestStore Store
        {
            get { return _store; }
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public void RefreshAndExport()
        {
            Refresh();
            Export();
        }

        public void Refresh()
        {
            _store.Clear();

            if (_bridge == null)
            {
                Log("MES spawn request refresh failed: bridge unavailable.");
                return;
            }

            _bridge.Refresh();

            if (_bridge.Snapshot == null || _bridge.Snapshot.Permissions == null)
            {
                Log("MES spawn request refresh failed: permission snapshot unavailable.");
                return;
            }

            foreach (MesSpawnPermission permission in _bridge.Snapshot.Permissions)
            {
                if (permission == null)
                    continue;

                MesSpawnRequestData request = new MesSpawnRequestData();
                request.NodeId = permission.NodeId;
                request.FactionTag = permission.FactionTag;
                request.SpawnGroup = permission.SpawnGroup;
                request.EncounterTag = permission.EncounterTag;
                request.NodeState = permission.NodeState;
                request.Allowed = permission.Allowed;
                request.Reason = permission.Reason;

                _store.Add(request);
            }

            Log("MES spawn requests refreshed. Requests: " + _store.Count);
        }

        public bool IsAllowed(string spawnGroupOrEncounterTag)
        {
            return _store.IsAllowed(spawnGroupOrEncounterTag);
        }

        public string BuildSummary()
        {
            return _store.BuildSummary();
        }

        public void Export()
        {
            try
            {
                string xml = MyAPIGateway.Utilities.SerializeToXML(_store);
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ExportFile, typeof(MesSpawnRequestRuntime));
                writer.Write(xml);
                writer.Close();

                Log("MES spawn requests exported. Requests: " + _store.Count);
            }
            catch (Exception e)
            {
                Log("MES spawn request export failed: " + e.Message);
            }
        }

        void Log(string message)
        {
            if (_log != null)
                _log(message);
        }
    }
}
