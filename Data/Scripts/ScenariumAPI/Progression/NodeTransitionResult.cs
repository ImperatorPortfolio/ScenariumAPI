namespace ScenariumAPI.Progression
{
    public class NodeTransitionResult
    {
        public bool Allowed;
        public bool Forced;
        public string NodeId;
        public string RequestedTransition;
        public string PreviousState;
        public string NewState;
        public string Reason;

        public static NodeTransitionResult Denied(string nodeId, string transition, string previousState, string reason)
        {
            return new NodeTransitionResult
            {
                Allowed = false,
                Forced = false,
                NodeId = nodeId,
                RequestedTransition = transition,
                PreviousState = previousState,
                NewState = previousState,
                Reason = reason
            };
        }

        public static NodeTransitionResult Approved(string nodeId, string transition, string previousState, bool forced)
        {
            return new NodeTransitionResult
            {
                Allowed = true,
                Forced = forced,
                NodeId = nodeId,
                RequestedTransition = transition,
                PreviousState = previousState,
                Reason = forced ? "Admin force override applied." : "Transition approved."
            };
        }
    }
}
