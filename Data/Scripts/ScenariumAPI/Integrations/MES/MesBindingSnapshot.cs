using System.Collections.Generic;

namespace ScenariumAPI.Integrations.MES
{
    public class MesBindingSnapshot
    {
        public List<MesSpawnPermission> Permissions = new List<MesSpawnPermission>();

        public void EnsureCollections()
        {
            if (Permissions == null)
                Permissions = new List<MesSpawnPermission>();
        }
    }
}
