using System;
using System.Collections.Generic;
using System.Text;

namespace ScenariumAPI.Events
{
    public class ScenariumEventBus
    {
        readonly List<ScenariumEventData> _events = new List<ScenariumEventData>();
        readonly Action<string> _log;
        long _sequence;

        public ScenariumEventBus(Action<string> log)
        {
            _log = log;
        }

        public IList<ScenariumEventData> Events
        {
            get { return _events; }
        }

        public int Count
        {
            get { return _events.Count; }
        }

        public ScenariumEventData Publish(ScenariumEventType type, string subjectId, string message, string previousState, string newState)
        {
            ScenariumEventData data = new ScenariumEventData();
            data.Sequence = ++_sequence;
            data.Type = type;
            data.SubjectId = subjectId;
            data.Message = message;
            data.PreviousState = previousState;
            data.NewState = newState;

            _events.Add(data);

            if (_events.Count > 100)
                _events.RemoveAt(0);

            if (_log != null)
                _log("EVENT " + data.Sequence + " | " + type + " | " + subjectId + " | " + message);

            return data;
        }

        public string BuildRecentSummary(int max)
        {
            StringBuilder sb = new StringBuilder();

            if (_events.Count == 0)
            {
                sb.AppendLine("No Scenarium events recorded.");
                return sb.ToString();
            }

            int start = Math.Max(0, _events.Count - max);

            for (int i = start; i < _events.Count; i++)
            {
                ScenariumEventData e = _events[i];
                sb.AppendLine(e.Sequence + " | " + e.Type + " | " + e.SubjectId + " | " + e.PreviousState + " -> " + e.NewState);
            }

            return sb.ToString();
        }

        public void Clear()
        {
            _events.Clear();
            _sequence = 0;
        }
    }
}
