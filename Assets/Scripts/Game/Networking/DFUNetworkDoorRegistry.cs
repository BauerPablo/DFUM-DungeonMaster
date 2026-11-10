using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Registry mapping door LoadIDs and positions to active DaggerfallActionDoor and DaggerfallActionDoorSpecial components in the world.
    /// </summary>
    public static class DFUNetworkDoorRegistry
    {
        private static readonly Dictionary<ulong, DaggerfallActionDoor> doorRegistry = new Dictionary<ulong, DaggerfallActionDoor>();
        private static readonly Dictionary<ulong, DaggerfallActionDoorSpecial> specialRegistry = new Dictionary<ulong, DaggerfallActionDoorSpecial>();

        public static void Register(ulong loadID, DaggerfallActionDoor door)
        {
            if (loadID != 0 && door != null)
            {
                doorRegistry[loadID] = door;
            }
        }

        public static void Unregister(ulong loadID)
        {
            if (loadID != 0)
            {
                doorRegistry.Remove(loadID);
            }
        }

        public static void RegisterSpecial(ulong loadID, DaggerfallActionDoorSpecial door)
        {
            if (loadID != 0 && door != null)
            {
                specialRegistry[loadID] = door;
            }
        }

        public static void UnregisterSpecial(ulong loadID)
        {
            if (loadID != 0)
            {
                specialRegistry.Remove(loadID);
            }
        }

        public static DaggerfallActionDoor FindDoor(ulong loadID, Vector3 position)
        {
            // 1. Direct registry lookup by LoadID
            if (loadID != 0 && doorRegistry.TryGetValue(loadID, out DaggerfallActionDoor cachedDoor) && cachedDoor != null)
            {
                return cachedDoor;
            }

            // 2. Search active action doors from ActiveGameObjectDatabase
            var activeDoors = ActiveGameObjectDatabase.GetActiveActionDoors();
            DaggerfallActionDoor closestDoor = null;
            float closestDistSq = float.MaxValue;

            foreach (DaggerfallActionDoor door in activeDoors)
            {
                if (door == null) continue;

                if (loadID != 0 && door.LoadID == loadID)
                {
                    doorRegistry[loadID] = door;
                    return door;
                }

                float distSq = (door.transform.position - position).sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestDoor = door;
                }
            }

            // If matched within 1.5 meters radius
            if (closestDoor != null && closestDistSq <= 2.25f)
            {
                if (loadID != 0)
                    doorRegistry[loadID] = closestDoor;
                return closestDoor;
            }

            return null;
        }

        public static DaggerfallActionDoorSpecial FindSpecialDoor(ulong loadID, Vector3 position)
        {
            // 1. Direct registry lookup
            if (loadID != 0 && specialRegistry.TryGetValue(loadID, out DaggerfallActionDoorSpecial cachedSpecial) && cachedSpecial != null)
            {
                return cachedSpecial;
            }

            // 2. Search all special doors in scene
            DaggerfallActionDoorSpecial[] allSpecials = Object.FindObjectsOfType<DaggerfallActionDoorSpecial>();
            DaggerfallActionDoorSpecial closestSpecial = null;
            float closestDistSq = float.MaxValue;

            foreach (DaggerfallActionDoorSpecial special in allSpecials)
            {
                if (special == null) continue;

                if (loadID != 0 && special.LoadID == loadID)
                {
                    specialRegistry[loadID] = special;
                    return special;
                }

                float distSq = (special.transform.position - position).sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestSpecial = special;
                }
            }

            if (closestSpecial != null && closestDistSq <= 2.25f)
            {
                if (loadID != 0)
                    specialRegistry[loadID] = closestSpecial;
                return closestSpecial;
            }

            return null;
        }

        public static void Clear()
        {
            doorRegistry.Clear();
            specialRegistry.Clear();
        }
    }
}
