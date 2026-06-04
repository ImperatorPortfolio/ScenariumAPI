using System;

namespace ScenariumAPI.Events
{
    public enum ScenariumEventType
    {
        Unknown = 0,
        CampaignLoaded = 10,
        RuntimeReset = 20,
        NodeDestroyed = 100,
        NodeCaptured = 101,
        NodeRevealed = 102,
        FactionDefeated = 200,
        MesPermissionsRefreshed = 300,
        ValidationCompleted = 400
    }

    public class ScenariumEventData
    {
        public long Sequence;
        public string TimestampUtc;
        public ScenariumEventType Type;
        public string SubjectId;
        public string Message;
        public string PreviousState;
        public string NewState;

        public ScenariumEventData()
        {
            TimestampUtc = DateTime.UtcNow.ToString("o");
        }
    }
}
