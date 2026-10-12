using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Registry mapping container LoadIDs to active DaggerfallLoot components in the world.
    /// </summary>
    public static class DFUNetworkLootRegistry
    {
        private static readonly Dictionary<ulong, DaggerfallLoot> registry = new Dictionary<ulong, DaggerfallLoot>();

        public static void Register(ulong loadID, DaggerfallLoot loot)
        {
            if (loadID != 0 && loot != null)
            {
                registry[loadID] = loot;
            }
        }

        public static void Unregister(ulong loadID)
        {
            if (loadID != 0)
            {
                registry.Remove(loadID);
            }
        }

        public static DaggerfallLoot Get(ulong loadID)
        {
            if (loadID == 0) return null;
            registry.TryGetValue(loadID, out DaggerfallLoot loot);
            return loot;
        }

        public static void Clear()
        {
            registry.Clear();
        }
    }
}
