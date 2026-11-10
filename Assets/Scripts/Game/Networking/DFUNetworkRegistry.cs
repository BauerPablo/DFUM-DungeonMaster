// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Central entity registry for native network synchronization.
    /// Replaces runtime polling (FindObjectsOfType) with deterministic O(1) Dictionary lookups by Network GUID.
    /// </summary>
    public static class DFUNetworkRegistry
    {
        private static readonly Dictionary<Guid, GameObject> entitiesByGuid = new Dictionary<Guid, GameObject>();
        private static readonly Dictionary<ulong, NativeNetworkEntity> entitiesByUlong = new Dictionary<ulong, NativeNetworkEntity>();

        /// <summary>
        /// Registers a spawned network entity.
        /// </summary>
        public static bool RegisterEntity(Guid entityId, GameObject go)
        {
            if (entityId == Guid.Empty || go == null)
                return false;

            entitiesByGuid[entityId] = go;

            NativeNetworkEntity net = go.GetComponent<NativeNetworkEntity>();
            if (net != null && net.EntityId != 0)
            {
                entitiesByUlong[net.EntityId] = net;
            }

            return true;
        }

        /// <summary>
        /// Registers a network entity by numeric ID.
        /// </summary>
        public static bool Register(ulong entityId, NativeNetworkEntity netEnt)
        {
            if (entityId == 0 || netEnt == null)
                return false;

            entitiesByUlong[entityId] = netEnt;
            if (netEnt.entityId != Guid.Empty && netEnt.gameObject != null)
            {
                entitiesByGuid[netEnt.entityId] = netEnt.gameObject;
            }
            return true;
        }

        /// <summary>
        /// Gets a network entity by its numeric EntityId.
        /// </summary>
        public static NativeNetworkEntity Get(ulong entityId)
        {
            if (entityId == 0)
                return null;

            if (entitiesByUlong.TryGetValue(entityId, out NativeNetworkEntity ent) && ent != null)
                return ent;

            return null;
        }

        /// <summary>
        /// Unregisters an entity by its numeric EntityId.
        /// </summary>
        public static bool Unregister(ulong entityId)
        {
            if (entitiesByUlong.TryGetValue(entityId, out NativeNetworkEntity netEnt))
            {
                if (netEnt != null && netEnt.entityId != Guid.Empty)
                {
                    entitiesByGuid.Remove(netEnt.entityId);
                }
                return entitiesByUlong.Remove(entityId);
            }
            return false;
        }

        /// <summary>
        /// Unregisters an entity when destroyed or despawned.
        /// </summary>
        public static bool UnregisterEntity(Guid entityId)
        {
            if (entitiesByGuid.TryGetValue(entityId, out GameObject go))
            {
                if (go != null)
                {
                    NativeNetworkEntity net = go.GetComponent<NativeNetworkEntity>();
                    if (net != null)
                        entitiesByUlong.Remove(net.EntityId);
                }
            }
            return entitiesByGuid.Remove(entityId);
        }

        /// <summary>
        /// Attempts to get an active networked entity by its Network GUID.
        /// </summary>
        public static bool TryGetEntity(Guid entityId, out GameObject go)
        {
            return entitiesByGuid.TryGetValue(entityId, out go);
        }

        /// <summary>
        /// Total number of tracked network entities.
        /// </summary>
        public static int Count => entitiesByGuid.Count;

        /// <summary>
        /// Returns all currently registered GUIDs.
        /// </summary>
        public static IEnumerable<Guid> GetAllGuids() => entitiesByGuid.Keys;

        /// <summary>
        /// Returns all currently registered GUIDs and GameObjects.
        /// </summary>
        public static IEnumerable<KeyValuePair<Guid, GameObject>> GetAllEntities() => entitiesByGuid;

        /// <summary>
        /// Clears all registered entities on session end.
        /// </summary>
        public static void Clear()
        {
            entitiesByGuid.Clear();
            entitiesByUlong.Clear();
        }
    }
}
