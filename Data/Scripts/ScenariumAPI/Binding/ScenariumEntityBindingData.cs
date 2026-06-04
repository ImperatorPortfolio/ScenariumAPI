using System;
using System.Collections.Generic;

namespace ScenariumAPI.Binding
{
    public class ScenariumEntityBindingData
    {
        public long EntityId;
        public string GridName;
        public string NodeId;
        public string FactionTag;
        public string BindingKey;
        public string BindingValue;
        public string CaptureMode;
        public bool TransitionApplied;
    }

    public class ScenariumEntityBindingSaveData
    {
        public List<ScenariumEntityBindingData> Bindings = new List<ScenariumEntityBindingData>();

        public void EnsureCollections()
        {
            if (Bindings == null)
                Bindings = new List<ScenariumEntityBindingData>();
        }
    }
}
