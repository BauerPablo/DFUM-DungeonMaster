// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Message sent from Server to Clients to instantiate an authoritative enemy replica.
    /// </summary>
    public struct MsgSpawnEnemy : NetworkMessage
    {
        public Guid entityId;
        public string entityName;
        public MobileTypes mobileType;
        public MobileGender mobileGender;
        public MobileReactions reaction;
        public Vector3 position;
        public Quaternion rotation;
        public ulong loadID;
        public bool isInside;
        public bool isInsideDungeon;
        public int buildingKey;
        public bool isQuestSpawn;
    }

    /// <summary>
    /// Message sent from Server to Clients when an authoritative entity is despawned or destroyed.
    /// </summary>
    public struct MsgDespawnEnemy : NetworkMessage
    {
        public Guid entityId;
    }

    /// <summary>
    /// Motion update broadcast from Server to Clients for an authoritative entity (e.g. monster).
    /// </summary>
    public struct MsgEntityTransform : NetworkMessage
    {
        public Guid entityId;
        public Vector3 position;
        public float yaw;
        public byte animState; // 0=Idle, 1=Move, 2=Attack, 3=Hurt, 4=Die
    }

    /// <summary>
    /// Profile sent by client or broadcast by server when a player joins.
    /// </summary>
    public struct MsgPlayerProfile : NetworkMessage
    {
        public int connectionId;
        public string playerName;
        public int gender; // 0=Male, 1=Female
        public string raceName;
        public string careerName;
        public Vector3 position;
        public float yaw;
        public bool isInside;
        public bool isInsideDungeon;
        public int buildingKey;
    }

    /// <summary>
    /// High-frequency motion and action update streamed between client and server for a player avatar.
    /// </summary>
    public struct MsgPlayerTransform : NetworkMessage
    {
        public int connectionId;
        public Vector3 position;
        public float yaw;
        public bool isMoving;
        public bool isCrouching;
        public bool isAttacking;
        public int weaponType;
        public bool isInside;
        public bool isInsideDungeon;
        public int buildingKey;
        public bool isInvisible;
    }

    /// <summary>
    /// Message broadcast between server and clients to coordinate party dungeon and interior cell transitions.
    /// </summary>
    public struct MsgPlayerTransition : NetworkMessage
    {
        public int connectionId;
        public int transitionType;
        public int buildingKey;
        public int mapPixelX;
        public int mapPixelY;
        public Vector3 playerPosition;
        public float playerYaw;
        public bool isGatherParty;
    }

    /// <summary>
    /// Message sent when a connected player leaves or disconnects.
    /// </summary>
    public struct MsgPlayerDespawn : NetworkMessage
    {
        public int connectionId;
    }

    /// <summary>
    /// Message sent from server to client with its assigned unique connectionId.
    /// </summary>
    public struct MsgPlayerInit : NetworkMessage
    {
        public int assignedConnectionId;
    }

    /// <summary>
    /// Sent from Client to Server when client attacks an authoritative entity.
    /// </summary>
    public struct MsgAttackEntity : NetworkMessage
    {
        public ulong entityId;
        public Vector3 hitPosition;
        public Vector3 hitDirection;
        public int weaponType;
        public int weaponMaterial;
        public int animTimeMs;
        public bool isEnemyFacingAway;
        public int damage;
    }

    /// <summary>
    /// Broadcast from Server to Clients when an entity takes authoritative damage.
    /// </summary>
    public struct MsgEntityDamaged : NetworkMessage
    {
        public ulong entityId;
        public int damage;
        public Vector3 hitPosition;
        public int bloodIndex;
    }

    /// <summary>
    /// Broadcast from Server to Clients when an entity dies authoritatively.
    /// </summary>
    public struct MsgEntityDeath : NetworkMessage
    {
        public ulong entityId;
        public int corpseTexture;
        public Vector3 position;
        public float yaw;
        public ulong lootLoadID;
        public string itemsJson;
    }

    /// <summary>
    /// Sent from Client to Server to request authoritative item contents for a loot container.
    /// </summary>
    public struct MsgRequestLootSync : NetworkMessage
    {
        public ulong loadID;
    }

    /// <summary>
    /// Broadcast from Server to Clients with authoritative item contents for a loot container.
    /// </summary>
    public struct MsgLootSync : NetworkMessage
    {
        public ulong loadID;
        public string itemsJson;
    }

    /// <summary>
    /// Sent from Client to Server when player takes an item from a loot container.
    /// </summary>
    public struct MsgLootTakeItem : NetworkMessage
    {
        public ulong loadID;
        public ulong itemUID;
        public int itemGroup;
        public int templateIndex;
        public int stackCount;
    }

    /// <summary>
    /// Sent from Client to Server when player places an item into a loot container.
    /// </summary>
    public struct MsgLootDropItem : NetworkMessage
    {
        public ulong loadID;
        public string itemJson;
    }

    /// <summary>
    /// Broadcast from Server to Clients when a loot container (corpse, chest, treasure) is spawned.
    /// </summary>
    public struct MsgSpawnLoot : NetworkMessage
    {
        public ulong loadID;
        public int containerType;
        public int containerImage;
        public Vector3 position;
        public int textureArchive;
        public int textureRecord;
        public string entityName;
        public bool isEnemyClass;
        public string itemsJson;
    }

    /// <summary>
    /// Broadcast from Server to Clients when a loot container is despawned/destroyed.
    /// </summary>
    public struct MsgDespawnLoot : NetworkMessage
    {
        public ulong loadID;
    }


    /// <summary>
    /// Synchronizes authoritative world time and timescale from server to clients.
    /// </summary>
    public struct MsgWorldTimeSync : NetworkMessage
    {
        public ulong timeInSeconds;
        public float timeScale;
        public bool isImmediate;
    }

    /// <summary>
    /// Synchronizes authoritative weather state from server to clients.
    /// </summary>
    public struct MsgWeatherSync : NetworkMessage
    {
        public byte weatherType;
        public bool isImmediate;
    }

    /// <summary>
    /// Types of party decisions/votes.
    /// </summary>
    public enum PartyVoteType : byte
    {
        Rest = 0,
        RestUntilHealed = 1,
        Loiter = 2,
        FastTravel = 3,
        QuestOffer = 4,
        Custom = 5
    }

    /// <summary>
    /// Consensus resolution policies for party votes.
    /// </summary>
    public enum VoteConsensusPolicy : byte
    {
        Unanimous = 0, // Requires 100% Yes votes; any No declines immediately
        OptIn = 1,     // Non-blocking; executes only for Yes voters upon completion
        Majority = 2   // Requires > 50% Yes votes to pass
    }

    /// <summary>
    /// Generic request from Client to Server to initiate a party vote.
    /// </summary>
    public struct MsgPartyVoteRequest : NetworkMessage
    {
        public byte voteType;
        public byte policy;
        public string initiatorName;
        public string title;
        public string details;
        public int intParam1;
        public int intParam2;
        public bool boolParam1;
        public bool boolParam2;
        public string stringParam;
    }

    /// <summary>
    /// Broadcast from Server to Clients with live party voting state and checklist.
    /// </summary>
    public struct MsgPartyVoteUpdate : NetworkMessage
    {
        public byte voteType;
        public byte policy;
        public string initiatorName;
        public int initiatorConnId;
        public string title;
        public string details;
        public int approvedCount;
        public int totalMembers;
        public float remainingSeconds;
        public string voterStatuses; // "PlayerName:Status,PlayerName2:Status"
        public int requiredGold;
    }

    /// <summary>
    /// Generic vote response from Client to Server.
    /// </summary>
    public struct MsgPartyVoteResponse : NetworkMessage
    {
        public bool approved;
    }

    /// <summary>
    /// Broadcast from Server to Clients with final party vote result.
    /// </summary>
    public struct MsgPartyVoteResult : NetworkMessage
    {
        public byte voteType;
        public bool approved;
        public string message;
        public int intParam1;
        public int intParam2;
        public bool boolParam1;
        public string stringParam;
    }

    /// <summary>
    /// Broadcast from Server to Clients executing synchronized regional party fast travel.
    /// </summary>
    public struct MsgPartyTravelExecute : NetworkMessage
    {
        public int mapPixelX;
        public int mapPixelY;
        public int travelTimeMins;
        public bool speedCautious;
        public string destinationName;
        public int totalCost;
        public int initiatorConnId;
    }

    /// <summary>
    /// Sent from Client to Server to request party rest/loiter consensus (legacy rest wrapper).
    /// </summary>
    public struct MsgPartyRestVoteRequest : NetworkMessage
    {
        public string initiatorName;
        public int hours;
        public bool isLoiter;
    }

    /// <summary>
    /// Sent from Server to Clients to prompt for party rest/loiter vote.
    /// </summary>
    public struct MsgPartyRestVotePrompt : NetworkMessage
    {
        public string initiatorName;
        public int hours;
        public bool isLoiter;
        public float timeoutSeconds;
    }

    /// <summary>
    /// Sent from Client to Server with vote response.
    /// </summary>
    public struct MsgPartyRestVoteResponse : NetworkMessage
    {
        public bool approved;
    }

    /// <summary>
    /// Broadcast from Server to Clients with live party rest voting state and checklist.
    /// </summary>
    public struct MsgPartyRestVoteUpdate : NetworkMessage
    {
        public string initiatorName;
        public int initiatorConnId;
        public int hours;
        public bool isLoiter;
        public int approvedCount;
        public int totalMembers;
        public float remainingSeconds;
        public string voterStatuses; // Comma-separated: "PlayerName:Status,PlayerName2:Status" (Status: Yes, Pending, No)
    }

    /// <summary>
    /// Sent from Client to Server when player commits a crime, requesting authoritative guards.
    /// </summary>
    public struct MsgPlayerCrime : NetworkMessage
    {
        public int crimeType;
        public Vector3 playerPosition;
    }

    /// <summary>
    /// Broadcast from Server to Clients with final vote result.
    /// </summary>
    public struct MsgPartyRestVoteResult : NetworkMessage
    {
        public bool approved;
        public string message;
        public int hours;
        public bool isLoiter;
    }

    /// <summary>
    /// Synchronizes party rest notification from host to clients.
    /// </summary>
    public struct MsgPartyRestNotification : NetworkMessage
    {
        public bool isResting;
        public int hours;
        public bool wasInterrupted;
        public bool isLoiter;
    }

    /// <summary>
    /// Message sent from Server to approved Clients to start a synchronized party quest.
    /// </summary>
    public struct MsgPartyQuestStart : NetworkMessage
    {
        public string questName;
        public int factionId;
        public int questorNameSeed;
        public int questorFactionId;
        public int questSeed;
    }

    /// <summary>
    /// Synchronizes overhead dialogue speech bubble (NPC or Player) for multiplayer eavesdropping.
    /// </summary>
    public struct MsgDialogueSpeech : NetworkMessage
    {
        public int speakerType;         // 0 = Player, 1 = NPC
        public int playerConnectionId;  // Connection ID of the player
        public string speakerName;      // Name of the speaker (Player or NPC)
        public Vector3 worldPosition;   // World position of speaker
        public string text;             // Spoken dialogue line
        public float lingerSeconds;     // Duration in seconds before fadeout
    }

    /// <summary>
    /// Notifies peers that a dialogue session ended, initiating the fadeout countdown for speech bubbles.
    /// </summary>
    public struct MsgDialogueEnded : NetworkMessage
    {
        public int playerConnectionId;
        public string npcName;
    }

    /// <summary>
    /// Message sent from Server to a Client to apply authoritative damage from an enemy or hazard.
    /// </summary>
    public struct MsgPlayerDamaged : NetworkMessage
    {
        public int connectionId;
        public int damage;
    }

    /// <summary>
    /// Broadcast from Server/Host to Clients when a quest is completed or canceled.
    /// </summary>
    public struct MsgPartyQuestComplete : NetworkMessage
    {
        public string questName;
        public int factionId;
        public bool success;
    }

    /// <summary>
    /// Synchronizes a faction reputation change from Host to Clients.
    /// </summary>
    public struct MsgFactionRepSync : NetworkMessage
    {
        public int targetConnectionId; // -1 for all party, or specific connectionId
        public int factionId;
        public int repValue;
        public int delta;
        public string factionName;
    }

    /// <summary>
    /// Sent from Host to Clients to request their character's reputation profile snapshot.
    /// </summary>
    public struct MsgRequestReputationReport : NetworkMessage
    {
    }

    /// <summary>
    /// Sent from Client to Host with full snapshot of character's guild reputations.
    /// </summary>
    public struct MsgClientReputationReport : NetworkMessage
    {
        public int connectionId;
        public string playerName;
        public string reputationsJson;
    }

    /// <summary>
    /// Serialized pair of faction ID and current reputation value.
    /// </summary>
    [Serializable]
    public struct FactionRepPair
    {
        public int factionId;
        public int rep;
    }

    /// <summary>
    /// Snapshot bundle of key faction reputations for a character.
    /// </summary>
    [Serializable]
    public class FactionRepSnapshot
    {
        public List<FactionRepPair> items = new List<FactionRepPair>();
    }

    /// <summary>
    /// Sent between peers to share map exploration and discovered locations/buildings.
    /// </summary>
    public struct MsgShareMapData : NetworkMessage
    {
        public int senderConnectionId;
        public string senderName;
        public int targetConnectionId;
        public string discoveryDataJson;
        public int locationCount;
        public int buildingCount;
    }

    /// <summary>
    /// Lightweight DTO for serializing building discoveries over the network.
    /// </summary>
    [Serializable]
    public class SharedBuildingItem
    {
        public int buildingKey;
        public string displayName;
        public string oldDisplayName;
        public bool isOverrideName;
        public int factionID;
        public int quality;
        public int buildingType;
    }

    /// <summary>
    /// Lightweight DTO for serializing location and town discoveries over the network.
    /// </summary>
    [Serializable]
    public class SharedLocationItem
    {
        public int mapID;
        public int mapPixelID;
        public string regionName;
        public string locationName;
        public List<SharedBuildingItem> buildings = new List<SharedBuildingItem>();
    }

    /// <summary>
    /// Container bundle for all discovered locations and buildings shared between players.
    /// </summary>
    [Serializable]
    public class SharedMapBundle
    {
        public List<SharedLocationItem> locations = new List<SharedLocationItem>();
    }

    #region Bilateral Trading System

    /// <summary>
    /// Lightweight DTO for previewing an offered item in the bilateral trade window.
    /// </summary>
    [Serializable]
    public class TradeItemDTO
    {
        public ulong uid;
        public string name;
        public float weight;
        public int stackCount;
        public int value;
    }

    /// <summary>
    /// Serialized bundle of offered items for visual synchronization during trade.
    /// </summary>
    [Serializable]
    public class TradeOfferBundle
    {
        public List<TradeItemDTO> items = new List<TradeItemDTO>();
    }

    /// <summary>
    /// Sent to invite a partner to a trade session.
    /// </summary>
    public struct MsgTradeInvite : NetworkMessage
    {
        public int senderConnectionId;
        public string senderName;
        public int targetConnectionId;
    }

    /// <summary>
    /// Response to a trade invite (accept / decline).
    /// </summary>
    public struct MsgTradeInviteResponse : NetworkMessage
    {
        public int senderConnectionId;
        public string senderName;
        public int targetConnectionId;
        public bool accepted;
    }

    /// <summary>
    /// Synchronizes changes to a player's trade offer (gold, items preview, locked, accepted).
    /// </summary>
    public struct MsgTradeStateSync : NetworkMessage
    {
        public int senderConnectionId;
        public int targetConnectionId;
        public int gold;
        public string offerItemsJson; // Serialized TradeOfferBundle
        public bool isLocked;
        public bool isAccepted;
    }

    /// <summary>
    /// Sent when either player or server cancels the trade.
    /// </summary>
    public struct MsgTradeCancel : NetworkMessage
    {
        public int senderConnectionId;
        public int targetConnectionId;
        public string reason;
    }

    /// <summary>
    /// Sent by players to server upon confirming trade with full serialized items.
    /// </summary>
    public struct MsgTradeCommit : NetworkMessage
    {
        public int senderConnectionId;
        public int targetConnectionId;
        public int offeredGold;
        public string offeredItemsJson; // Serialized ItemData_v1[]
    }

    /// <summary>
    /// Sent by server to both players when trade executes or fails.
    /// </summary>
    public struct MsgTradeCompleted : NetworkMessage
    {
        public bool success;
        public string message;
        public string receivedItemsJson; // Serialized ItemData_v1[] of items transferred to this player
        public int receivedGold; // Gold transferred to this player
    }

    #region Town Population Sync

    /// <summary>
    /// Synchronized state of a single civilian wandering NPC in a town or city.
    /// </summary>
    [Serializable]
    public struct CivilianStateDTO
    {
        public int poolIndex;
        public bool active;
        public byte race;
        public byte gender;
        public byte outfitVariant;
        public int faceRecordId;
        public bool isGuard;
        public string nameNPC;
        public Vector3 position;
        public bool isIdle;
        public int moveCount;
    }

    /// <summary>
    /// Broadcast from Server to Clients in the same location to synchronize wandering town civilians.
    /// </summary>
    public struct MsgPopulationSync : NetworkMessage
    {
        public int mapID;
        public CivilianStateDTO[] civilians;
    }

    #endregion

    #endregion
}



