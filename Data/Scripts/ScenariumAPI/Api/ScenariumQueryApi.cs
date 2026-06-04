using System;
using ScenariumAPI.Data;
using ScenariumAPI.Runtime;

namespace ScenariumAPI.Api
{
    public class ScenariumQueryApi
    {
        CampaignRuntime _runtime;

        public ScenariumQueryApi(CampaignRuntime runtime)
        {
            _runtime = runtime;
        }

        public void SetRuntime(CampaignRuntime runtime)
        {
            _runtime = runtime;
        }

        public bool IsCampaignLoaded()
        {
            return _runtime != null && _runtime.Campaign != null && _runtime.State != null;
        }

        public string GetCampaignId()
        {
            if (!IsCampaignLoaded())
                return null;

            return _runtime.State.CampaignId;
        }

        public ScenariumFactionStateType GetFactionState(string factionTag)
        {
            if (!IsCampaignLoaded() || string.IsNullOrWhiteSpace(factionTag))
                return ScenariumFactionStateType.Unknown;

            foreach (var faction in _runtime.State.Factions)
            {
                if (string.Equals(faction.Tag, factionTag, StringComparison.OrdinalIgnoreCase))
                    return faction.State;
            }

            return ScenariumFactionStateType.Unknown;
        }

        public bool IsFactionDefeated(string factionTag)
        {
            if (!IsCampaignLoaded() || string.IsNullOrWhiteSpace(factionTag))
                return false;

            foreach (var faction in _runtime.State.Factions)
            {
                if (string.Equals(faction.Tag, factionTag, StringComparison.OrdinalIgnoreCase))
                    return faction.Defeated || faction.State == ScenariumFactionStateType.Defeated;
            }

            return false;
        }

        public ScenariumConquestNodeState GetNodeState(string nodeId)
        {
            if (!IsCampaignLoaded() || string.IsNullOrWhiteSpace(nodeId))
                return ScenariumConquestNodeState.Hidden;

            var state = _runtime.GetNodeState(nodeId);

            if (state == null)
                return ScenariumConquestNodeState.Hidden;

            return state.State;
        }

        public bool CanFactionSpawn(string factionTag, string sectorId)
        {
            if (!IsCampaignLoaded())
                return false;

            if (IsFactionDefeated(factionTag))
                return false;

            ScenariumFactionStateType state = GetFactionState(factionTag);

            if (state == ScenariumFactionStateType.Disabled || state == ScenariumFactionStateType.Defeated)
                return false;

            return true;
        }
    }
}
