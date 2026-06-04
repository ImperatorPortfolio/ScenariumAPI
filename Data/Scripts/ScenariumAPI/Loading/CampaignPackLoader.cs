using Sandbox.ModAPI;
using Sandbox.Game.World;
using VRage.Game;
using System;
using System.IO;
using ScenariumAPI.Data;

namespace ScenariumAPI.Loading
{
    public class CampaignPackLoader
    {
        const string CampaignPath = "Data/Scenarium/Campaign.xml";

        public CampaignData LoadedCampaign { get; private set; }
        public string LastError { get; private set; }

        public bool TryLoad(out CampaignData campaign)
        {
            campaign = null;
            LastError = null;

            try
            {
                if (MyAPIGateway.Session == null)
                {
                    LastError = "Session is not available.";
                    return false;
                }

                if (MyAPIGateway.Session.Mods == null)
                {
                    LastError = "Session mod list is not available.";
                    return false;
                }

                foreach (MyObjectBuilder_Checkpoint.ModItem mod in MyAPIGateway.Session.Mods)
                {
                    TextReader reader = null;

                    try
                    {
                        reader = MyAPIGateway.Utilities.ReadFileInModLocation(CampaignPath, mod);
                    }
                    catch
                    {
                        reader = null;
                    }

                    if (reader == null)
                        continue;

                    string xml = reader.ReadToEnd();
                    reader.Close();

                    if (string.IsNullOrWhiteSpace(xml))
                    {
                        LastError = "Campaign file is empty in mod: " + mod.FriendlyName;
                        continue;
                    }

                    campaign = MyAPIGateway.Utilities.SerializeFromXML<CampaignData>(xml);

                    if (campaign == null)
                    {
                        LastError = "Campaign XML deserialized to null in mod: " + mod.FriendlyName;
                        continue;
                    }

                    campaign.EnsureCollections();

                    foreach (var s in campaign.Scenarios) if (s != null) s.EnsureCollections();
                    foreach (var f in campaign.Factions) if (f != null) f.EnsureCollections();
                    foreach (var q in campaign.Quests) if (q != null) q.EnsureCollections();
                    foreach (var n in campaign.ConquestNodes) if (n != null) n.EnsureCollections();
                    foreach (var r in campaign.Rewards) if (r != null) r.EnsureCollections();

                    LoadedCampaign = campaign;
                    LastError = null;
                    return true;
                }

                LastError = "No loaded mod contains " + CampaignPath;
                return false;
            }
            catch (Exception e)
            {
                LastError = e.ToString();
                return false;
            }
        }
    }
}
