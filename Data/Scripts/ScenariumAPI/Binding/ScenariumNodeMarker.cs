using System;

namespace ScenariumAPI.Binding
{
    public class ScenariumNodeMarker
    {
        public string NodeId;
        public string FactionTag;
        public string NodeType;
        public string CaptureMode;
        public string Source;

        public bool IsDestroyMode
        {
            get
            {
                return string.IsNullOrWhiteSpace(CaptureMode) ||
                    CaptureMode.Equals("Destroy", StringComparison.OrdinalIgnoreCase) ||
                    CaptureMode.Equals("Destroyed", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsCaptureMode
        {
            get
            {
                return CaptureMode != null &&
                    (CaptureMode.Equals("Capture", StringComparison.OrdinalIgnoreCase) ||
                     CaptureMode.Equals("Captured", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool TryParse(string text, out ScenariumNodeMarker marker)
        {
            marker = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string nodeId = null;
            string faction = null;
            string nodeType = null;
            string captureMode = null;

            string[] lines = text.Split(new[] { '\n', '\r', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string raw in lines)
            {
                string line = raw.Trim();

                if (line.StartsWith("[SCENARIUM:", StringComparison.OrdinalIgnoreCase) && line.EndsWith("]"))
                {
                    nodeId = line.Substring("[SCENARIUM:".Length);
                    nodeId = nodeId.Substring(0, nodeId.Length - 1).Trim();
                }

                ReadKey(line, "ScenariumNodeId", ref nodeId);
                ReadKey(line, "NodeId", ref nodeId);
                ReadKey(line, "ScenariumFaction", ref faction);
                ReadKey(line, "Faction", ref faction);
                ReadKey(line, "ScenariumNodeType", ref nodeType);
                ReadKey(line, "NodeType", ref nodeType);
                ReadKey(line, "ScenariumCaptureMode", ref captureMode);
                ReadKey(line, "CaptureMode", ref captureMode);
            }

            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            marker = new ScenariumNodeMarker();
            marker.NodeId = nodeId;
            marker.FactionTag = faction;
            marker.NodeType = nodeType;
            marker.CaptureMode = captureMode;
            marker.Source = text;

            return true;
        }

        static void ReadKey(string line, string key, ref string value)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                return;

            int idx = line.IndexOf('=');

            if (idx < 0)
                idx = line.IndexOf(':');

            if (idx < 0 || idx >= line.Length - 1)
                return;

            value = line.Substring(idx + 1).Trim();
        }
    }
}
