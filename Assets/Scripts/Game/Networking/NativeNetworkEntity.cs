// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using UnityEngine;
using Mirror;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Component identifying a networked entity in Daggerfall Unity.
    /// Replaces mod-based EntitySyncMarker with deterministic GUID registration in DFUNetworkRegistry.
    /// </summary>
    public class NativeNetworkEntity : MonoBehaviour
    {
        [Header("Network Identity")]
        public string entityIdString;
        public Guid entityId;
        public bool isClientProxy = false;

        [Header("Entity Metadata")]
        public string entityName;
        public MobileTypes mobileType;
        public MobileGender mobileGender;
        public MobileReactions reaction;
        public ulong loadID;

        [Header("Cell Context")]
        public bool isInside = false;
        public bool isInsideDungeon = false;
        public int buildingKey = 0;
        public bool isQuestSpawn = false;

        /// <summary>
        /// Unique numeric identifier for network combat messages.
        /// </summary>
        public ulong EntityId
        {
            get
            {
                if (loadID != 0)
                    return loadID;
                if (entityId != Guid.Empty)
                {
                    byte[] bytes = entityId.ToByteArray();
                    return BitConverter.ToUInt64(bytes, 0) ^ BitConverter.ToUInt64(bytes, 8);
                }
                return 0;
            }
            set
            {
                loadID = value;
            }
        }

        private void Awake()
        {
            if (entityId == Guid.Empty && NetworkSession.IsServer)
            {
                entityId = Guid.NewGuid();
                entityIdString = entityId.ToString();
            }
        }

        private void Start()
        {
            if (entityId != Guid.Empty)
            {
                DFUNetworkRegistry.RegisterEntity(entityId, gameObject);
            }
        }

        private void OnDestroy()
        {
            if (entityId != Guid.Empty)
            {
                DFUNetworkRegistry.UnregisterEntity(entityId);

                // If running on authoritative server, broadcast despawn message
                if (NetworkSession.IsServer && NetworkServer.active)
                {
                    NetworkServer.SendToAll(new MsgDespawnEnemy { entityId = entityId });
                }
            }
        }

        /// <summary>
        /// Initializes the identity and metadata from server instantiation parameters.
        /// </summary>
        public void Initialize(Guid id, string name, MobileTypes type, MobileGender gender, MobileReactions react, ulong loadId, bool proxy, bool inside = false, bool dungeon = false, int bKey = 0, bool quest = false)
        {
            entityId = id;
            entityIdString = id.ToString();
            entityName = name;
            mobileType = type;
            mobileGender = gender;
            reaction = react;
            loadID = loadId;
            isClientProxy = proxy;
            isInside = inside;
            isInsideDungeon = dungeon;
            buildingKey = bKey;
            isQuestSpawn = quest;

            DFUNetworkRegistry.RegisterEntity(entityId, gameObject);
        }

        /// <summary>
        /// Constructs a spawn packet suitable for sending across the network.
        /// </summary>
        public MsgSpawnEnemy ToSpawnMessage()
        {
            PlayerEnterExit pee = GameManager.Instance != null ? GameManager.Instance.PlayerEnterExit : null;
            bool inside = pee != null && pee.IsPlayerInside;
            bool insideDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int bKey = (pee != null && pee.Interior != null) ? pee.Interior.EntryDoor.buildingKey : 0;
            DaggerfallEnemy dfEnemy = GetComponent<DaggerfallEnemy>();
            bool quest = (dfEnemy != null && dfEnemy.QuestSpawn) || isQuestSpawn;

            return new MsgSpawnEnemy
            {
                entityId = entityId,
                entityName = string.IsNullOrEmpty(entityName) ? gameObject.name : entityName,
                mobileType = mobileType,
                mobileGender = mobileGender,
                reaction = reaction,
                position = transform.position,
                rotation = transform.rotation,
                loadID = loadID,
                isInside = inside,
                isInsideDungeon = insideDungeon,
                buildingKey = bKey,
                isQuestSpawn = quest
            };
        }
    }
}
