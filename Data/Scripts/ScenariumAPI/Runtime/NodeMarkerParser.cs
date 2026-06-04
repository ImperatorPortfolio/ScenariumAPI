using System;

namespace ScenariumAPI.Runtime
{
    public class NodeMarkerData
    {
        public string NodeId;
        public string FactionTag;
        public string NodeType;
        public string Source;
    }

    public static class NodeMarkerParser
    {
        public static bool TryParse(string text, out NodeMarkerData marker)
        {
            marker = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string nodeId = null;
            string faction = null;
            string nodeType = null;

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
            }

            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            marker = new NodeMarkerData
            {
                NodeId = nodeId,
                FactionTag = faction,
                NodeType = nodeType,
                Source = text
            };

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
