using System;
using System.Xml.Serialization;

namespace ScenariumAPI.Objectives
{
    public class ObjectiveData
    {
        public string ObjectiveId;
        public string NodeId;
        public string ObjectiveType;
        public string TargetBlockName;
        public string OnCompleteTransition;
        public bool Required;

        public bool IsControlBlockDestroyed
        {
            get { return string.Equals(ObjectiveType, "ControlBlockDestroyed", StringComparison.OrdinalIgnoreCase); }
        }
    }
}
