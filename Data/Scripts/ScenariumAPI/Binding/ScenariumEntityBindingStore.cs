using Sandbox.ModAPI;
using VRage.Utils;
using System;
using System.IO;
using System.Collections.Generic;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingStore
    {
        const string SaveFile = "ScenariumAPI_EntityBindings.xml";
        readonly Action<string> _log;

        public ScenariumEntityBindingStore(Action<string> log)
        {
            _log = log;
        }

        public ScenariumEntityBindingSaveData Load()
        {
            try
            {
                ScenariumEntityBindingSaveData data = new ScenariumEntityBindingSaveData();
                data.EnsureCollections();

                if (MyAPIGateway.Utilities == null)
                    return data;

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFile, typeof(ScenariumEntityBindingStore)))
                    return data;

                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SaveFile, typeof(ScenariumEntityBindingStore));
                string xml = reader.ReadToEnd();
                reader.Close();

                if (string.IsNullOrWhiteSpace(xml))
                    return data;

                data = MyAPIGateway.Utilities.SerializeFromXML<ScenariumEntityBindingSaveData>(xml);

                if (data == null)
                    data = new ScenariumEntityBindingSaveData();

                data.EnsureCollections();
                return data;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Scenarium entity binding load failed: " + e);
                if (_log != null)
                    _log("Entity binding load failed: " + e.Message);
                ScenariumEntityBindingSaveData data = new ScenariumEntityBindingSaveData();
                data.EnsureCollections();
                return data;
            }
        }

        public void Save(ScenariumEntityBindingSaveData data)
        {
            try
            {
                if (MyAPIGateway.Utilities == null || data == null)
                    return;

                data.EnsureCollections();
                string xml = MyAPIGateway.Utilities.SerializeToXML(data);
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(SaveFile, typeof(ScenariumEntityBindingStore));
                writer.Write(xml);
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Scenarium entity binding save failed: " + e);
                if (_log != null)
                    _log("Entity binding save failed: " + e.Message);
            }
        }
    }
}
