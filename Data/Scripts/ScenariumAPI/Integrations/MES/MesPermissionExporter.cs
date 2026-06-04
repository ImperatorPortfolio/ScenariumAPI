using Sandbox.ModAPI;
using VRage.Utils;
using System;
using System.IO;

namespace ScenariumAPI.Integrations.MES
{
    public class MesPermissionExporter
    {
        const string ExportFile = "ScenariumAPI_MESPermissions.xml";

        readonly Action<string> _log;

        public MesPermissionExporter(Action<string> log)
        {
            _log = log;
        }

        public void Export(MesBindingSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    _log("MES permission export skipped: snapshot is null.");
                    return;
                }

                snapshot.EnsureCollections();

                string xml = MyAPIGateway.Utilities.SerializeToXML(snapshot);
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ExportFile, typeof(MesPermissionExporter));
                writer.Write(xml);
                writer.Close();

                _log("MES permission export written. Entries: " + snapshot.Permissions.Count);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Scenarium MES permission export failed: " + e);
                _log("MES permission export failed: " + e.Message);
            }
        }
    }
}
