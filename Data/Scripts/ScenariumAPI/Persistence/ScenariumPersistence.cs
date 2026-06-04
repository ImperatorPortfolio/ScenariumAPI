using Sandbox.ModAPI;
using VRage.Utils;
using System;
using System.IO;
using ScenariumAPI.Data;

namespace ScenariumAPI.Persistence
{
    public class ScenariumPersistence
    {
        const string RuntimeSaveFile = "ScenariumAPI_RuntimeState.xml";

        public CampaignRuntimeStateData LoadRuntimeState()
        {
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return null;

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(RuntimeSaveFile, typeof(ScenariumPersistence)))
                    return null;

                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(RuntimeSaveFile, typeof(ScenariumPersistence));
                string xml = reader.ReadToEnd();
                reader.Close();

                if (string.IsNullOrWhiteSpace(xml))
                    return null;

                CampaignRuntimeStateData state = MyAPIGateway.Utilities.SerializeFromXML<CampaignRuntimeStateData>(xml);

                if (state != null)
                    state.EnsureCollections();

                return state;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Scenarium runtime load failed: " + e);
                return null;
            }
        }

        public void SaveRuntimeState(CampaignRuntimeStateData state)
        {
            try
            {
                if (MyAPIGateway.Utilities == null || state == null)
                    return;

                state.EnsureCollections();

                string xml = MyAPIGateway.Utilities.SerializeToXML(state);
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(RuntimeSaveFile, typeof(ScenariumPersistence));
                writer.Write(xml);
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Scenarium runtime save failed: " + e);
            }
        }
    }
}
