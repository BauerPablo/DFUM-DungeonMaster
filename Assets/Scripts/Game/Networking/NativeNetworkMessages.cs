// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop.Game.MagicAndEffects;

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
        public bool isDm;
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
    /// Broadcast between server and clients to synchronize hinged action door state (interior and dungeon doors).
    /// </summary>
    public struct MsgDoorToggle : NetworkMessage
    {
        public ulong loadID;
        public Vector3 position;
        public bool isOpen;
        public bool instant;
    }

    /// <summary>
    /// Sent by client when entering an interior or dungeon, requesting authoritative door states from server.
    /// </summary>
    public struct MsgRequestDoorSync : NetworkMessage
    {
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
    /// Dual entity operational state for Dungeon Masters.
    /// PhysicalAvatar: Visible physical presence, active collider, golden overhead billboard nametag.
    /// SpectatorSpirit: Intangible Zeus 6-DOF flight, true invisibility, no mob aggro, infinite vitals/godmode.
    /// </summary>
    public enum DMEntityMode : byte
    {
        PhysicalAvatar = 0,
        SpectatorSpirit = 1
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
        public int civilianPoolIndex;
        public int mapID;
        public bool isInside;
        public bool isInsideDungeon;
        public int buildingKey;
    }

    /// <summary>
    /// Sent from Server to Client when a City Watch guard engages a criminal player, prompting surrender.
    /// </summary>
    public struct MsgGuardArrestPrompt : NetworkMessage
    {
        public int guardEntityId;
    }

    /// <summary>
    /// Sent from Client to Server responding to guard surrender prompt or completion of court.
    /// </summary>
    public struct MsgPlayerSurrender : NetworkMessage
    {
        public bool surrendered;
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
        public float yaw;
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

    #region Magic & PvP Sync

    /// <summary>
    /// Broadcast from Server to Clients when a player casts a spell, for replicating visual missiles, audio, and particles.
    /// </summary>
    public struct MsgCastSpell : NetworkMessage
    {
        public int casterConnectionId;
        public string casterName;
        public string spellName;
        public ElementTypes elementType;
        public TargetTypes targetType;
        public Vector3 origin;
        public Vector3 direction;
    }

    /// <summary>
    /// Routed between players (or via Server) to deliver a supportive or offensive spell bundle payload.
    /// </summary>
    public struct MsgApplySpellBundle : NetworkMessage
    {
        public int targetConnectionId;
        public int casterConnectionId;
        public string casterName;
        public string spellName;
        public ElementTypes elementType;
        public string bundleSettingsJson;
        public bool isHarmful;
    }

    /// <summary>
    /// Synchronizes PvP friendly fire toggle state from Server to all Clients.
    /// </summary>
    public struct MsgPvpStateSync : NetworkMessage
    {
        public bool isPvpAllowed;
    }

    /// <summary>
    /// Broadcast when a player or enemy shoots an arrow, replicating visual arrow flight on peers.
    /// </summary>
    public struct MsgArrowShot : NetworkMessage
    {
        public int shooterConnectionId;
        public Vector3 origin;
        public Vector3 direction;
        public int arrowType;
    }

    /// <summary>
    /// Sent by the DM/Host to teach a spell to a remote player.
    /// </summary>
    public struct MsgTeachSpell : NetworkMessage
    {
        public int targetConnectionId;
        public string spellBundleJson;
    }

    /// <summary>
    /// Sent by Server/DM to all clients to trigger an authoritative screen shake and thunder roll.
    /// </summary>
    public struct MsgEarthquake : NetworkMessage
    {
        public float duration;
        public float magnitude;
    }

    /// <summary>
    /// Sent by Server/DM to all clients to play an environmental ambient sound.
    /// </summary>
    public struct MsgPlayWorldSound : NetworkMessage
    {
        public int soundClip;
    }

    /// <summary>
    /// Sent by Server/DM to a player (or all with -1) to grant minted gold pieces.
    /// </summary>
    public struct MsgGrantGold : NetworkMessage
    {
        public int targetConnectionId;
        public int amount;
    }

    /// <summary>
    /// Sent by Server/DM to a player (or all with -1) to pardon crimes and restore legal reputation.
    /// </summary>
    public struct MsgPardonCrimes : NetworkMessage
    {
        public int targetConnectionId;
    }

    /// <summary>
    /// Sent by Server/DM to grant a custom forged weapon or armor directly to a player's inventory.
    /// </summary>
    public struct MsgGrantForgedItem : NetworkMessage
    {
        public int targetConnectionId;
        public bool isWeapon;
        public int itemType;
        public int materialType;
    }

    /// <summary>
    /// Phase 17: Multi-channel chat message packet.
    /// Channels: 0=Say, 1=Party, 2=Whisper, 3=DM, 4=OOC, 5=Roll, 6=System.
    /// </summary>
    public struct MsgChatMessage : NetworkMessage
    {
        public byte channel;
        public string senderName;
        public int senderConnectionId;
        public int targetConnectionId;
        public string text;
        public byte languageId;
        public Vector3 worldPosition;
    }

    /// <summary>
    /// Phase 17: DM initiates a Baldur's Gate 3-style DC Skill Challenge.
    /// </summary>
    public struct MsgDiceChallenge : NetworkMessage
    {
        public int challengeId;
        public string challengeTitle;
        public int targetDC;
        public bool showDC;
    }

    /// <summary>
    /// Phase 17: Client submits their roll for an active DC Challenge or free roll.
    /// </summary>
    public struct MsgSubmitDiceRoll : NetworkMessage
    {
        public int challengeId;
        public int rollResult;
        public string rollerName;
        public int rollerConnectionId;
    }

    /// <summary>
    /// Phase 17: Broadcast of resolved DC challenge outcome or free roll to all clients.
    /// </summary>
    public struct MsgDiceResultBroadcast : NetworkMessage
    {
        public int challengeId;
        public string challengeTitle;
        public string rollerName;
        public int rollerConnectionId;
        public int rollResult;
        public int targetDC;
        public bool showDC;
        public bool isCriticalSuccess;
        public bool isCriticalFail;
        public bool passed;
    }

    #endregion

    #region Multiplayer Pre-Game Lobby & Mod Verification

    /// <summary>
    /// Represents a single player's state inside the pre-game Multiplayer Lobby.
    /// </summary>
    [Serializable]
    public class LobbyPlayerDTO
    {
        public int connectionId;
        public string playerName;
        public bool isHost;
        public bool isDm;
        public int selectionMode; // 0 = None, 1 = New Character, 2 = Saved Character
        public int saveKey;       // Local save folder key
        public string characterName;
        public string raceName;
        public string className;
        public int level;
        public bool isReady;
        public bool modsCompatible;
        public string modMismatchSummary;
        public string[] activeMods;
    }

    /// <summary>
    /// Serializable container for broadcasting the full lobby roster and host mod list.
    /// </summary>
    [Serializable]
    public class LobbyRosterBundle
    {
        public int sessionMode; // 0 = Undecided/Mixed, 1 = New Game, 2 = Load Game
        public string[] hostRequiredMods;
        public List<LobbyPlayerDTO> players = new List<LobbyPlayerDTO>();
    }

    /// <summary>
    /// Sent from Client to Server whenever the client updates its lobby character selection, role, mod list, or ready state.
    /// </summary>
    public struct MsgLobbyClientUpdate : NetworkMessage
    {
        public string playerName;
        public bool isDm;
        public int selectionMode; // 0 = None, 1 = New Character, 2 = Saved Character
        public int saveKey;
        public string characterName;
        public string raceName;
        public string className;
        public int level;
        public bool isReady;
        public string activeModsJson; // Pipe-separated or JSON array of enabled .dfmod filenames
    }

    /// <summary>
    /// Broadcast from Server to all Clients whenever the lobby state or roster changes.
    /// </summary>
    public struct MsgLobbyStateSync : NetworkMessage
    {
        public string rosterBundleJson;
    }

    /// <summary>
    /// Broadcast from Server (Host/DM) to all Clients when all members are Ready and the session launches into the 3D world.
    /// </summary>
    public struct MsgLobbyLaunchSession : NetworkMessage
    {
        public int sessionMode; // 1 = Start New Character, 2 = Load Saved Character
        public int hostSaveKey;
    }

    #endregion

    #region Phase 18: Mods Catcher & Configuration Replication

    /// <summary>
    /// Metadata for a single loaded mod in the 1:1 manifest handshake.
    /// </summary>
    [Serializable]
    public class ModManifestEntry
    {
        public string fileName;
        public string title;
        public string version;
        public string guid;
        public string sha256Hash;
        public long fileSizeBytes;
        public bool hasSettings;
        public bool isEnabled;
    }

    /// <summary>
    /// Container for transmitting full mod manifests over the network.
    /// </summary>
    [Serializable]
    public class ModManifestBundle
    {
        public int connectionId;
        public List<ModManifestEntry> entries = new List<ModManifestEntry>();
    }

    /// <summary>
    /// Sent by client to server reporting its complete mod manifest with SHA-256 hashes.
    /// </summary>
    public struct MsgModManifestReport : NetworkMessage
    {
        public string manifestBundleJson;
    }

    /// <summary>
    /// Authoritative diagnostic evaluation sent from Host to Client detailing parity or conflicts.
    /// </summary>
    public struct MsgModCompatibilityResponse : NetworkMessage
    {
        public bool is100PercentMatch;
        public string missingModsJson;       // JSON array of strings
        public string extraModsJson;         // JSON array of strings
        public string versionMismatchesJson; // JSON array of strings
        public string diagnosticSummary;
    }

    /// <summary>
    /// Replicated mod configuration payload containing the raw JSON settings for a single mod.
    /// </summary>
    [Serializable]
    public class ModConfigPayload
    {
        public string guid;
        public string fileName;
        public string title;
        public string settingsJson;
    }

    /// <summary>
    /// Authoritative mod settings broadcast sent from Host to Client upon connection or world entry.
    /// </summary>
    public struct MsgModSettingsSync : NetworkMessage
    {
        public string configsJson; // JSON array of ModConfigPayload
    }

    #endregion

    #region Phase 19: Downed State, Cooperative Revive & FreeCam Spectator

    /// <summary>
    /// Message broadcast when a player character drops to 0 HP and enters the unconscious downed (bleedout) state.
    /// </summary>
    public struct MsgPlayerDowned : NetworkMessage
    {
        public int connectionId;
        public string playerName;
        public Vector3 position;
        public float bleedoutSeconds;
    }

    /// <summary>
    /// Message broadcast when a downed player is resuscitated (via first aid, potion, healing spell, or DM resurrect).
    /// </summary>
    public struct MsgPlayerRevive : NetworkMessage
    {
        public int targetConnectionId;
        public string revivedPlayerName;
        public string healerPlayerName;
        public int restoredHealth;
    }

    /// <summary>
    /// Message broadcast when a downed player's bleedout timer expires, transitioning them to FreeCam spectator.
    /// </summary>
    public struct MsgPlayerPermaDeath : NetworkMessage
    {
        public int connectionId;
        public string playerName;
        public Vector3 position;
        public float yaw;
        public int corpseTexture;
    }

    #endregion

    #region Phase 20: Cooperative Party Restoration, Shared Buffs & Non-Magical First Aid

    public enum PartySupportActionType
    {
        HealingPotion,
        StaminaPotion,
        CurePoisonPotion,
        MedicalFirstAid,
        Bandage
    }

    /// <summary>
    /// Routed between party members to deliver non-magical support actions (potions, medical treatment).
    /// </summary>
    public struct MsgPartySupportAction : NetworkMessage
    {
        public int senderConnectionId;
        public int targetConnectionId;
        public string senderName;
        public string targetName;
        public PartySupportActionType actionType;
        public int amount;
        public string actionDescription;
    }

    #endregion

    #region Phase 21: Party Justice, World Stasis & Narrative Prison Escapes

    public enum JusticeEventType
    {
        ArrestAndTrial = 0,
        PrisonDilemma = 1,
        InfectionProgress = 2,
        DaedricSummonCutscene = 3,
        DungeonTeleportTrap = 4,
        CommunalRestVote = 5,
        GuardStandDown = 6
    }

    /// <summary>
    /// Sent by Server/DM or triggered between peers to execute justice, stasis, prison dilemma, or cutscene sync.
    /// </summary>
    public struct MsgTriggerJusticeEvent : NetworkMessage
    {
        public int targetConnectionId;
        public JusticeEventType eventType;
        public int extraIntParam;
        public string extraStringParam;
    }

    /// <summary>
    /// Synchronizes combat stasis state for a player (immune to damage / guard pursuit).
    /// </summary>
    public struct MsgJusticeStasisSync : NetworkMessage
    {
        public int targetConnectionId;
        public bool isStasisActive;
        public string reason;
    }

    /// <summary>
    /// Phase 21: Client requests an escape challenge from the DM (solo or party jailbreak step).
    /// </summary>
    public struct MsgRequestEscapeChallenge : NetworkMessage
    {
        public int rollerConnectionId;
        public string rollerName;
        public int escapeMode; // 0: Solo, 1: Grupal
        public int stepIndex;  // 0, 1, 2
        public string stepTitle;
        public string tacticDescription;
    }

    /// <summary>
    /// Phase 21: Server/DM issues an interactive D20 roll challenge for prison escape.
    /// </summary>
    public struct MsgStartEscapeRollChallenge : NetworkMessage
    {
        public int challengeId;
        public int rollerConnectionId;
        public int escapeMode; // 0: Solo, 1: Grupal
        public int stepIndex;  // 0, 1, 2
        public string title;
        public int targetDC;
    }

    /// <summary>
    /// Phase 21: Server broadcasts outcome of escape roll challenge (advancing step or doubling sentence).
    /// </summary>
    public struct MsgEscapeRollResolution : NetworkMessage
    {
        public int challengeId;
        public int targetConnectionId;
        public int escapeMode; // 0: Solo, 1: Grupal
        public int rollResult;
        public int targetDC;
        public bool passed;
        public int newSentenceDays;
        public int newStepIndex; // 0, 1, 2, or 3 (complete)
        public string feedback;
    }

    #endregion

    #endregion

    /// <summary>
    /// Ensures Mirror Reader&lt;T&gt; and Writer&lt;T&gt; delegates are always registered at runtime,
    /// both when Mirror.GeneratedNetworkCode is present and as a fallback when an assembly is compiled
    /// directly via CLI without Mirror Weaver post-processing.
    /// </summary>
    public static class DFUNetworkSerializerBootstrap
    {
        private static bool isInitialized;
        private static readonly HashSet<Type> registeredWriterTypes = new HashSet<Type>();
        private static readonly HashSet<Type> registeredReaderTypes = new HashSet<Type>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInitialized()
        {
            if (isInitialized && Writer<MsgPlayerProfile>.write != null && Reader<MsgPlayerProfile>.read != null)
                return;

            isInitialized = true;

            try
            {
                // 1. Invoke Mirror's woven InitReadWriters() if present in any loaded assembly
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type genType = asm.GetType("Mirror.GeneratedNetworkCode", false);
                    if (genType != null)
                    {
                        MethodInfo initMethod = genType.GetMethod("InitReadWriters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        initMethod?.Invoke(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU Network] Error invoking GeneratedNetworkCode.InitReadWriters: {ex.Message}");
            }

            // 2. If already populated by Mirror Weaver, we are done
            if (Writer<MsgPlayerProfile>.write != null && Reader<MsgPlayerProfile>.read != null)
                return;

            // 3. Fallback registration for CLI builds where Mirror Weaver did not run
            try
            {
                RegisterBuiltInMirrorExtensions();

                // Register Mirror built-in NetworkMessages
                foreach (Type t in typeof(NetworkWriter).Assembly.GetTypes())
                {
                    if (!t.IsInterface && !t.IsAbstract && typeof(NetworkMessage).IsAssignableFrom(t))
                    {
                        EnsureTypeRegistered(t);
                    }
                }

                // Register all NetworkMessages in this assembly
                foreach (Type t in typeof(DFUNetworkSerializerBootstrap).Assembly.GetTypes())
                {
                    if (!t.IsInterface && !t.IsAbstract && typeof(NetworkMessage).IsAssignableFrom(t))
                    {
                        EnsureTypeRegistered(t);
                    }
                }

                Debug.Log("[DFU Network] Runtime fallback NetworkMessage serializers initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DFU Network] Failed to initialize fallback network serializers: {ex}");
            }
        }

        private static void RegisterBuiltInMirrorExtensions()
        {
            foreach (MethodInfo method in typeof(NetworkWriterExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.IsGenericMethod || method.ReturnType != typeof(void))
                    continue;
                ParameterInfo[] pars = method.GetParameters();
                if (pars.Length != 2 || pars[0].ParameterType != typeof(NetworkWriter))
                    continue;

                Type dataType = pars[1].ParameterType;
                if (dataType.IsByRef || dataType.ContainsGenericParameters)
                    continue;

                SetWriterDelegate(dataType, CreateMethodDelegate(typeof(Action<,>).MakeGenericType(typeof(NetworkWriter), dataType), method));
                registeredWriterTypes.Add(dataType);
            }

            foreach (MethodInfo method in typeof(NetworkReaderExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.IsGenericMethod || method.ReturnType == typeof(void))
                    continue;
                ParameterInfo[] pars = method.GetParameters();
                if (pars.Length != 1 || pars[0].ParameterType != typeof(NetworkReader))
                    continue;

                Type dataType = method.ReturnType;
                if (dataType.IsByRef || dataType.ContainsGenericParameters)
                    continue;

                SetReaderDelegate(dataType, CreateMethodDelegate(typeof(Func<,>).MakeGenericType(typeof(NetworkReader), dataType), method));
                registeredReaderTypes.Add(dataType);
            }
        }

        private static void EnsureTypeRegistered(Type type)
        {
            if (type == null || type.IsByRef || type.ContainsGenericParameters)
                return;

            EnsureWriterRegistered(type);
            EnsureReaderRegistered(type);
        }

        private static void EnsureWriterRegistered(Type type)
        {
            if (registeredWriterTypes.Contains(type) || GetWriterDelegate(type) != null)
            {
                registeredWriterTypes.Add(type);
                return;
            }

            registeredWriterTypes.Add(type);

            MethodInfo genericHelper = typeof(DFUNetworkSerializerBootstrap)
                .GetMethod(nameof(RegisterWriterGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            genericHelper.Invoke(null, null);
        }

        private static void EnsureReaderRegistered(Type type)
        {
            if (registeredReaderTypes.Contains(type) || GetReaderDelegate(type) != null)
            {
                registeredReaderTypes.Add(type);
                return;
            }

            registeredReaderTypes.Add(type);

            MethodInfo genericHelper = typeof(DFUNetworkSerializerBootstrap)
                .GetMethod(nameof(RegisterReaderGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            genericHelper.Invoke(null, null);
        }

        private static void RegisterWriterGeneric<T>()
        {
            if (Writer<T>.write != null)
                return;

            Type type = typeof(T);

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                EnsureWriterRegistered(elemType);
                MethodInfo writeArrayMethod = FindGenericExtension(typeof(NetworkWriterExtensions), nameof(NetworkWriterExtensions.WriteArray), 2)
                    .MakeGenericMethod(elemType);
                Writer<T>.write = (Action<NetworkWriter, T>)Delegate.CreateDelegate(typeof(Action<NetworkWriter, T>), writeArrayMethod);
                return;
            }

            if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                EnsureWriterRegistered(underlying);
                Writer<T>.write = (writer, value) =>
                {
                    if (underlying == typeof(byte)) writer.WriteByte(Convert.ToByte(value));
                    else if (underlying == typeof(sbyte)) writer.WriteSByte(Convert.ToSByte(value));
                    else if (underlying == typeof(short)) writer.WriteShort(Convert.ToInt16(value));
                    else if (underlying == typeof(ushort)) writer.WriteUShort(Convert.ToUInt16(value));
                    else if (underlying == typeof(uint)) writer.WriteUInt(Convert.ToUInt32(value));
                    else if (underlying == typeof(long)) writer.WriteLong(Convert.ToInt64(value));
                    else if (underlying == typeof(ulong)) writer.WriteULong(Convert.ToUInt64(value));
                    else writer.WriteInt(Convert.ToInt32(value));
                };
                return;
            }

            if (type.IsGenericType)
            {
                Type genDef = type.GetGenericTypeDefinition();
                if (genDef == typeof(List<>))
                {
                    Type elemType = type.GetGenericArguments()[0];
                    EnsureWriterRegistered(elemType);
                    MethodInfo writeListMethod = FindGenericExtension(typeof(NetworkWriterExtensions), nameof(NetworkWriterExtensions.WriteList), 2)
                        .MakeGenericMethod(elemType);
                    Writer<T>.write = (Action<NetworkWriter, T>)Delegate.CreateDelegate(typeof(Action<NetworkWriter, T>), writeListMethod);
                    return;
                }
                if (genDef == typeof(ArraySegment<>))
                {
                    Type elemType = type.GetGenericArguments()[0];
                    EnsureWriterRegistered(elemType);
                    MethodInfo writeSegMethod = FindGenericExtension(typeof(NetworkWriterExtensions), nameof(NetworkWriterExtensions.WriteArraySegment), 2)
                        .MakeGenericMethod(elemType);
                    Writer<T>.write = (Action<NetworkWriter, T>)Delegate.CreateDelegate(typeof(Action<NetworkWriter, T>), writeSegMethod);
                    return;
                }
            }

            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                Writer<T>.write = (writer, value) => writer.WriteNetworkBehaviour((NetworkBehaviour)(object)value);
                return;
            }

            FieldInfo[] fields = GetOrderedPublicInstanceFields(type);
            Action<NetworkWriter, object>[] fieldWriters = new Action<NetworkWriter, object>[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                EnsureWriterRegistered(f.FieldType);
                MethodInfo writeFieldHelper = typeof(DFUNetworkSerializerBootstrap)
                    .GetMethod(nameof(CreateFieldWriter), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(f.FieldType);
                fieldWriters[i] = (Action<NetworkWriter, object>)writeFieldHelper.Invoke(null, new object[] { f });
            }

            bool isValueType = type.IsValueType;
            Writer<T>.write = (writer, value) =>
            {
                if (!isValueType)
                {
                    if (value == null)
                    {
                        writer.WriteBool(false);
                        return;
                    }
                    writer.WriteBool(true);
                }

                object boxed = value;
                for (int i = 0; i < fieldWriters.Length; i++)
                {
                    fieldWriters[i](writer, boxed);
                }
            };
        }

        private static void RegisterReaderGeneric<T>()
        {
            if (Reader<T>.read != null)
                return;

            Type type = typeof(T);

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                EnsureReaderRegistered(elemType);
                MethodInfo readArrayMethod = FindGenericExtension(typeof(NetworkReaderExtensions), nameof(NetworkReaderExtensions.ReadArray), 1)
                    .MakeGenericMethod(elemType);
                Reader<T>.read = (Func<NetworkReader, T>)Delegate.CreateDelegate(typeof(Func<NetworkReader, T>), readArrayMethod);
                return;
            }

            if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                EnsureReaderRegistered(underlying);
                Reader<T>.read = reader =>
                {
                    object raw;
                    if (underlying == typeof(byte)) raw = reader.ReadByte();
                    else if (underlying == typeof(sbyte)) raw = reader.ReadSByte();
                    else if (underlying == typeof(short)) raw = reader.ReadShort();
                    else if (underlying == typeof(ushort)) raw = reader.ReadUShort();
                    else if (underlying == typeof(uint)) raw = reader.ReadUInt();
                    else if (underlying == typeof(long)) raw = reader.ReadLong();
                    else if (underlying == typeof(ulong)) raw = reader.ReadULong();
                    else raw = reader.ReadInt();
                    return (T)Enum.ToObject(type, raw);
                };
                return;
            }

            if (type.IsGenericType)
            {
                Type genDef = type.GetGenericTypeDefinition();
                if (genDef == typeof(List<>))
                {
                    Type elemType = type.GetGenericArguments()[0];
                    EnsureReaderRegistered(elemType);
                    MethodInfo readListMethod = FindGenericExtension(typeof(NetworkReaderExtensions), nameof(NetworkReaderExtensions.ReadList), 1)
                        .MakeGenericMethod(elemType);
                    Reader<T>.read = (Func<NetworkReader, T>)Delegate.CreateDelegate(typeof(Func<NetworkReader, T>), readListMethod);
                    return;
                }
                if (genDef == typeof(ArraySegment<>))
                {
                    Type elemType = type.GetGenericArguments()[0];
                    Type arrType = elemType.MakeArrayType();
                    EnsureReaderRegistered(arrType);
                    ConstructorInfo segCtor = type.GetConstructor(new[] { arrType });
                    Reader<T>.read = reader =>
                    {
                        object arr = InvokeReadGeneric(elemType.MakeArrayType(), reader);
                        return (T)segCtor.Invoke(new[] { arr });
                    };
                    return;
                }
            }

            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                MethodInfo readNbMethod = FindGenericExtension(typeof(NetworkReaderExtensions), nameof(NetworkReaderExtensions.ReadNetworkBehaviour), 1)
                    .MakeGenericMethod(type);
                Reader<T>.read = (Func<NetworkReader, T>)Delegate.CreateDelegate(typeof(Func<NetworkReader, T>), readNbMethod);
                return;
            }

            FieldInfo[] fields = GetOrderedPublicInstanceFields(type);
            Action<NetworkReader, object>[] fieldReaders = new Action<NetworkReader, object>[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                EnsureReaderRegistered(f.FieldType);
                MethodInfo readFieldHelper = typeof(DFUNetworkSerializerBootstrap)
                    .GetMethod(nameof(CreateFieldReader), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(f.FieldType);
                fieldReaders[i] = (Action<NetworkReader, object>)readFieldHelper.Invoke(null, new object[] { f });
            }

            bool isValueType = type.IsValueType;
            Reader<T>.read = reader =>
            {
                if (!isValueType)
                {
                    if (!reader.ReadBool())
                        return default;
                }

                object boxed = isValueType ? (object)default(T) : Activator.CreateInstance(type);
                for (int i = 0; i < fieldReaders.Length; i++)
                {
                    fieldReaders[i](reader, boxed);
                }
                return (T)boxed;
            };
        }

        private static Action<NetworkWriter, object> CreateFieldWriter<TField>(FieldInfo field)
        {
            return (writer, instance) =>
            {
                TField val = (TField)field.GetValue(instance);
                writer.Write(val);
            };
        }

        private static Action<NetworkReader, object> CreateFieldReader<TField>(FieldInfo field)
        {
            return (reader, instance) =>
            {
                TField val = reader.Read<TField>();
                field.SetValue(instance, val);
            };
        }

        private static FieldInfo[] GetOrderedPublicInstanceFields(Type type)
        {
            List<Type> hierarchy = new List<Type>();
            Type cur = type;
            while (cur != null && cur != typeof(object) && cur != typeof(ValueType))
            {
                hierarchy.Insert(0, cur);
                cur = cur.BaseType;
            }

            List<FieldInfo> result = new List<FieldInfo>();
            foreach (Type t in hierarchy)
            {
                FieldInfo[] declared = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                result.AddRange(declared);
            }
            return result.ToArray();
        }

        private static MethodInfo FindGenericExtension(Type extClass, string name, int paramCount)
        {
            foreach (MethodInfo m in extClass.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name == name && m.IsGenericMethodDefinition && m.GetParameters().Length == paramCount)
                    return m;
            }
            return null;
        }

        private static object InvokeReadGeneric(Type t, NetworkReader reader)
        {
            FieldInfo readField = typeof(Reader<>).MakeGenericType(t).GetField("read", BindingFlags.Public | BindingFlags.Static);
            Delegate del = readField?.GetValue(null) as Delegate;
            return del != null ? del.DynamicInvoke(reader) : null;
        }

        private static Delegate CreateMethodDelegate(Type delegateType, MethodInfo method)
        {
            return Delegate.CreateDelegate(delegateType, method);
        }

        private static object GetWriterDelegate(Type t)
        {
            FieldInfo f = typeof(Writer<>).MakeGenericType(t).GetField("write", BindingFlags.Public | BindingFlags.Static);
            return f?.GetValue(null);
        }

        private static void SetWriterDelegate(Type t, Delegate del)
        {
            FieldInfo f = typeof(Writer<>).MakeGenericType(t).GetField("write", BindingFlags.Public | BindingFlags.Static);
            f?.SetValue(null, del);
        }

        private static object GetReaderDelegate(Type t)
        {
            FieldInfo f = typeof(Reader<>).MakeGenericType(t).GetField("read", BindingFlags.Public | BindingFlags.Static);
            return f?.GetValue(null);
        }

        private static void SetReaderDelegate(Type t, Delegate del)
        {
            FieldInfo f = typeof(Reader<>).MakeGenericType(t).GetField("read", BindingFlags.Public | BindingFlags.Static);
            f?.SetValue(null, del);
        }
    }
}




