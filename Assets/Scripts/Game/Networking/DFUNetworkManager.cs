// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using kcp2k;
using Wenzil.Console;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallConnect.Save;
using DaggerfallConnect.FallExe;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Native NetworkManager for Daggerfall Unity.
    /// Manages server/client lifecycle, KCP transport configuration, and connected peer sessions.
    /// </summary>
    public class DFUNetworkManager : NetworkManager
    {
        #region Fields & Properties

        public static DFUNetworkManager Instance
        {
            get { return singleton as DFUNetworkManager; }
        }

        public static int LastCrimeConnId = 0;

        [Header("DFU Native Network Settings")]
        [Tooltip("Default port for KCP transport")]
        public ushort defaultPort = 7777;

        [Tooltip("Unified TTRPG Master Suite overlay (toggle via Ctrl+F11 or console command ttrpg_gui / dm_gui)")]
        public bool showTtrpgSuite = false;
        private Rect ttrpgSuiteRect = new Rect(200, 20, 660, 620);
        public int ttrpgSelectedTab = 0;
        public readonly string[] ttrpgTabNames = new string[]
        {
            "Red & Sesión",
            "Spawns & NPCs",
            "Mundo & Clima",
            "Quests & Diálogos",
            "Loot & Forja",
            "Party & DM"
        };
        private Vector2 ttrpgSuiteScrollPos = Vector2.zero;

        [Tooltip("Show in-game network debug overlay (toggle via console command net_gui)")]
        public bool showDebugGui = false;

        [Tooltip("Show Dungeon Master (DM) enemy spawner overlay (toggle via console command dm_gui)")]
        public bool showDmGui = false;

        [Tooltip("Show DM Loot Spawner overlay (toggle via Ctrl+F12 or console command loot_gui)")]
        public bool showLootGui = false;
        private Rect lootWindowRect = new Rect(680, 10, 360, 560);
        private readonly List<DaggerfallLoot> spawnedLootContainers = new List<DaggerfallLoot>();

        [Tooltip("Show Quest Creator / Test Menu overlay (toggle via Ctrl+F7 or console command quest_gui)")]
        public bool showQuestGui = false;
        private Rect questWindowRect = new Rect(650, 20, 480, 650);
        private Vector2 questScrollPos = Vector2.zero;
        private string questInputName = "M0B00Y00";
        private string questInputFaction = "0";

        // Phase 10: Quest Creator Geolocation & Deterministic Seed state
        private int questCreatorMode = 0; // 0 = Cercanía (Local Town), 1 = Destino Remoto (Remote Province & Settlement)
        private int questSelectedRegionIndex = 17; // Default 17 = Daggerfall
        private string questTownSearchFilter = "";
        private string questCustomSeedText = "0";
        private int questSelectedSettlementIndex = 0;

        public struct SettlementItem
        {
            public string name;
            public DFRegion.LocationTypes type;
            public int locationIndex;
            public int mapPixelX;
            public int mapPixelY;
        }
        private readonly List<SettlementItem> cachedSettlements = new List<SettlementItem>();
        private int cachedSettlementRegionIndex = -1;

        private int selectedLootEnemyIndex = 0;
        private readonly MobileTypes[] lootEnemies = new MobileTypes[]
        {
            MobileTypes.Rat,
            MobileTypes.GiantBat,
            MobileTypes.SkeletalWarrior,
            MobileTypes.Zombie,
            MobileTypes.GrizzlyBear,
            MobileTypes.Thief,
            MobileTypes.Orc,
            MobileTypes.Giant,
            MobileTypes.Mummy,
            MobileTypes.Vampire,
            MobileTypes.Daedroth,
            MobileTypes.Knight_CityWatch
        };

        // P2P Interactive Player Menu & Map Sharing
        public bool showPlayerInteractionWindow = false;
        private Rect playerInteractionWindowRect = new Rect((Screen.width - 380) * 0.5f, (Screen.height - 270) * 0.5f, 380, 270);
        private NativePlayerPuppet interactionTargetPuppet = null;

        // Party Reputation Inspector state
        private int selectedRepPlayerIndex = 0;
        private readonly Dictionary<int, Dictionary<int, int>> remotePlayerReputations = new Dictionary<int, Dictionary<int, int>>();

        // Bilateral Trading System State
        public bool showTradeWindow = false;
        public bool showTradeInvitePrompt = false;
        private Rect tradeWindowRect = new Rect((Screen.width - 720) * 0.5f, (Screen.height - 560) * 0.5f, 720, 560);
        private Rect tradeInviteRect = new Rect((Screen.width - 420) * 0.5f, 120, 420, 185);
        private int activeTradePartnerConnId = -1;
        private string activeTradePartnerName = "";
        private NativePlayerPuppet activeTradePartnerPuppet = null;
        private float tradeInviteTimeout = 0f;
        private int pendingTradeInviteSenderConnId = -1;
        private string pendingTradeInviteSenderName = "";

        // DM Spell Teaching State
        private int selectedDmSpellTargetIndex = 0;
        private string dmSpellFilter = "";
        private Vector2 dmSpellListScrollPos;

        // Local trade offer state
        private int localTradeGold = 0;
        private readonly List<DaggerfallUnityItem> localTradeItems = new List<DaggerfallUnityItem>();
        private bool localTradeLocked = false;
        private bool localTradeAccepted = false;
        private Vector2 localOfferScrollPos;
        private Vector2 inventoryPickerScrollPos;
        private bool showInventoryPicker = false;

        // Remote partner trade offer state
        private int remoteTradeGold = 0;
        private readonly List<TradeItemDTO> remoteTradeItems = new List<TradeItemDTO>();
        private bool remoteTradeLocked = false;
        private bool remoteTradeAccepted = false;
        private Vector2 remoteOfferScrollPos;

        // Server authoritative pending commits
        private readonly Dictionary<int, MsgTradeCommit> serverPendingTradeCommits = new Dictionary<int, MsgTradeCommit>();

        /// <summary>
        /// True if any network, DM, loot, quest, player interaction, or trade debug overlay is open.
        /// </summary>
        public bool IsAnyGuiOpen => showTtrpgSuite || showDebugGui || showDmGui || showLootGui || showQuestGui || showPlayerInteractionWindow || showTradeWindow || showTradeInvitePrompt;

        private KcpTransport kcpTransport;

        /// <summary>
        /// Track active player connections on the server.
        /// </summary>
        public readonly HashSet<NetworkConnection> connectedClients = new HashSet<NetworkConnection>();

        /// <summary>
        /// Local player connection ID assigned by the server (Host is always 0).
        /// </summary>
        public int localConnectionId = 0;

        /// <summary>
        /// True if there are multiple active players in the session (host with clients or connected client).
        /// </summary>
        public bool HasMultiplePlayers => NetworkSession.IsActive && ((NetworkServer.active && (connectedPlayerProfiles.Count > 1 || connectedClients.Count > 0)) || (NetworkClient.active && !NetworkServer.active));

        [Header("Party Decision & Voting State")]
        private bool isVoteActive = false;
        private PartyVoteType currentVoteType = PartyVoteType.Rest;
        private VoteConsensusPolicy currentVotePolicy = VoteConsensusPolicy.Unanimous;
        private string currentVoteTitle = "";
        private string currentVoteDetails = "";
        private string currentVoteInitiator = "";
        private int currentVoteInitiatorConnId = -1;
        private float currentVoteTimer = 0f;
        private const float DefaultVoteTimeout = 60f;
        public bool isDmInvisible = false;
        public DMEntityMode currentDmEntityMode = DMEntityMode.PhysicalAvatar;
        public DMEntityMode CurrentDmEntityMode => currentDmEntityMode;
        public bool IsSpectatorSpirit => currentDmEntityMode == DMEntityMode.SpectatorSpirit;
        private readonly HashSet<ulong> expandedQuestDetails = new HashSet<ulong>();
        private readonly HashSet<int> currentVoteApprovedClients = new HashSet<int>();
        private DaggerfallPartyVoteWindow partyVoteWindow = null;
        private Dictionary<int, string> currentVoteMemberStatuses = new Dictionary<int, string>(); // connId -> "Yes" | "Pending" | "No"
        private bool localClientHasVoted = false;
        private float currentVoteBroadcastTimer = 0f;
        private const float VoteBroadcastInterval = 0.5f;

        // Custom vote payload
        private int voteIntParam1 = 0;
        private int voteIntParam2 = 0;
        private bool voteBoolParam1 = false;
        private bool voteBoolParam2 = false;
        private string voteStringParam = "";
        private int currentVoteRequiredGold = 0;

        // Backwards compatibility aliases for legacy rest code
        private bool isRestVoteActive { get => isVoteActive; set => isVoteActive = value; }
        private int restVoteHours { get => voteIntParam1; set => voteIntParam1 = value; }
        private bool restVoteIsLoiter { get => voteBoolParam1; set => voteBoolParam1 = value; }
        private string restVoteInitiator { get => currentVoteInitiator; set => currentVoteInitiator = value; }
        private int restVoteInitiatorConnId { get => currentVoteInitiatorConnId; set => currentVoteInitiatorConnId = value; }
        private float restVoteTimer { get => currentVoteTimer; set => currentVoteTimer = value; }
        private float restVoteBroadcastTimer { get => currentVoteBroadcastTimer; set => currentVoteBroadcastTimer = value; }
        private HashSet<int> restVoteApprovedClients => currentVoteApprovedClients;
        private Dictionary<int, string> restVoteMemberStatuses => currentVoteMemberStatuses;

        private bool isPartyResting = false;
        private bool isPartyRestingBlackout = false;
        private int partyRestHours = 0;
        private bool partyRestIsLoiter = false;
        private GUIStyle blackoutStyle;

        [Header("World Time & Weather Sync")]
        private float worldTimeSyncTimer = 0f;
        private const float WorldTimeSyncInterval = 3.0f;
        private ulong lastBroadcastTimeInSeconds = 0;
        private Vector2 dmScrollPosition = Vector2.zero;
        private Vector2 dmLootScrollPosition = Vector2.zero;

        [Header("Town Population Sync")]
        private float populationSyncTimer = 0f;
        private const float PopulationSyncInterval = 0.35f;

        [Header("Magic & PvP Settings")]
        public static bool isPvpAllowed = false;

        [Header("Nametags & Visibility Settings")]
        public float nametagMaxDistance = 35f;

        #endregion

        #region Events

        public event Action OnServerStartedEvent;
        public event Action OnServerStoppedEvent;
        public event Action<NetworkConnection> OnClientConnectedToServerEvent;
        public event Action<NetworkConnection> OnClientDisconnectedFromServerEvent;
        public event Action OnClientConnectedToHostEvent;
        public event Action OnClientDisconnectedFromHostEvent;

        #endregion

        #region Initialization

        public override void Awake()
        {
            autoCreatePlayer = false;
            EnsureTransportConfigured();
            if (GetComponent<FreeCamController>() == null)
            {
                gameObject.AddComponent<FreeCamController>();
            }
            if (GetComponent<DMNpcController>() == null)
            {
                gameObject.AddComponent<DMNpcController>();
            }
            PlayerEnterExit.OnPreTransition += HandleOnPreTransition;
            PlayerEnterExit.OnTransitionInterior += HandleOnTransitionInterior;
            PlayerEnterExit.OnTransitionExterior += HandleOnTransitionExterior;
            PlayerEnterExit.OnTransitionDungeonInterior += HandleOnTransitionDungeonInterior;
            PlayerEnterExit.OnTransitionDungeonExterior += HandleOnTransitionDungeonExterior;
            SaveLoadManager.OnLoad += HandleSaveLoadManagerOnLoad;
            StartGameBehaviour.OnStartGame += HandleStartGameBehaviourOnStartGame;
            base.Awake();
        }

        public override void OnDestroy()
        {
            PlayerEnterExit.OnPreTransition -= HandleOnPreTransition;
            PlayerEnterExit.OnTransitionInterior -= HandleOnTransitionInterior;
            PlayerEnterExit.OnTransitionExterior -= HandleOnTransitionExterior;
            PlayerEnterExit.OnTransitionDungeonInterior -= HandleOnTransitionDungeonInterior;
            PlayerEnterExit.OnTransitionDungeonExterior -= HandleOnTransitionDungeonExterior;
            SaveLoadManager.OnLoad -= HandleSaveLoadManagerOnLoad;
            StartGameBehaviour.OnStartGame -= HandleStartGameBehaviourOnStartGame;
            base.OnDestroy();
        }

        private void Update()
        {
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F11))
            {
                showTtrpgSuite = !showTtrpgSuite;
            }

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F10))
            {
                if (showTtrpgSuite && ttrpgSelectedTab == 0)
                    showTtrpgSuite = false;
                else
                {
                    showTtrpgSuite = true;
                    ttrpgSelectedTab = 0;
                }
            }

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F12))
            {
                if (NetworkSession.IsServer || localLobbyIsDm)
                {
                    if (showTtrpgSuite && ttrpgSelectedTab == 4)
                        showTtrpgSuite = false;
                    else
                    {
                        showTtrpgSuite = true;
                        ttrpgSelectedTab = 4;
                    }
                }
            }

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F7))
            {
                if (NetworkSession.IsServer || localLobbyIsDm)
                {
                    if (showTtrpgSuite && ttrpgSelectedTab == 3)
                        showTtrpgSuite = false;
                    else
                    {
                        showTtrpgSuite = true;
                        ttrpgSelectedTab = 3;
                    }
                }
            }

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F8))
            {
                if (NetworkSession.IsServer || localLobbyIsDm)
                {
                    ToggleDmEntityMode();
                }
                else if (FreeCamController.Instance != null)
                {
                    FreeCamController.Instance.ToggleFreeCam();
                }
            }

            // DM Quick Teleport in Physical Avatar mode: Ctrl + Shift + Left Click
            if ((NetworkSession.IsServer || localLobbyIsDm) && (FreeCamController.Instance == null || !FreeCamController.Instance.IsActive))
            {
                if (ctrlHeld && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButtonDown(0))
                {
                    if (GameManager.Instance != null && GameManager.Instance.MainCamera != null)
                    {
                        Ray ray = new Ray(GameManager.Instance.MainCamera.transform.position, GameManager.Instance.MainCamera.transform.forward);
                        if (Physics.Raycast(ray, out RaycastHit hit, 2000f))
                        {
                            Vector3 targetPos = hit.point + Vector3.up * 0.15f;
                            RelocateLocalPlayerToCamera(targetPos, GameManager.Instance.MainCamera.transform.eulerAngles.y);
                            DaggerfallUI.AddHUDText($"[DM Teleport] Teleported to ({hit.point.x:F1}, {hit.point.y:F1}, {hit.point.z:F1})");
                        }
                    }
                }
            }

            if (showTtrpgSuite || showDebugGui || showDmGui || showLootGui || showQuestGui || showPlayerInteractionWindow || showTradeWindow || showTradeInvitePrompt)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (showTtrpgSuite && Input.GetKeyDown(KeyCode.Escape))
            {
                showTtrpgSuite = false;
            }

            if (showTradeInvitePrompt)
            {
                tradeInviteTimeout -= Time.deltaTime;
                if (tradeInviteTimeout <= 0f)
                {
                    showTradeInvitePrompt = false;
                    DaggerfallUI.AddHUDText("[Comercio] La solicitud de comercio ha expirado.");
                }
            }

            if (showTradeWindow)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelTrade("Comercio cancelado por el jugador.");
                }
                else if (activeTradePartnerPuppet != null && activeTradePartnerPuppet.gameObject != null && GameManager.Instance.PlayerObject != null)
                {
                    float dist = Vector3.Distance(GameManager.Instance.PlayerObject.transform.position, activeTradePartnerPuppet.transform.position);
                    if (dist > 6.0f)
                    {
                        CancelTrade("Cancelado: El compañero se alejó demasiado.");
                    }
                }
            }

            if (showPlayerInteractionWindow)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F))
                {
                    showPlayerInteractionWindow = false;
                }
                else if (interactionTargetPuppet == null || interactionTargetPuppet.gameObject == null)
                {
                    showPlayerInteractionWindow = false;
                }
                else if (GameManager.Instance.PlayerObject != null &&
                         Vector3.Distance(GameManager.Instance.PlayerObject.transform.position, interactionTargetPuppet.transform.position) > 4.5f)
                {
                    showPlayerInteractionWindow = false;
                }
            }
            else if (NetworkSession.IsActive && Input.GetKeyDown(KeyCode.F))
            {
                if (!IsAnyGuiOpen && (DaggerfallUI.UIManager == null || DaggerfallUI.UIManager.WindowCount == 0) && GUIUtility.keyboardControl == 0)
                {
                    TryInteractWithNearbyPlayer();
                }
            }

            UpdateDialogueBubbles(Time.deltaTime);

            if (NetworkSession.IsActive)
            {
                StreamLocalPlayerTransform();

                if (NetworkServer.active && DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
                {
                    worldTimeSyncTimer += Time.deltaTime;
                    ulong currentSec = DaggerfallUnity.Instance.WorldTime.Now.ToSeconds();
                    long deltaSec = (long)currentSec - (long)lastBroadcastTimeInSeconds;
                    bool timeJump = lastBroadcastTimeInSeconds != 0 && (deltaSec < 0 || deltaSec > 15);

                    if (worldTimeSyncTimer >= WorldTimeSyncInterval || timeJump)
                    {
                        BroadcastWorldTime(timeJump);
                    }
                }

                if (NetworkServer.active)
                {
                    SyncTownPopulation(Time.deltaTime);
                }
            }

            if (isVoteActive && NetworkServer.active)
            {
                currentVoteTimer -= Time.deltaTime;
                if (currentVoteTimer <= 0)
                {
                    HandleVoteTimeout();
                }
                else
                {
                    currentVoteBroadcastTimer -= Time.deltaTime;
                    if (currentVoteBroadcastTimer <= 0f)
                    {
                        currentVoteBroadcastTimer = VoteBroadcastInterval;
                        BroadcastVoteUpdate();
                    }
                }
            }
        }

        /// <summary>
        /// Ensures a Transport (defaulting to KcpTransport) is attached and assigned.
        /// </summary>
        public void EnsureTransportConfigured()
        {
            if (transport == null)
            {
                kcpTransport = GetComponent<KcpTransport>();
                if (kcpTransport == null)
                {
                    kcpTransport = gameObject.AddComponent<KcpTransport>();
                }
                kcpTransport.Port = defaultPort;
                transport = kcpTransport;
            }
            else if (transport is KcpTransport kt)
            {
                kcpTransport = kt;
            }
        }

        #region Logging Filters & Helpers

        public static bool LogTimeEnabled = false; // default false
        public static bool LogWeatherEnabled = true;
        public static bool LogEntitiesEnabled = true;
        public static bool LogSpawnsEnabled = true;
        public static bool LogLootEnabled = true;
        public static bool LogTransitionsEnabled = true;

        public void LogTime(string msg) { if (LogTimeEnabled) LogNetwork(msg); }
        public void LogWeather(string msg) { if (LogWeatherEnabled) LogNetwork(msg); }
        public void LogSpawn(string msg) { if (LogSpawnsEnabled) LogNetwork(msg); }
        public void LogLoot(string msg) { if (LogLootEnabled) LogNetwork(msg); }
        public void LogTransition(string msg) { if (LogTransitionsEnabled) LogNetwork(msg); }
        public void LogEntity(string msg) { if (LogEntitiesEnabled) LogNetwork(msg); }

        #endregion

        private void LogNetwork(string message)
        {
            DaggerfallUnity.LogMessage(message, true);
            try
            {
                Wenzil.Console.Console.Log(message);
            }
            catch {}
        }

        #endregion

        #region Session Lifecycle Control

        /// <summary>
        /// Starts hosting a game session (authoritative server + local client).
        /// </summary>
        public bool StartHosting(ushort port = 7777)
        {
            EnsureTransportConfigured();
            if (kcpTransport != null)
                kcpTransport.Port = port;

            networkAddress = "localhost";
            LogNetwork($"[DFU Network] Starting Host on port {port} (KCP Transport)...");
            StartHost();
            return NetworkServer.active;
        }

        /// <summary>
        /// Connects to a remote host as a pure client.
        /// </summary>
        public void ConnectToServer(string hostAddress, ushort port = 7777)
        {
            EnsureTransportConfigured();
            if (kcpTransport != null)
                kcpTransport.Port = port;

            if (string.IsNullOrEmpty(hostAddress))
                hostAddress = "127.0.0.1";

            networkAddress = hostAddress;
            LogNetwork($"[DFU Network] Connecting to {hostAddress}:{port} (KCP Transport)...");
            StartClient();
        }

        /// <summary>
        /// Disconnects from current session (stops host, server, or client).
        /// </summary>
        public void DisconnectSession()
        {
            LogNetwork($"[DFU Network] Disconnecting session ({NetworkSession.GetStatusString()})...");

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StopHost();
            }
            else if (NetworkClient.active)
            {
                StopClient();
            }
            else if (NetworkServer.active)
            {
                StopServer();
            }

            if (currentDmEntityMode == DMEntityMode.SpectatorSpirit)
            {
                SetDmEntityMode(DMEntityMode.PhysicalAvatar);
            }

            connectedClients.Clear();
        }

        #endregion

        #region Mirror Server Callbacks

        public override void OnStartServer()
        {
            base.OnStartServer();
            connectedClients.Clear();
            ClearPlayerEntities();
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntityBehaviour != null)
            {
                RegisterPlayerEntity(GameManager.Instance.PlayerEntityBehaviour);
            }
            NetworkServer.RegisterHandler<MsgPlayerProfile>(OnServerReceivePlayerProfile);
            NetworkServer.RegisterHandler<MsgPlayerTransform>(OnServerReceivePlayerTransform);
            NetworkServer.RegisterHandler<MsgPlayerTransition>(OnServerReceivePlayerTransition);
            NetworkServer.RegisterHandler<MsgAttackEntity>(OnServerReceiveAttackEntity);
            NetworkServer.RegisterHandler<MsgRequestLootSync>(OnServerReceiveRequestLootSync);
            NetworkServer.RegisterHandler<MsgLootTakeItem>(OnServerReceiveLootTakeItem);
            NetworkServer.RegisterHandler<MsgLootDropItem>(OnServerReceiveLootDropItem);
            NetworkServer.RegisterHandler<MsgPartyRestVoteRequest>(OnServerReceivePartyRestVoteRequest);
            NetworkServer.RegisterHandler<MsgPartyRestVoteResponse>(OnServerReceivePartyRestVoteResponse);
            NetworkServer.RegisterHandler<MsgPartyVoteRequest>(OnServerReceivePartyVoteRequest);
            NetworkServer.RegisterHandler<MsgPartyVoteResponse>(OnServerReceivePartyVoteResponse);
            NetworkServer.RegisterHandler<MsgPlayerCrime>(OnServerReceivePlayerCrime);
            NetworkServer.RegisterHandler<MsgDialogueSpeech>(OnServerReceiveDialogueSpeech);
            NetworkServer.RegisterHandler<MsgDialogueEnded>(OnServerReceiveDialogueEnded);
            NetworkServer.RegisterHandler<MsgShareMapData>(OnServerReceiveShareMapData);
            NetworkServer.RegisterHandler<MsgClientReputationReport>(OnServerReceiveClientReputationReport);
            NetworkServer.RegisterHandler<MsgTradeInvite>(OnServerReceiveTradeInvite);
            NetworkServer.RegisterHandler<MsgTradeInviteResponse>(OnServerReceiveTradeInviteResponse);
            NetworkServer.RegisterHandler<MsgTradeStateSync>(OnServerReceiveTradeStateSync);
            NetworkServer.RegisterHandler<MsgTradeCancel>(OnServerReceiveTradeCancel);
            NetworkServer.RegisterHandler<MsgTradeCommit>(OnServerReceiveTradeCommit);
            NetworkServer.RegisterHandler<MsgCastSpell>(OnServerReceiveCastSpell);
            NetworkServer.RegisterHandler<MsgApplySpellBundle>(OnServerReceiveApplySpellBundle);
            NetworkServer.RegisterHandler<MsgArrowShot>(OnServerReceiveArrowShot);
            NetworkServer.RegisterHandler<MsgLobbyClientUpdate>(OnServerReceiveLobbyClientUpdate);
            GameManager.OnEnemySpawn += OnServerEnemySpawned;
            WeatherManager.OnWeatherChange += OnServerWeatherChanged;
            worldTimeSyncTimer = 0f;
            if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
                lastBroadcastTimeInSeconds = DaggerfallUnity.Instance.WorldTime.Now.ToSeconds();

            RegisterExistingSceneEnemies();

            MsgPlayerProfile hostProfile = BuildLocalPlayerProfile(0);
            connectedPlayerProfiles[0] = hostProfile;
            InitializeServerLobbyHost();

            LogNetwork("[DFU Network] Authoritative Server started.");
            OnServerStartedEvent?.Invoke();
        }

        public override void OnStopServer()
        {
            GameManager.OnEnemySpawn -= OnServerEnemySpawned;
            WeatherManager.OnWeatherChange -= OnServerWeatherChanged;
            NetworkServer.UnregisterHandler<MsgPlayerProfile>();
            NetworkServer.UnregisterHandler<MsgPlayerTransform>();
            NetworkServer.UnregisterHandler<MsgPlayerTransition>();
            NetworkServer.UnregisterHandler<MsgAttackEntity>();
            NetworkServer.UnregisterHandler<MsgRequestLootSync>();
            NetworkServer.UnregisterHandler<MsgLootTakeItem>();
            NetworkServer.UnregisterHandler<MsgLootDropItem>();
            NetworkServer.UnregisterHandler<MsgPartyRestVoteRequest>();
            NetworkServer.UnregisterHandler<MsgPartyRestVoteResponse>();
            NetworkServer.UnregisterHandler<MsgPartyVoteRequest>();
            NetworkServer.UnregisterHandler<MsgPartyVoteResponse>();
            NetworkServer.UnregisterHandler<MsgPlayerCrime>();
            NetworkServer.UnregisterHandler<MsgDialogueSpeech>();
            NetworkServer.UnregisterHandler<MsgDialogueEnded>();
            NetworkServer.UnregisterHandler<MsgShareMapData>();
            NetworkServer.UnregisterHandler<MsgClientReputationReport>();
            NetworkServer.UnregisterHandler<MsgTradeInvite>();
            NetworkServer.UnregisterHandler<MsgTradeInviteResponse>();
            NetworkServer.UnregisterHandler<MsgTradeStateSync>();
            NetworkServer.UnregisterHandler<MsgTradeCancel>();
            NetworkServer.UnregisterHandler<MsgTradeCommit>();
            NetworkServer.UnregisterHandler<MsgCastSpell>();
            NetworkServer.UnregisterHandler<MsgApplySpellBundle>();
            NetworkServer.UnregisterHandler<MsgArrowShot>();
            NetworkServer.UnregisterHandler<MsgLobbyClientUpdate>();
            serverLobbyPlayers.Clear();
            serverPendingTradeCommits.Clear();
            activeDialogueBubbles.Clear();
            remotePlayerReputations.Clear();
            isVoteActive = false;
            currentVoteApprovedClients.Clear();
            currentVoteMemberStatuses.Clear();
            ClosePartyVoteWindow();
            showTradeWindow = false;
            showTradeInvitePrompt = false;
            ResetTradeOfferState();
            DestroyAllRemotePlayerPuppets();
            ClearPlayerEntities();
            DFUNetworkRegistry.Clear();
            DFUNetworkLootRegistry.Clear();
            spawnedLootContainers.Clear();
            base.OnStopServer();
            connectedClients.Clear();
            LogNetwork("[DFU Network] Server stopped.");
            OnServerStoppedEvent?.Invoke();
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);
            connectedClients.Add(conn);
            LogNetwork($"[DFU Network] Client connected to server: connectionId={conn.connectionId}, address={conn.address}");

            // Inform client of its assigned connection ID
            conn.Send(new MsgPlayerInit { assignedConnectionId = conn.connectionId });

            // Synchronize existing authoritative entities and loot to newly connected client
            SyncExistingEntitiesToClient(conn);

            // Send all registered player profiles to newly connected client
            foreach (var kvp in connectedPlayerProfiles)
            {
                conn.Send(kvp.Value);
            }

            // Synchronize authoritative world time and weather to newly connected client
            if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
            {
                conn.Send(new MsgWorldTimeSync
                {
                    timeInSeconds = DaggerfallUnity.Instance.WorldTime.Now.ToSeconds(),
                    timeScale = DaggerfallUnity.Instance.WorldTime.TimeScale,
                    isImmediate = true
                });
            }
            if (GameManager.Instance != null && GameManager.Instance.WeatherManager != null)
            {
                conn.Send(new MsgWeatherSync
                {
                    weatherType = (byte)GameManager.Instance.WeatherManager.CurrentWeather,
                    isImmediate = true
                });
            }

            OnClientConnectedToServerEvent?.Invoke(conn);
        }

        private void SyncExistingEntitiesToClient(NetworkConnection conn)
        {
            // Synchronize existing authoritative entities to newly connected client
            int syncedCount = 0;
            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value != null)
                {
                    NativeNetworkEntity netEntity = kvp.Value.GetComponent<NativeNetworkEntity>();
                    if (netEntity != null && !netEntity.isClientProxy)
                    {
                        conn.Send(netEntity.ToSpawnMessage());
                        syncedCount++;
                    }
                }
            }
            if (syncedCount > 0)
            {
                LogSpawn($"[DFU Spawn] Synchronized {syncedCount} existing entities to client {conn.connectionId}.");
            }

            // Synchronize existing spawned loot containers to newly connected client
            int syncedLootCount = 0;
            foreach (var loot in spawnedLootContainers)
            {
                if (loot != null)
                {
                    conn.Send(CreateSpawnLootMessage(loot));
                    syncedLootCount++;
                }
            }
            if (syncedLootCount > 0)
            {
                LogLoot($"[DFU Loot] Synchronized {syncedLootCount} spawned loot containers to client {conn.connectionId}.");
            }

            conn.Send(new MsgPvpStateSync { isPvpAllowed = isPvpAllowed });
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            connectedClients.Remove(conn);
            if (isRestVoteActive)
            {
                restVoteApprovedClients.Remove(conn.connectionId);
                restVoteMemberStatuses.Remove(conn.connectionId);
                BroadcastRestVoteUpdate();
                CheckVoteConsensus();
            }
            LogNetwork($"[DFU Network] Client disconnected from server: connectionId={conn.connectionId}");
            serverLobbyPlayers.Remove(conn.connectionId);
            BroadcastLobbyState();
            DespawnRemotePlayerPuppet(conn.connectionId);
            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(new MsgPlayerDespawn { connectionId = conn.connectionId });
            }
            OnClientDisconnectedFromServerEvent?.Invoke(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            if (playerPrefab != null)
            {
                base.OnServerAddPlayer(conn);
            }
        }

        #endregion

        #region Mirror Client Callbacks

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<MsgSpawnEnemy>(OnClientReceiveSpawnEnemy);
            NetworkClient.RegisterHandler<MsgDespawnEnemy>(OnClientReceiveDespawnEnemy);
            NetworkClient.RegisterHandler<MsgEntityTransform>(OnClientReceiveEntityTransform);
            NetworkClient.RegisterHandler<MsgPlayerInit>(OnClientReceivePlayerInit);
            NetworkClient.RegisterHandler<MsgPlayerProfile>(OnClientReceivePlayerProfile);
            NetworkClient.RegisterHandler<MsgPlayerTransform>(OnClientReceivePlayerTransform);
            NetworkClient.RegisterHandler<MsgPlayerTransition>(OnClientReceivePlayerTransition);
            NetworkClient.RegisterHandler<MsgPlayerDespawn>(OnClientReceivePlayerDespawn);
            NetworkClient.RegisterHandler<MsgEntityDamaged>(OnClientReceiveEntityDamaged);
            NetworkClient.RegisterHandler<MsgEntityDeath>(OnClientReceiveEntityDeath);
            NetworkClient.RegisterHandler<MsgLootSync>(OnClientReceiveLootSync);
            NetworkClient.RegisterHandler<MsgSpawnLoot>(OnClientReceiveSpawnLoot);
            NetworkClient.RegisterHandler<MsgDespawnLoot>(OnClientReceiveDespawnLoot);
            NetworkClient.RegisterHandler<MsgWorldTimeSync>(OnClientReceiveWorldTimeSync);
            NetworkClient.RegisterHandler<MsgWeatherSync>(OnClientReceiveWeatherSync);
            NetworkClient.RegisterHandler<MsgPartyRestNotification>(OnClientReceivePartyRestNotification);
            NetworkClient.RegisterHandler<MsgPartyRestVotePrompt>(OnClientReceivePartyRestVotePrompt);
            NetworkClient.RegisterHandler<MsgPartyRestVoteResult>(OnClientReceivePartyRestVoteResult);
            NetworkClient.RegisterHandler<MsgPartyRestVoteUpdate>(OnClientReceivePartyRestVoteUpdate);
            NetworkClient.RegisterHandler<MsgPartyVoteUpdate>(OnClientReceivePartyVoteUpdate);
            NetworkClient.RegisterHandler<MsgPartyVoteResult>(OnClientReceivePartyVoteResult);
            NetworkClient.RegisterHandler<MsgPartyTravelExecute>(OnClientReceivePartyTravelExecute);
            NetworkClient.RegisterHandler<MsgPartyQuestStart>(OnClientReceivePartyQuestStart);
            NetworkClient.RegisterHandler<MsgPartyQuestComplete>(OnClientReceivePartyQuestComplete);
            NetworkClient.RegisterHandler<MsgFactionRepSync>(OnClientReceiveFactionRepSync);
            NetworkClient.RegisterHandler<MsgRequestReputationReport>(OnClientReceiveRequestReputationReport);
            NetworkClient.RegisterHandler<MsgShareMapData>(OnClientReceiveShareMapData);
            NetworkClient.RegisterHandler<MsgDialogueSpeech>(OnClientReceiveDialogueSpeech);
            NetworkClient.RegisterHandler<MsgDialogueEnded>(OnClientReceiveDialogueEnded);
            NetworkClient.RegisterHandler<MsgPlayerDamaged>(OnClientReceivePlayerDamaged);
            NetworkClient.RegisterHandler<MsgTradeInvite>(OnClientReceiveTradeInvite);
            NetworkClient.RegisterHandler<MsgTradeInviteResponse>(OnClientReceiveTradeInviteResponse);
            NetworkClient.RegisterHandler<MsgTradeStateSync>(OnClientReceiveTradeStateSync);
            NetworkClient.RegisterHandler<MsgTradeCancel>(OnClientReceiveTradeCancel);
            NetworkClient.RegisterHandler<MsgTradeCompleted>(OnClientReceiveTradeCompleted);
            NetworkClient.RegisterHandler<MsgPopulationSync>(OnClientReceivePopulationSync);
            NetworkClient.RegisterHandler<MsgCastSpell>(OnClientReceiveCastSpell);
            NetworkClient.RegisterHandler<MsgApplySpellBundle>(OnClientReceiveApplySpellBundle);
            NetworkClient.RegisterHandler<MsgPvpStateSync>(OnClientReceivePvpStateSync);
            NetworkClient.RegisterHandler<MsgArrowShot>(OnClientReceiveArrowShot);
            NetworkClient.RegisterHandler<MsgTeachSpell>(OnClientReceiveTeachSpell);
            NetworkClient.RegisterHandler<MsgLobbyStateSync>(OnClientReceiveLobbyStateSync);
            NetworkClient.RegisterHandler<MsgLobbyLaunchSession>(OnClientReceiveLobbyLaunchSession);
        }

        public override void OnStopClient()
        {
            NetworkClient.UnregisterHandler<MsgSpawnEnemy>();
            NetworkClient.UnregisterHandler<MsgDespawnEnemy>();
            NetworkClient.UnregisterHandler<MsgEntityTransform>();
            NetworkClient.UnregisterHandler<MsgPlayerInit>();
            NetworkClient.UnregisterHandler<MsgPlayerProfile>();
            NetworkClient.UnregisterHandler<MsgPlayerTransform>();
            NetworkClient.UnregisterHandler<MsgPlayerTransition>();
            NetworkClient.UnregisterHandler<MsgPlayerDespawn>();
            NetworkClient.UnregisterHandler<MsgEntityDamaged>();
            NetworkClient.UnregisterHandler<MsgEntityDeath>();
            NetworkClient.UnregisterHandler<MsgPlayerDamaged>();
            NetworkClient.UnregisterHandler<MsgLootSync>();
            NetworkClient.UnregisterHandler<MsgSpawnLoot>();
            NetworkClient.UnregisterHandler<MsgDespawnLoot>();
            NetworkClient.UnregisterHandler<MsgWorldTimeSync>();
            NetworkClient.UnregisterHandler<MsgWeatherSync>();
            NetworkClient.UnregisterHandler<MsgPartyRestNotification>();
            NetworkClient.UnregisterHandler<MsgPartyRestVotePrompt>();
            NetworkClient.UnregisterHandler<MsgPartyRestVoteResult>();
            NetworkClient.UnregisterHandler<MsgPartyRestVoteUpdate>();
            NetworkClient.UnregisterHandler<MsgPartyVoteUpdate>();
            NetworkClient.UnregisterHandler<MsgPartyVoteResult>();
            NetworkClient.UnregisterHandler<MsgPartyTravelExecute>();
            NetworkClient.UnregisterHandler<MsgPartyQuestStart>();
            NetworkClient.UnregisterHandler<MsgPartyQuestComplete>();
            NetworkClient.UnregisterHandler<MsgFactionRepSync>();
            NetworkClient.UnregisterHandler<MsgRequestReputationReport>();
            NetworkClient.UnregisterHandler<MsgShareMapData>();
            NetworkClient.UnregisterHandler<MsgDialogueSpeech>();
            NetworkClient.UnregisterHandler<MsgDialogueEnded>();
            NetworkClient.UnregisterHandler<MsgTradeInvite>();
            NetworkClient.UnregisterHandler<MsgTradeInviteResponse>();
            NetworkClient.UnregisterHandler<MsgTradeStateSync>();
            NetworkClient.UnregisterHandler<MsgTradeCancel>();
            NetworkClient.UnregisterHandler<MsgTradeCompleted>();
            NetworkClient.UnregisterHandler<MsgPopulationSync>();
            NetworkClient.UnregisterHandler<MsgCastSpell>();
            NetworkClient.UnregisterHandler<MsgApplySpellBundle>();
            NetworkClient.UnregisterHandler<MsgPvpStateSync>();
            NetworkClient.UnregisterHandler<MsgArrowShot>();
            NetworkClient.UnregisterHandler<MsgTeachSpell>();
            NetworkClient.UnregisterHandler<MsgLobbyStateSync>();
            NetworkClient.UnregisterHandler<MsgLobbyLaunchSession>();
            activeDialogueBubbles.Clear();
            ClosePartyVoteWindow();
            showPlayerInteractionWindow = false;
            interactionTargetPuppet = null;
            showTradeWindow = false;
            showTradeInvitePrompt = false;
            ResetTradeOfferState();
            remotePlayerReputations.Clear();
            selectedRepPlayerIndex = 0;
            isPartyRestingBlackout = false;
            partyRestHours = 0;
            DestroyClientProxies();
            DestroyAllRemotePlayerPuppets();
            DFUNetworkLootRegistry.Clear();
            localConnectionId = NetworkSession.IsHost ? 0 : -1;
            base.OnStopClient();
        }

        public override void OnClientConnect()
        {
            if (!autoCreatePlayer)
            {
                if (!NetworkClient.ready)
                    NetworkClient.Ready();
            }
            else
            {
                base.OnClientConnect();
            }

            LogNetwork($"[DFU Network] Successfully connected to server at {networkAddress}.");
            
            // Pure clients must never retain local offline single-player enemies
            if (!NetworkSession.IsHost)
            {
                localConnectionId = -1; // Wait for server MsgPlayerInit assignment
                PurgeLocalSceneEnemies();
                if (NetworkClient.isConnected && NetworkClient.connection != null)
                {
                    MsgPlayerProfile myProfile = BuildLocalPlayerProfile(-1);
                    NetworkClient.Send(myProfile);
                    SendLocalLobbyUpdate();
                }
            }

            OnClientConnectedToHostEvent?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            LogNetwork("[DFU Network] Disconnected from server.");
            OnClientDisconnectedFromHostEvent?.Invoke();
            base.OnClientDisconnect();
        }

        public override void OnClientError(Exception exception)
        {
            LogNetwork($"[DFU Network] Client error: {exception.Message}");
            base.OnClientError(exception);
        }

        #endregion

        #region Authoritative Entity Synchronization

        private void OnServerEnemySpawned(GameObject go)
        {
            if (!NetworkSession.IsServer || go == null)
                return;

            NativeNetworkEntity netEntity = go.GetComponent<NativeNetworkEntity>();
            if (netEntity == null)
            {
                netEntity = go.AddComponent<NativeNetworkEntity>();
            }

            SetupDemoEnemy setup = go.GetComponent<SetupDemoEnemy>();
            DaggerfallEnemy dfEnemy = go.GetComponent<DaggerfallEnemy>();
            MobileTypes mobileType = setup != null ? setup.EnemyType : MobileTypes.None;
            MobileGender mobileGender = setup != null ? setup.EnemyGender : MobileGender.Unspecified;
            MobileReactions reaction = setup != null ? setup.EnemyReaction : MobileReactions.Hostile;
            ulong loadID = dfEnemy != null ? dfEnemy.LoadID : 0;

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            bool isInside = pee != null && pee.IsPlayerInside;
            bool isInsideDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int bKey = (pee != null && pee.BuildingDiscoveryData.buildingKey != 0) ? pee.BuildingDiscoveryData.buildingKey : 0;
            bool isQuest = (dfEnemy != null && dfEnemy.QuestSpawn);

            if (netEntity.entityId == Guid.Empty)
            {
                netEntity.Initialize(Guid.NewGuid(), go.name, mobileType, mobileGender, reaction, loadID, proxy: false, isInside, isInsideDungeon, bKey, isQuest);
            }
            else
            {
                netEntity.entityName = go.name;
                netEntity.mobileType = mobileType;
                netEntity.mobileGender = mobileGender;
                netEntity.reaction = reaction;
                netEntity.loadID = loadID;
                netEntity.isInside = isInside;
                netEntity.isInsideDungeon = isInsideDungeon;
                netEntity.buildingKey = bKey;
                netEntity.isQuestSpawn = isQuest;
                netEntity.isClientProxy = false;
                DFUNetworkRegistry.RegisterEntity(netEntity.entityId, go);
            }

            // Attach and configure NativeNetworkTransform for server transform streaming
            NativeNetworkTransform netTrans = go.GetComponent<NativeNetworkTransform>();
            if (netTrans == null)
                netTrans = go.AddComponent<NativeNetworkTransform>();
            netTrans.netEntity = netEntity;
            netTrans.isClientProxy = false;

            if (NetworkServer.active)
            {
                LogSpawn($"[DFU Spawn] Server broadcasting spawn for {netEntity.entityName} (GUID: {netEntity.entityId})");
                NetworkServer.SendToAll(netEntity.ToSpawnMessage());
            }
        }

        private void RegisterExistingSceneEnemies()
        {
            DaggerfallEnemy[] enemies = FindObjectsOfType<DaggerfallEnemy>();
            if (enemies == null || enemies.Length == 0)
                return;

            int registered = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    OnServerEnemySpawned(enemy.gameObject);
                    registered++;
                }
            }

            if (registered > 0)
            {
                LogSpawn($"[DFU Spawn] Server registered {registered} pre-existing scene enemies.");
            }
        }

        private void PurgeLocalSceneEnemies()
        {
            if (NetworkSession.IsHost)
                return;

            DaggerfallEnemy[] enemies = FindObjectsOfType<DaggerfallEnemy>();
            if (enemies == null || enemies.Length == 0)
                return;

            int purged = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    NativeNetworkEntity netEntity = enemy.GetComponent<NativeNetworkEntity>();
                    if (netEntity == null || !netEntity.isClientProxy)
                    {
                        Destroy(enemy.gameObject);
                        purged++;
                    }
                }
            }

            if (purged > 0)
            {
                LogSpawn($"[DFU Spawn] Purged {purged} pre-existing offline local enemies on client connect.");
            }
        }

        private void OnClientReceiveSpawnEnemy(MsgSpawnEnemy msg)
        {
            // Host already possesses the authoritative entity locally. Never spawn a proxy on Host!
            if (NetworkSession.IsHost)
                return;

            if (DFUNetworkRegistry.TryGetEntity(msg.entityId, out GameObject existing))
            {
                if (existing != null)
                {
                    NativeNetworkEntity netEnt = existing.GetComponent<NativeNetworkEntity>();
                    if (netEnt != null)
                    {
                        netEnt.loadID = msg.loadID;
                        netEnt.isInside = msg.isInside;
                        netEnt.isInsideDungeon = msg.isInsideDungeon;
                        netEnt.buildingKey = msg.buildingKey;
                        netEnt.isQuestSpawn = msg.isQuestSpawn;
                        if (msg.loadID != 0)
                        {
                            DFUNetworkRegistry.Register(msg.loadID, netEnt);
                        }
                    }
                    DaggerfallEnemy enemy = existing.GetComponent<DaggerfallEnemy>();
                    if (enemy != null && msg.loadID != 0)
                    {
                        enemy.LoadID = msg.loadID;
                        enemy.QuestSpawn = msg.isQuestSpawn;
                    }
                    RefreshAllClientEnemyProxies();
                    return;
                }
            }

            LogSpawn($"[DFU Spawn] Client received MsgSpawnEnemy: {msg.entityName} (GUID: {msg.entityId})");
            GameObject proxy = GameObjectHelper.CreateClientEnemyProxy(msg);
            if (proxy != null)
            {
                DFUNetworkRegistry.RegisterEntity(msg.entityId, proxy);
                RefreshAllClientEnemyProxies();
            }
        }

        private void OnClientReceiveDespawnEnemy(MsgDespawnEnemy msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (DFUNetworkRegistry.TryGetEntity(msg.entityId, out GameObject go))
            {
                DFUNetworkRegistry.UnregisterEntity(msg.entityId);
                if (go != null)
                {
                    LogSpawn($"[DFU Spawn] Client despawning proxy (GUID: {msg.entityId})");
                    Destroy(go);
                }
            }
        }

        private void DestroyClientProxies()
        {
            List<Guid> toRemove = new List<Guid>();
            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value != null)
                {
                    NativeNetworkEntity netEntity = kvp.Value.GetComponent<NativeNetworkEntity>();
                    if (netEntity != null && netEntity.isClientProxy)
                    {
                        Destroy(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var guid in toRemove)
            {
                DFUNetworkRegistry.UnregisterEntity(guid);
            }
        }

        public void RefreshAllClientEnemyProxies()
        {
            if (NetworkSession.IsHost)
                return;

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            bool localInside = pee != null && pee.IsPlayerInside;
            bool localDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int localBuildingKey = (pee != null && pee.IsPlayerInsideBuilding) ? pee.BuildingDiscoveryData.buildingKey : 0;

            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value == null) continue;
                NativeNetworkEntity netEnt = kvp.Value.GetComponent<NativeNetworkEntity>();
                if (netEnt == null || !netEnt.isClientProxy) continue;

                bool sameCell = false;
                Transform bestParent = null;

                if (!localInside && !netEnt.isInside)
                {
                    sameCell = true;
                    bestParent = GameObjectHelper.GetBestParent();
                }
                else if (localDungeon && netEnt.isInsideDungeon)
                {
                    sameCell = true;
                    bestParent = (pee != null && pee.Dungeon != null) ? pee.Dungeon.transform : GameObjectHelper.GetBestParent();
                }
                else if (localInside && !localDungeon && netEnt.isInside && !netEnt.isInsideDungeon)
                {
                    sameCell = (localBuildingKey == netEnt.buildingKey);
                    bestParent = (pee != null && pee.Interior != null) ? pee.Interior.transform : GameObjectHelper.GetBestParent();
                }

                if (bestParent != null && kvp.Value.transform.parent != bestParent)
                {
                    kvp.Value.transform.SetParent(bestParent, true);
                }

                kvp.Value.SetActive(sameCell);
            }
        }

        #endregion

        #region Player Registry for Multi-Target AI

        private static readonly List<DaggerfallEntityBehaviour> connectedPlayerEntities = new List<DaggerfallEntityBehaviour>();

        /// <summary>
        /// Read-only list of all currently connected player entities on the server (Host + Client puppets).
        /// </summary>
        public static IReadOnlyList<DaggerfallEntityBehaviour> ConnectedPlayers => connectedPlayerEntities;

        /// <summary>
        /// Registers a player entity in the server's multi-target registry.
        /// </summary>
        public static void RegisterPlayerEntity(DaggerfallEntityBehaviour entity)
        {
            if (entity != null && !connectedPlayerEntities.Contains(entity))
            {
                connectedPlayerEntities.Add(entity);
            }
        }

        /// <summary>
        /// Unregisters a player entity from the server's multi-target registry.
        /// </summary>
        public static void UnregisterPlayerEntity(DaggerfallEntityBehaviour entity)
        {
            if (entity != null)
            {
                connectedPlayerEntities.Remove(entity);
            }
        }

        /// <summary>
        /// Clears all registered players.
        /// </summary>
        public static void ClearPlayerEntities()
        {
            connectedPlayerEntities.Clear();
        }

        /// <summary>
        /// Checks if the given entity behaviour belongs to any player (Host or remote client puppet).
        /// </summary>
        public static bool IsAnyPlayer(DaggerfallEntityBehaviour entity)
        {
            if (entity == null) return false;
            if (GameManager.Instance != null && entity == GameManager.Instance.PlayerEntityBehaviour) return true;
            if (entity.GetComponent<NativePlayerPuppet>() != null) return true;
            return connectedPlayerEntities.Contains(entity);
        }

        #endregion

        #region Remote Player Replication & Transform Streaming

        private readonly Dictionary<int, MsgPlayerProfile> connectedPlayerProfiles = new Dictionary<int, MsgPlayerProfile>();
        public readonly Dictionary<int, NativePlayerPuppet> remotePlayerPuppets = new Dictionary<int, NativePlayerPuppet>();
        public Dictionary<int, NativePlayerPuppet> RemotePlayerPuppets => remotePlayerPuppets;

        private float playerStreamTimer = 0f;
        private Vector3 lastLocalPlayerPos;
        private float lastLocalPlayerYaw;
        private bool lastLocalPlayerCrouching;
        private bool lastLocalPlayerAttacking;
        private int lastLocalPlayerWeaponType;
        private bool lastLocalPlayerInside;
        private bool lastLocalPlayerInsideDungeon;
        private int lastLocalPlayerBuildingKey;
        private bool lastLocalPlayerInvisible;
        private float playerHeartbeatTimer = 0f;
        private bool isSyncingTransition = false;

        private void StreamLocalPlayerTransform()
        {
            GameObject player = GameManager.Instance.PlayerObject;
            if (player == null)
                return;

            playerStreamTimer += Time.deltaTime;
            playerHeartbeatTimer += Time.deltaTime;
            if (playerStreamTimer < 0.05f) // 20 Hz
                return;

            playerStreamTimer = 0f;

            Vector3 currentPos = player.transform.position;
            float currentYaw = player.transform.eulerAngles.y;

            bool isCrouching = GameManager.Instance.PlayerMotor != null && GameManager.Instance.PlayerMotor.IsCrouching;
            bool isAttacking = GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null && GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking();
            int weaponType = (int)(GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null ? GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType : WeaponTypes.None);

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            bool isInside = pee != null && pee.IsPlayerInside;
            bool isInsideDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int buildingKey = (pee != null && pee.IsPlayerInsideBuilding) ? pee.BuildingDiscoveryData.buildingKey : 0;
            bool isInvisible = isDmInvisible || (GameManager.Instance.PlayerEntity != null && (GameManager.Instance.PlayerEntity.MagicalConcealmentFlags.HasFlag(MagicalConcealmentFlags.InvisibleTrue) || GameManager.Instance.PlayerEntity.IsInvisible));

            bool posChanged = Vector3.Distance(currentPos, lastLocalPlayerPos) > 0.02f;
            bool rotChanged = Mathf.Abs(Mathf.DeltaAngle(currentYaw, lastLocalPlayerYaw)) > 1.5f;
            bool actionChanged = (isCrouching != lastLocalPlayerCrouching) || (isAttacking != lastLocalPlayerAttacking);
            bool cellChanged = (isInside != lastLocalPlayerInside) || (isInsideDungeon != lastLocalPlayerInsideDungeon) || (buildingKey != lastLocalPlayerBuildingKey);
            bool invisChanged = (isInvisible != lastLocalPlayerInvisible);
            bool heartbeat = playerHeartbeatTimer >= 0.5f;

            if (posChanged || rotChanged || actionChanged || cellChanged || invisChanged || heartbeat)
            {
                playerHeartbeatTimer = 0f;
                lastLocalPlayerPos = currentPos;
                lastLocalPlayerYaw = currentYaw;
                lastLocalPlayerCrouching = isCrouching;
                lastLocalPlayerAttacking = isAttacking;
                lastLocalPlayerWeaponType = weaponType;
                lastLocalPlayerInside = isInside;
                lastLocalPlayerInsideDungeon = isInsideDungeon;
                lastLocalPlayerBuildingKey = buildingKey;
                lastLocalPlayerInvisible = isInvisible;

                bool isMoving = posChanged;

                MsgPlayerTransform msg = new MsgPlayerTransform
                {
                    connectionId = NetworkSession.IsHost ? 0 : localConnectionId,
                    position = currentPos,
                    yaw = currentYaw,
                    isMoving = isMoving,
                    isCrouching = isCrouching,
                    isAttacking = isAttacking,
                    weaponType = weaponType,
                    isInside = isInside,
                    isInsideDungeon = isInsideDungeon,
                    buildingKey = buildingKey,
                    isInvisible = isInvisible
                };

                if (NetworkSession.IsHost)
                {
                    NetworkServer.SendToAll(msg);
                }
                else if (NetworkClient.isConnected && NetworkClient.connection != null)
                {
                    NetworkClient.Send(msg);
                }
            }
        }

        public MsgPlayerProfile BuildLocalPlayerProfile(int connId)
        {
            GameObject player = GameManager.Instance.PlayerObject;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;
            float yaw = player != null ? player.transform.eulerAngles.y : 0f;

            string pName = "Adventurer";
            int gender = 0;
            string career = "Warrior";
            string race = "Breton";

            if (GameManager.Instance.PlayerEntity != null)
            {
                pName = GameManager.Instance.PlayerEntity.Name;
                gender = GameManager.Instance.PlayerEntity.Gender == DaggerfallWorkshop.Game.Entity.Genders.Male ? 0 : 1;
                if (GameManager.Instance.PlayerEntity.Career != null)
                    career = GameManager.Instance.PlayerEntity.Career.Name;
                race = GameManager.Instance.PlayerEntity.Race.ToString();
            }

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            bool isInside = pee != null && pee.IsPlayerInside;
            bool isInsideDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int bKey = (pee != null && pee.BuildingDiscoveryData.buildingKey != 0) ? pee.BuildingDiscoveryData.buildingKey : 0;
            bool isDm = (connId == 0 && (NetworkSession.IsServer || localLobbyIsDm)) || (localConnectionId == connId && localLobbyIsDm);

            return new MsgPlayerProfile
            {
                connectionId = connId,
                playerName = pName,
                gender = gender,
                careerName = career,
                raceName = race,
                position = pos,
                yaw = yaw,
                isInside = isInside,
                isInsideDungeon = isInsideDungeon,
                buildingKey = bKey,
                isDm = isDm
            };
        }

        private void SpawnRemotePlayerPuppet(MsgPlayerProfile profile)
        {
            int selfConnId = NetworkSession.IsHost ? 0 : localConnectionId;
            if (profile.connectionId == selfConnId || (selfConnId != -1 && profile.connectionId == selfConnId))
                return;

            if (remotePlayerPuppets.ContainsKey(profile.connectionId))
            {
                if (remotePlayerPuppets[profile.connectionId] != null)
                    return;
                remotePlayerPuppets.Remove(profile.connectionId);
            }

            LogEntity($"[DFU Player] Spawning remote player avatar for '{profile.playerName}' (ConnId: {profile.connectionId})");
            GameObject puppetGo = GameObjectHelper.CreateRemotePlayerProxy(profile);
            if (puppetGo != null)
            {
                NativePlayerPuppet puppet = puppetGo.GetComponent<NativePlayerPuppet>();
                remotePlayerPuppets[profile.connectionId] = puppet;

                DaggerfallEntityBehaviour entity = puppetGo.GetComponent<DaggerfallEntityBehaviour>();
                if (entity != null)
                {
                    RegisterPlayerEntity(entity);
                }
            }
        }

        private void DespawnRemotePlayerPuppet(int connectionId)
        {
            if (remotePlayerPuppets.TryGetValue(connectionId, out NativePlayerPuppet puppet))
            {
                remotePlayerPuppets.Remove(connectionId);
                if (puppet != null && puppet.gameObject != null)
                {
                    DaggerfallEntityBehaviour entity = puppet.GetComponent<DaggerfallEntityBehaviour>();
                    if (entity != null)
                    {
                        UnregisterPlayerEntity(entity);
                    }
                    LogEntity($"[DFU Player] Despawning avatar for connection {connectionId}.");
                    Destroy(puppet.gameObject);
                }
            }
            connectedPlayerProfiles.Remove(connectionId);
        }

        private void DestroyAllRemotePlayerPuppets()
        {
            foreach (var kvp in remotePlayerPuppets)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    DaggerfallEntityBehaviour entity = kvp.Value.GetComponent<DaggerfallEntityBehaviour>();
                    if (entity != null)
                    {
                        UnregisterPlayerEntity(entity);
                    }
                    Destroy(kvp.Value.gameObject);
                }
            }
            remotePlayerPuppets.Clear();
            connectedPlayerProfiles.Clear();
        }

        // Server handlers
        private void OnServerReceivePlayerProfile(NetworkConnection conn, MsgPlayerProfile msg)
        {
            msg.connectionId = conn.connectionId;
            connectedPlayerProfiles[conn.connectionId] = msg;
            LogEntity($"[DFU Player] Server registered profile for '{msg.playerName}' (ConnId: {conn.connectionId})");

            conn.Send(new MsgPlayerInit { assignedConnectionId = conn.connectionId });

            if (NetworkSession.IsHost)
            {
                SpawnRemotePlayerPuppet(msg);
            }

            foreach (var client in connectedClients)
            {
                if (client.connectionId != conn.connectionId)
                    client.Send(msg);
            }

            foreach (var kvp in connectedPlayerProfiles)
            {
                if (kvp.Key != conn.connectionId)
                    conn.Send(kvp.Value);
            }

            // Synchronize all existing active server entities to new connecting client
            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value != null && kvp.Value.activeInHierarchy)
                {
                    NativeNetworkEntity netEnt = kvp.Value.GetComponent<NativeNetworkEntity>();
                    if (netEnt != null && !netEnt.isClientProxy)
                    {
                        conn.Send(netEnt.ToSpawnMessage());
                    }
                }
            }
        }

        private void UpdatePuppetCellVisibility(NativePlayerPuppet puppet, MsgPlayerTransform msg)
        {
            if (puppet == null) return;

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            bool localInside = pee != null && pee.IsPlayerInside;
            bool localDungeon = pee != null && pee.IsPlayerInsideDungeon;
            int localBuildingKey = (pee != null && pee.IsPlayerInsideBuilding) ? pee.BuildingDiscoveryData.buildingKey : 0;

            bool sameContext = false;
            if (!localInside && !msg.isInside)
            {
                sameContext = true; // Both in exterior
            }
            else if (localDungeon && msg.isInsideDungeon)
            {
                sameContext = true; // Both in dungeon
            }
            else if (localInside && !localDungeon && msg.isInside && !msg.isInsideDungeon)
            {
                sameContext = (localBuildingKey == msg.buildingKey); // Both in same interior building
            }

            puppet.SetCellVisibility(sameContext && !msg.isInvisible);
        }

        private void OnServerReceivePlayerTransform(NetworkConnection conn, MsgPlayerTransform msg)
        {
            msg.connectionId = conn.connectionId;

            if (!remotePlayerPuppets.TryGetValue(conn.connectionId, out NativePlayerPuppet puppet) || puppet == null || puppet.gameObject == null)
            {
                if (connectedPlayerProfiles.TryGetValue(conn.connectionId, out MsgPlayerProfile profile))
                {
                    LogEntity($"[DFU Puppet] Auto-recovering missing puppet for connection {conn.connectionId} ({profile.playerName}).");
                    profile.position = msg.position;
                    profile.yaw = msg.yaw;
                    SpawnRemotePlayerPuppet(profile);
                    remotePlayerPuppets.TryGetValue(conn.connectionId, out puppet);
                }
            }

            if (puppet != null)
            {
                puppet.ApplyTransform(msg.position, msg.yaw, msg.isMoving, msg.isCrouching, msg.isAttacking, msg.weaponType);
                UpdatePuppetCellVisibility(puppet, msg);
            }

            foreach (var client in connectedClients)
            {
                if (client.connectionId != conn.connectionId)
                    client.Send(msg);
            }
        }

        // Client handlers
        private void OnClientReceiveEntityTransform(MsgEntityTransform msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (DFUNetworkRegistry.TryGetEntity(msg.entityId, out GameObject go) && go != null)
            {
                NativeNetworkTransform netTrans = go.GetComponent<NativeNetworkTransform>();
                if (netTrans != null)
                {
                    netTrans.ApplySnapshot(msg.position, msg.yaw, msg.animState);
                }
            }
        }

        private void HandleSaveLoadManagerOnLoad(SaveData_v1 saveData)
        {
            if (NetworkSession.IsActive)
            {
                RefreshLocalAndRemotePuppetsAfterWorldLoad();
            }
        }

        private void HandleStartGameBehaviourOnStartGame(object sender, EventArgs e)
        {
            if (NetworkSession.IsActive)
            {
                RefreshLocalAndRemotePuppetsAfterWorldLoad();
            }
        }

        private void RefreshLocalAndRemotePuppetsAfterWorldLoad()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.ClearAllActions();
                InputManager.Instance.ToggleAutorun = false;
            }
            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }

            // Rebuild and broadcast our local character profile now that the character/save is loaded in the 3D world
            int myConnId = NetworkSession.IsHost ? 0 : localConnectionId;
            MsgPlayerProfile updatedProfile = BuildLocalPlayerProfile(myConnId);
            connectedPlayerProfiles[myConnId] = updatedProfile;

            if (NetworkSession.IsHost)
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerEntityBehaviour != null)
                {
                    RegisterPlayerEntity(GameManager.Instance.PlayerEntityBehaviour);
                }
                RegisterExistingSceneEnemies();
                NetworkServer.SendToAll(updatedProfile);
            }
            else if (NetworkClient.isConnected)
            {
                PurgeLocalSceneEnemies();
                NetworkClient.Send(updatedProfile);
            }

            // Re-spawn or re-parent all remote player puppets so they appear in the active 3D cell
            RefreshAllRemotePlayerPuppets();
            lastLocalPlayerPos = (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                ? GameManager.Instance.PlayerObject.transform.position
                : Vector3.zero;
            playerHeartbeatTimer = 1.0f;
            playerStreamTimer = 1.0f;
            StreamLocalPlayerTransform();
        }

        private void OnClientReceivePlayerInit(MsgPlayerInit msg)
        {
            localConnectionId = msg.assignedConnectionId;
            LogEntity($"[DFU Player] Assigned local connection ID: {localConnectionId}");

            // Safety cleanup: If an avatar was spawned for this connection before ID assignment, despawn it
            if (remotePlayerPuppets.ContainsKey(localConnectionId))
            {
                DespawnRemotePlayerPuppet(localConnectionId);
            }

            SendReputationReportToHost();
        }

        private void OnClientReceivePlayerProfile(MsgPlayerProfile msg)
        {
            // Always cache the profile on client so auto-recovery and world-load refresh can spawn the puppet
            connectedPlayerProfiles[msg.connectionId] = msg;

            // Never spawn an avatar for our own local connection
            int selfConnId = NetworkSession.IsHost ? 0 : localConnectionId;
            if (msg.connectionId == selfConnId || (selfConnId != -1 && msg.connectionId == selfConnId))
                return;

            // If we already have a puppet for this peer, update its visual identity / position if needed
            if (remotePlayerPuppets.TryGetValue(msg.connectionId, out NativePlayerPuppet existingPuppet) && existingPuppet != null && existingPuppet.gameObject != null)
            {
                existingPuppet.Initialize(msg);
                Transform bestParent = GameObjectHelper.GetBestPuppetParent();
                if (bestParent != null && existingPuppet.transform.parent != bestParent)
                    existingPuppet.transform.SetParent(bestParent, true);
                existingPuppet.gameObject.SetActive(true);
            }
            else
            {
                SpawnRemotePlayerPuppet(msg);
            }

            // Do not force-transition if client is currently loading a save, respawning, or launched from the pre-game lobby
            if (msg.connectionId == 0 && !NetworkSession.IsHost && localLobbySelectionMode == 0)
            {
                PlayerEnterExit pee = GameManager.Instance != null ? GameManager.Instance.PlayerEnterExit : null;
                bool isBusyLoading = pee == null || pee.IsRespawning || (GameManager.Instance.SaveLoadManager != null && GameManager.Instance.SaveLoadManager.LoadInProgress);
                if (!isBusyLoading && GameManager.Instance.StateManager.CurrentState == StateManager.StateTypes.Game)
                {
                    if (msg.isInsideDungeon && !pee.IsPlayerInsideDungeon)
                    {
                        LogTransition("[DFU Transition] Synchronizing client into host's dungeon on join.");
                        pee.TransitionDungeonInterior(null, new StaticDoor(), GameManager.Instance.PlayerGPS.CurrentLocation, false);
                        ReparentAllPuppets();
                    }
                    else if (msg.isInside && !msg.isInsideDungeon && !pee.IsPlayerInsideBuilding && msg.buildingKey != 0)
                    {
                        LogTransition($"[DFU Transition] Synchronizing client into host's building interior ({msg.buildingKey}) on join.");
                        TransitionToBuildingByKey(msg.buildingKey);
                        ReparentAllPuppets();
                    }
                }
            }
        }

        private void OnClientReceivePlayerTransform(MsgPlayerTransform msg)
        {
            int selfConnId = NetworkSession.IsHost ? 0 : localConnectionId;
            if (msg.connectionId == selfConnId || (selfConnId != -1 && msg.connectionId == selfConnId))
                return;

            if (!remotePlayerPuppets.TryGetValue(msg.connectionId, out NativePlayerPuppet puppet) || puppet == null || puppet.gameObject == null)
            {
                if (!connectedPlayerProfiles.TryGetValue(msg.connectionId, out MsgPlayerProfile profile))
                {
                    // Fallback profile if transform arrived before or without profile packet
                    profile = new MsgPlayerProfile
                    {
                        connectionId = msg.connectionId,
                        playerName = msg.connectionId == 0 ? "Host" : $"Player {msg.connectionId}",
                        gender = 0,
                        careerName = "Warrior",
                        raceName = "Breton",
                        position = msg.position,
                        yaw = msg.yaw,
                        isInside = msg.isInside,
                        isInsideDungeon = msg.isInsideDungeon,
                        buildingKey = msg.buildingKey,
                        isDm = (msg.connectionId == 0)
                    };
                    connectedPlayerProfiles[msg.connectionId] = profile;
                }
                else
                {
                    profile.position = msg.position;
                    profile.yaw = msg.yaw;
                    profile.isInside = msg.isInside;
                    profile.isInsideDungeon = msg.isInsideDungeon;
                    profile.buildingKey = msg.buildingKey;
                    connectedPlayerProfiles[msg.connectionId] = profile;
                }

                LogEntity($"[DFU Puppet] Auto-recovering missing puppet for connection {msg.connectionId} ({profile.playerName}).");
                SpawnRemotePlayerPuppet(profile);
                remotePlayerPuppets.TryGetValue(msg.connectionId, out puppet);
            }

            if (puppet != null)
            {
                Transform bestParent = GameObjectHelper.GetBestPuppetParent();
                if (bestParent != null && puppet.transform.parent != bestParent)
                {
                    puppet.transform.SetParent(bestParent, true);
                }
                puppet.ApplyTransform(msg.position, msg.yaw, msg.isMoving, msg.isCrouching, msg.isAttacking, msg.weaponType);
                UpdatePuppetCellVisibility(puppet, msg);
            }
        }

        private void OnClientReceivePlayerDespawn(MsgPlayerDespawn msg)
        {
            DespawnRemotePlayerPuppet(msg.connectionId);
        }

        private void OnServerReceiveAttackEntity(NetworkConnection conn, MsgAttackEntity msg)
        {
            NativeNetworkEntity targetNet = DFUNetworkRegistry.Get(msg.entityId);
            if (targetNet == null || targetNet.gameObject == null || !targetNet.gameObject.activeInHierarchy)
                return;

            if (!remotePlayerPuppets.TryGetValue(conn.connectionId, out NativePlayerPuppet puppet) || puppet == null)
                return;

            // Distance reach check
            float dist = Vector3.Distance(puppet.transform.position, targetNet.transform.position);
            if (dist > 6.0f)
            {
                LogEntity($"[DFU Combat] Attack rejected from conn {conn.connectionId}: target out of reach ({dist:F1}m)");
                return;
            }

            DaggerfallEntityBehaviour targetBehaviour = targetNet.GetComponent<DaggerfallEntityBehaviour>();
            if (targetBehaviour == null || !(targetBehaviour.Entity is EnemyEntity targetEntity))
                return;

            int damage = msg.damage;
            // Metal material check
            if (targetEntity.MinMetalToHit > (WeaponMaterialTypes)msg.weaponMaterial)
            {
                damage = 0;
            }

            damage = Mathf.Clamp(damage, 0, 150);

            // Apply authoritative damage
            targetEntity.DecreaseHealth(damage);

            // Switch aggro to attacking player puppet
            DaggerfallEntityBehaviour puppetBehaviour = puppet.GetComponent<DaggerfallEntityBehaviour>();
            if (puppetBehaviour != null)
            {
                EnemySenses senses = targetNet.GetComponent<EnemySenses>();
                if (senses != null)
                {
                    senses.Target = puppetBehaviour;
                    senses.SecondaryTarget = puppetBehaviour;
                    senses.OldTarget = puppetBehaviour;
                }

                EnemyMotor motor = targetNet.GetComponent<EnemyMotor>();
                if (motor != null)
                {
                    motor.MakeEnemyHostileToAttacker(puppetBehaviour);
                }
            }

            // Show blood and play hit sound on host
            if (damage > 0)
            {
                EnemyBlood blood = targetNet.GetComponent<EnemyBlood>();
                if (blood != null)
                    blood.ShowBloodSplash(targetEntity.MobileEnemy.BloodIndex, msg.hitPosition);

                EnemySounds sounds = targetNet.GetComponent<EnemySounds>();
                if (sounds != null)
                    sounds.PlayHitSound(null);
            }

            // Broadcast damage effect to all clients
            MsgEntityDamaged dmgMsg = new MsgEntityDamaged
            {
                entityId = targetNet.EntityId,
                damage = damage,
                hitPosition = msg.hitPosition,
                bloodIndex = targetEntity.MobileEnemy.BloodIndex
            };
            NetworkServer.SendToAll(dmgMsg);

            LogEntity($"[DFU Combat] Client {conn.connectionId} damaged entity {msg.entityId} for {damage} HP (Remaining: {targetEntity.CurrentHealth})");
        }

        private void OnClientReceiveEntityDamaged(MsgEntityDamaged msg)
        {
            if (NetworkSession.IsHost)
                return;

            NativeNetworkEntity netEnt = DFUNetworkRegistry.Get(msg.entityId);
            if (netEnt == null || netEnt.gameObject == null)
                return;

            if (msg.damage > 0)
            {
                EnemyBlood blood = netEnt.GetComponent<EnemyBlood>();
                if (blood != null)
                    blood.ShowBloodSplash(msg.bloodIndex, msg.hitPosition);

                EnemySounds sounds = netEnt.GetComponent<EnemySounds>();
                if (sounds != null)
                    sounds.PlayHitSound(null);
            }

            DaggerfallEntityBehaviour behaviour = netEnt.GetComponent<DaggerfallEntityBehaviour>();
            if (behaviour != null && behaviour.Entity is EnemyEntity enemy)
            {
                enemy.DecreaseHealth(msg.damage);
            }
        }

        /// <summary>
        /// Routes damage to a player across the network. If host (connId == 0), applies locally.
        /// If client (connId > 0), sends authoritative MsgPlayerDamaged to that client.
        /// </summary>
        public void SendPlayerDamage(int connId, int damage)
        {
            if (connId == 0)
            {
                if (GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.DecreaseHealth(damage);
                }
            }
            else if (NetworkServer.active)
            {
                MsgPlayerDamaged msg = new MsgPlayerDamaged
                {
                    connectionId = connId,
                    damage = damage
                };
                foreach (var client in connectedClients)
                {
                    if (client.connectionId == connId)
                    {
                        client.Send(msg);
                        break;
                    }
                }
            }
        }

        private void OnClientReceivePlayerDamaged(MsgPlayerDamaged msg)
        {
            if (msg.connectionId == localConnectionId || (!NetworkServer.active && msg.connectionId != 0))
            {
                if (GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.DecreaseHealth(msg.damage);
                }
            }
        }

        private void OnClientReceiveEntityDeath(MsgEntityDeath msg)
        {
            if (NetworkSession.IsHost)
                return;

            NativeNetworkEntity proxyNet = DFUNetworkRegistry.Get(msg.entityId);
            if (proxyNet == null)
            {
                foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
                {
                    if (kvp.Value != null)
                    {
                        NativeNetworkEntity ne = kvp.Value.GetComponent<NativeNetworkEntity>();
                        if (ne != null && (ne.EntityId == msg.entityId || (ne.loadID != 0 && ne.loadID == msg.entityId) || (msg.lootLoadID != 0 && ne.loadID == msg.lootLoadID)))
                        {
                            proxyNet = ne;
                            break;
                        }
                    }
                }
            }
            if (proxyNet == null)
            {
                float closestDist = float.MaxValue;
                foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
                {
                    if (kvp.Value != null && kvp.Value.activeInHierarchy)
                    {
                        NativeNetworkEntity ne = kvp.Value.GetComponent<NativeNetworkEntity>();
                        if (ne != null && ne.isClientProxy)
                        {
                            float d = Vector3.Distance(ne.transform.position, msg.position);
                            if (d < 3.5f && d < closestDist)
                            {
                                closestDist = d;
                                proxyNet = ne;
                            }
                        }
                    }
                }
            }
            if (proxyNet == null || proxyNet.gameObject == null)
                return;

            LogEntity($"[DFU Combat] Client received death for entity {msg.entityId} (Loot LoadID: {msg.lootLoadID})");

            // Play body fall sound at death position
            if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.DaggerfallAudioSource != null)
            {
                DaggerfallUI.Instance.DaggerfallAudioSource.PlayClipAtPoint(SoundClips.BodyFall, msg.position, 1f);
            }

            // Spawn corpse marker billboard so body rests on floor
            try
            {
                ulong assignedLoadID = msg.lootLoadID != 0 ? msg.lootLoadID : DaggerfallUnity.NextUID;
                DaggerfallLoot corpseLoot = GameObjectHelper.CreateLootableCorpseMarker(
                    GameManager.Instance.PlayerObject,
                    proxyNet.gameObject,
                    proxyNet.GetComponent<DaggerfallEntityBehaviour>()?.Entity as EnemyEntity,
                    msg.corpseTexture,
                    assignedLoadID);

                if (corpseLoot != null)
                {
                    corpseLoot.LoadID = assignedLoadID;
                    if (!string.IsNullOrEmpty(msg.itemsJson))
                    {
                        ItemData_v1[] itemArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), msg.itemsJson);
                        if (itemArray != null)
                        {
                            corpseLoot.Items.DeserializeItems(itemArray);
                        }
                    }
                    DFUNetworkLootRegistry.Register(corpseLoot.LoadID, corpseLoot);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU Combat] Could not create client corpse marker for entity {msg.entityId}: {ex.Message}");
            }

            // If proxy is linked to a quest foe, increment kills on client quest
            QuestResourceBehaviour qrb = proxyNet.GetComponent<QuestResourceBehaviour>();
            if (qrb != null && qrb.TargetResource is Foe questFoe)
            {
                questFoe.IncrementKills();
                LogEntity($"[DFU Combat] Client quest foe killed, incremented kills for {questFoe.Symbol.Name} on quest {qrb.QuestUID}.");
            }

            // Deactivate proxy gameobject and unregister
            proxyNet.gameObject.SetActive(false);
            DFUNetworkRegistry.Unregister(msg.entityId);
            if (proxyNet.EntityId != 0 && proxyNet.EntityId != msg.entityId)
                DFUNetworkRegistry.Unregister(proxyNet.EntityId);
            if (proxyNet.entityId != Guid.Empty)
                DFUNetworkRegistry.UnregisterEntity(proxyNet.entityId);
        }

        private void OnServerReceiveRequestLootSync(NetworkConnection conn, MsgRequestLootSync msg)
        {
            DaggerfallLoot loot = DFUNetworkLootRegistry.Get(msg.loadID);
            if (loot == null || loot.Items == null)
                return;

            ItemData_v1[] itemArray = loot.Items.SerializeItems();
            string itemsJson = SaveLoadManager.Serialize(typeof(ItemData_v1[]), itemArray, false);
            conn.Send(new MsgLootSync
            {
                loadID = msg.loadID,
                itemsJson = itemsJson
            });
        }

        private void OnServerReceiveLootTakeItem(NetworkConnection conn, MsgLootTakeItem msg)
        {
            DaggerfallLoot loot = DFUNetworkLootRegistry.Get(msg.loadID);
            if (loot == null || loot.Items == null)
                return;

            DaggerfallUnityItem targetItem = loot.Items.GetItem(msg.itemUID);
            if (targetItem == null && msg.templateIndex > 0)
            {
                // Fallback for gold or split stacks where UID differed
                targetItem = loot.Items.GetItem((ItemGroups)msg.itemGroup, msg.templateIndex);
            }

            if (targetItem != null)
            {
                if (msg.stackCount > 0 && targetItem.stackCount > msg.stackCount)
                {
                    targetItem.stackCount -= msg.stackCount;
                }
                else
                {
                    loot.Items.RemoveItem(targetItem);
                }
                LogLoot($"[DFU Loot] Conn {conn.connectionId} took item '{GetSafeItemName(targetItem)}' from container {msg.loadID}. Remaining: {loot.Items.Count}");
            }
            else
            {
                LogLoot($"[DFU Loot] Item UID {msg.itemUID} not found in container {msg.loadID}.");
            }

            // Refresh Host UI if Host has this container open
            if (DaggerfallUI.Instance != null &&
                DaggerfallUI.Instance.InventoryWindow != null &&
                DaggerfallUI.Instance.InventoryWindow.IsOpen &&
                DaggerfallUI.Instance.InventoryWindow.LootTarget == loot)
            {
                DaggerfallUI.Instance.InventoryWindow.Refresh(false);
            }

            // Broadcast updated container state to all peers
            ItemData_v1[] itemArray = loot.Items.SerializeItems();
            string itemsJson = SaveLoadManager.Serialize(typeof(ItemData_v1[]), itemArray, false);
            NetworkServer.SendToAll(new MsgLootSync
            {
                loadID = loot.LoadID,
                itemsJson = itemsJson
            });
        }

        private void OnServerReceiveLootDropItem(NetworkConnection conn, MsgLootDropItem msg)
        {
            DaggerfallLoot loot = DFUNetworkLootRegistry.Get(msg.loadID);
            if (loot == null || loot.Items == null)
                return;

            if (!string.IsNullOrEmpty(msg.itemJson))
            {
                ItemData_v1 itemData = (ItemData_v1)SaveLoadManager.Deserialize(typeof(ItemData_v1), msg.itemJson);
                if (itemData != null)
                {
                    DaggerfallUnityItem droppedItem = new DaggerfallUnityItem(itemData);
                    loot.Items.AddItem(droppedItem);
                    LogLoot($"[DFU Loot] Conn {conn.connectionId} placed item '{GetSafeItemName(droppedItem)}' (UID: {droppedItem.UID}) into container {msg.loadID}. Total: {loot.Items.Count}");
                }
            }

            // Refresh Host UI if Host has this container open
            if (DaggerfallUI.Instance != null &&
                DaggerfallUI.Instance.InventoryWindow != null &&
                DaggerfallUI.Instance.InventoryWindow.IsOpen &&
                DaggerfallUI.Instance.InventoryWindow.LootTarget == loot)
            {
                DaggerfallUI.Instance.InventoryWindow.Refresh(false);
            }

            // Broadcast updated container state to all peers
            ItemData_v1[] itemArray = loot.Items.SerializeItems();
            string itemsJson = SaveLoadManager.Serialize(typeof(ItemData_v1[]), itemArray, false);
            NetworkServer.SendToAll(new MsgLootSync
            {
                loadID = loot.LoadID,
                itemsJson = itemsJson
            });
        }

        private void OnClientReceiveLootSync(MsgLootSync msg)
        {
            if (NetworkSession.IsHost)
                return;

            DaggerfallLoot loot = DFUNetworkLootRegistry.Get(msg.loadID);
            if (loot == null)
                return;

            if (!string.IsNullOrEmpty(msg.itemsJson))
            {
                ItemData_v1[] itemArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), msg.itemsJson);
                if (itemArray != null)
                {
                    loot.Items.DeserializeItems(itemArray);
                }
            }
            else
            {
                loot.Items.Clear();
            }

            // Refresh open inventory window if inspecting this container
            if (DaggerfallUI.Instance != null &&
                DaggerfallUI.Instance.InventoryWindow != null &&
                DaggerfallUI.Instance.InventoryWindow.IsOpen &&
                DaggerfallUI.Instance.InventoryWindow.LootTarget == loot)
            {
                DaggerfallUI.Instance.InventoryWindow.Refresh(false);
            }
        }

        private void OnClientReceiveSpawnLoot(MsgSpawnLoot msg)
        {
            if (NetworkSession.IsHost)
                return;

            DaggerfallLoot existing = DFUNetworkLootRegistry.Get(msg.loadID);
            if (existing != null)
                return;

            Transform parent = GameManager.Instance.IsPlayerInside ?
                (GameManager.Instance.IsPlayerInsideDungeon ? GameManager.Instance.PlayerEnterExit.Dungeon?.transform : GameManager.Instance.PlayerEnterExit.Interior?.transform) :
                GameManager.Instance.StreamingTarget?.transform;

            DaggerfallLoot loot = GameObjectHelper.CreateLootContainer(
                (LootContainerTypes)msg.containerType,
                (InventoryContainerImages)msg.containerImage,
                msg.position,
                parent,
                msg.textureArchive,
                msg.textureRecord,
                msg.loadID);

            if (loot != null)
            {
                loot.LoadID = msg.loadID;
                loot.entityName = msg.entityName;
                loot.isEnemyClass = msg.isEnemyClass;
                if (!string.IsNullOrEmpty(msg.itemsJson))
                {
                    ItemData_v1[] itemArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), msg.itemsJson);
                    if (itemArray != null)
                        loot.Items.DeserializeItems(itemArray);
                }
                DFUNetworkLootRegistry.Register(msg.loadID, loot);
            }
        }

        private void OnClientReceiveDespawnLoot(MsgDespawnLoot msg)
        {
            if (NetworkSession.IsHost)
                return;

            DaggerfallLoot loot = DFUNetworkLootRegistry.Get(msg.loadID);
            if (loot != null)
            {
                DFUNetworkLootRegistry.Unregister(msg.loadID);
                Destroy(loot.gameObject);
            }
        }

        #endregion

        #region Cell Transition Coordination

        public void ReparentAllPuppets()
        {
            Transform bestParent = GameObjectHelper.GetBestPuppetParent();
            foreach (var kvp in remotePlayerPuppets)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    kvp.Value.transform.SetParent(bestParent, true);
                }
            }
            RefreshAllClientEnemyProxies();
        }

        public void RefreshAllRemotePlayerPuppets()
        {
            Transform bestParent = GameObjectHelper.GetBestPuppetParent();
            foreach (var kvp in connectedPlayerProfiles)
            {
                int connId = kvp.Key;
                int selfConnId = NetworkSession.IsHost ? 0 : localConnectionId;
                if (connId == selfConnId || (selfConnId != -1 && connId == selfConnId))
                    continue;

                if (!remotePlayerPuppets.TryGetValue(connId, out NativePlayerPuppet puppet) || puppet == null || puppet.gameObject == null)
                {
                    LogEntity($"[DFU Puppet] Refreshing missing puppet for connection {connId} ({kvp.Value.playerName}).");
                    SpawnRemotePlayerPuppet(kvp.Value);
                }
                else
                {
                    puppet.transform.SetParent(bestParent, true);
                    puppet.gameObject.SetActive(true);
                }
            }
            RefreshAllClientEnemyProxies();
        }

        /// <summary>
        /// Transitions local player into a building interior matching the buildingKey using the authentic scene StaticDoor.
        /// Ensures interior is instantiated at the exact world coordinates with populated ExteriorDoors.
        /// </summary>
        public static bool TransitionToBuildingByKey(int buildingKey)
        {
            if (buildingKey == 0) return false;

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            if (pee == null) return false;

            DaggerfallLocation dfLocation = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
            if (dfLocation == null) return false;

            DaggerfallStaticDoors[] allDoors = dfLocation.gameObject.GetComponentsInChildren<DaggerfallStaticDoors>();
            if (allDoors == null || allDoors.Length == 0) return false;

            Transform doorOwner = null;
            StaticDoor targetDoor = new StaticDoor();
            bool found = false;

            foreach (DaggerfallStaticDoors dfStaticDoors in allDoors)
            {
                if (dfStaticDoors.Doors == null) continue;
                for (int i = 0; i < dfStaticDoors.Doors.Length; i++)
                {
                    if (dfStaticDoors.Doors[i].buildingKey == buildingKey)
                    {
                        targetDoor = dfStaticDoors.Doors[i];
                        doorOwner = dfStaticDoors.transform;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                Debug.LogWarning($"[DFU Transition] Could not find static door for buildingKey {buildingKey} in location {dfLocation.Summary.LocationName}.");
                return false;
            }

            // Discover and populate discovery data
            GameManager.Instance.PlayerGPS.DiscoverBuilding(buildingKey);
            if (GameManager.Instance.PlayerGPS.GetDiscoveredBuilding(buildingKey, out PlayerGPS.DiscoveredBuilding db))
            {
                pee.BuildingDiscoveryData = db;
                pee.IsPlayerInsideOpenShop = RMBLayout.IsShop(db.buildingType) && PlayerActivate.IsBuildingOpen(db.buildingType);
                pee.IsPlayerInsideTavern = RMBLayout.IsTavern(db.buildingType);
                pee.IsPlayerInsideResidence = RMBLayout.IsResidence(db.buildingType);
            }

            pee.TransitionInterior(doorOwner, targetDoor, true, false);
            return true;
        }

        private void HandleOnPreTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            foreach (var kvp in remotePlayerPuppets)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    kvp.Value.transform.SetParent(null, true);
                }
            }
        }

        private void HandleOnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            ReparentAllPuppets();
            RefreshAllClientEnemyProxies();
        }

        private void HandleOnTransitionExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            ReparentAllPuppets();
            RefreshAllClientEnemyProxies();
            lastLocalPlayerPos = (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                ? GameManager.Instance.PlayerObject.transform.position
                : Vector3.zero;
            playerHeartbeatTimer = 1.0f;
            playerStreamTimer = 1.0f;
            StreamLocalPlayerTransform();
        }

        private void HandleOnTransitionDungeonInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            ReparentAllPuppets();
            RefreshAllClientEnemyProxies();
            PlayerEnterExit pee = GameManager.Instance != null ? GameManager.Instance.PlayerEnterExit : null;
            bool isBusyLoading = (pee != null && pee.IsRespawning) || (GameManager.Instance != null && GameManager.Instance.SaveLoadManager != null && GameManager.Instance.SaveLoadManager.LoadInProgress);
            if (!isSyncingTransition && !isBusyLoading && NetworkSession.IsActive)
            {
                BroadcastTransition(PlayerEnterExit.TransitionType.ToDungeonInterior, 0, false);
            }
        }

        private void HandleOnTransitionDungeonExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            ReparentAllPuppets();
            RefreshAllClientEnemyProxies();
            PlayerEnterExit pee = GameManager.Instance != null ? GameManager.Instance.PlayerEnterExit : null;
            bool isBusyLoading = (pee != null && pee.IsRespawning) || (GameManager.Instance != null && GameManager.Instance.SaveLoadManager != null && GameManager.Instance.SaveLoadManager.LoadInProgress);
            if (!isSyncingTransition && !isBusyLoading && NetworkSession.IsActive)
            {
                BroadcastTransition(PlayerEnterExit.TransitionType.ToDungeonExterior, 0, false);
            }
        }

        private void BroadcastTransition(PlayerEnterExit.TransitionType type, int buildingKey, bool isGatherParty = false)
        {
            GameObject player = GameManager.Instance.PlayerObject;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;
            float yaw = player != null ? player.transform.eulerAngles.y : 0f;

            DFPosition mapPixel = GameManager.Instance.PlayerGPS != null ? GameManager.Instance.PlayerGPS.CurrentMapPixel : new DFPosition();

            MsgPlayerTransition msg = new MsgPlayerTransition
            {
                connectionId = NetworkSession.IsHost ? 0 : localConnectionId,
                transitionType = (int)type,
                buildingKey = buildingKey,
                mapPixelX = mapPixel.X,
                mapPixelY = mapPixel.Y,
                playerPosition = pos,
                playerYaw = yaw,
                isGatherParty = isGatherParty
            };

            if (NetworkSession.IsHost)
            {
                NetworkServer.SendToAll(msg);
            }
            else if (NetworkClient.isConnected && NetworkClient.connection != null)
            {
                NetworkClient.Send(msg);
            }
        }

        private void OnServerReceivePlayerTransition(NetworkConnection conn, MsgPlayerTransition msg)
        {
            msg.connectionId = conn.connectionId;
            LogTransition($"[DFU Transition] Server received party transition {((PlayerEnterExit.TransitionType)msg.transitionType)} (Gather: {msg.isGatherParty}) from client {conn.connectionId}");

            foreach (var client in connectedClients)
            {
                if (client.connectionId != conn.connectionId)
                    client.Send(msg);
            }

            SyncEnemiesInCellToClient(conn, msg.transitionType, msg.buildingKey);
            ExecuteNetworkTransition(msg);
        }

        private void SyncEnemiesInCellToClient(NetworkConnection conn, int transitionType, int buildingKey)
        {
            if (!NetworkServer.active || conn == null) return;

            PlayerEnterExit.TransitionType type = (PlayerEnterExit.TransitionType)transitionType;
            bool targetInside = (type == PlayerEnterExit.TransitionType.ToBuildingInterior || type == PlayerEnterExit.TransitionType.ToDungeonInterior);
            bool targetDungeon = (type == PlayerEnterExit.TransitionType.ToDungeonInterior);

            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value == null || !kvp.Value.activeInHierarchy) continue;
                NativeNetworkEntity netEnt = kvp.Value.GetComponent<NativeNetworkEntity>();
                if (netEnt == null || netEnt.isClientProxy) continue;

                bool match = false;
                if (!targetInside && !netEnt.isInside)
                {
                    match = true;
                }
                else if (targetDungeon && netEnt.isInsideDungeon)
                {
                    match = true;
                }
                else if (targetInside && !targetDungeon && netEnt.isInside && !netEnt.isInsideDungeon)
                {
                    match = (buildingKey == 0 || netEnt.buildingKey == buildingKey);
                }

                if (match)
                {
                    conn.Send(netEnt.ToSpawnMessage());
                }
            }
        }

        private void OnClientReceivePlayerTransition(MsgPlayerTransition msg)
        {
            if (msg.connectionId == localConnectionId)
                return;

            LogTransition($"[DFU Transition] Client received party transition {((PlayerEnterExit.TransitionType)msg.transitionType)} (Gather: {msg.isGatherParty}) from peer {msg.connectionId}");
            ExecuteNetworkTransition(msg);
        }

        public void ExecuteNetworkTransition(MsgPlayerTransition msg)
        {
            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            if (pee == null) return;

            bool isBusyLoading = pee.IsRespawning || (GameManager.Instance.SaveLoadManager != null && GameManager.Instance.SaveLoadManager.LoadInProgress);
            if (isBusyLoading && !msg.isGatherParty)
                return;

            isSyncingTransition = true;
            try
            {
                PlayerEnterExit.TransitionType type = (PlayerEnterExit.TransitionType)msg.transitionType;

                if (msg.isGatherParty)
                {
                    // Step 1: Cross-pixel synchronization if peer is in another map pixel
                    DFPosition currentMapPixel = GameManager.Instance.PlayerGPS != null ? GameManager.Instance.PlayerGPS.CurrentMapPixel : new DFPosition();
                    bool needsMapWarp = (msg.mapPixelX != 0 || msg.mapPixelY != 0) &&
                                        (currentMapPixel.X != msg.mapPixelX || currentMapPixel.Y != msg.mapPixelY);

                    if (needsMapWarp)
                    {
                        LogTransition($"[DFU Party] Gather Party cross-pixel warp from ({currentMapPixel.X}, {currentMapPixel.Y}) to ({msg.mapPixelX}, {msg.mapPixelY}).");
                        if (pee.IsPlayerInsideDungeon)
                            pee.TransitionDungeonExteriorImmediate();
                        else if (pee.IsPlayerInsideBuilding)
                            pee.TransitionExterior(false);

                        GameManager.Instance.StreamingWorld.RestoreWorldCompensationHeight(0);
                        GameManager.Instance.StreamingWorld.TeleportToCoordinates(msg.mapPixelX, msg.mapPixelY, StreamingWorld.RepositionMethods.None);
                        if (pee != null) pee.EnableExteriorParent();
                    }

                    // Step 2: Interior/Dungeon cell transitions
                    if (type == PlayerEnterExit.TransitionType.ToDungeonInterior)
                    {
                        if (pee.IsPlayerInsideBuilding)
                            pee.TransitionExterior(false);

                        if (!pee.IsPlayerInsideDungeon)
                        {
                            pee.TransitionDungeonInterior(null, new StaticDoor(), GameManager.Instance.PlayerGPS.CurrentLocation, false);
                        }
                    }
                    else if (type == PlayerEnterExit.TransitionType.ToBuildingInterior)
                    {
                        if (pee.IsPlayerInsideDungeon)
                            pee.TransitionDungeonExteriorImmediate();

                        if (!pee.IsPlayerInsideBuilding || pee.BuildingDiscoveryData.buildingKey != msg.buildingKey)
                        {
                            TransitionToBuildingByKey(msg.buildingKey);
                        }
                    }
                    else if (type == PlayerEnterExit.TransitionType.ToBuildingExterior)
                    {
                        if (pee.IsPlayerInsideDungeon)
                            pee.TransitionDungeonExterior(false);
                        else if (pee.IsPlayerInsideBuilding)
                            pee.TransitionExterior(false);
                    }

                    // Step 3: Reposition player to host with elevation safeguard
                    if (msg.playerPosition != Vector3.zero)
                    {
                        CharacterController cc = GameManager.Instance.PlayerController;
                        if (cc != null) cc.enabled = false;

                        Vector3 offset = Quaternion.Euler(0, msg.playerYaw, 0) * Vector3.right * 0.9f;
                        Vector3 targetPos = msg.playerPosition + offset;

                        // On exterior terrain, raycast from above to align to ground surface
                        if (pee != null && !pee.IsPlayerInside)
                        {
                            Ray ray = new Ray(targetPos + Vector3.up * 100f, Vector3.down);
                            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                            {
                                targetPos.y = hit.point.y + (cc != null ? cc.height * 0.52f : 1.1f);
                            }
                        }

                        GameManager.Instance.PlayerObject.transform.position = targetPos;
                        GameManager.Instance.PlayerObject.transform.rotation = Quaternion.Euler(0, msg.playerYaw, 0);

                        if (cc != null) cc.enabled = true;
                        if (GameManager.Instance.AcrobatMotor != null)
                            GameManager.Instance.AcrobatMotor.ClearFallingDamage();

                        LogTransition($"[DFU Party] Gather Party repositioned local player to {targetPos}.");

                        // Schedule elevation ground check if outdoors
                        if (pee != null && !pee.IsPlayerInside)
                        {
                            StartCoroutine(EnsurePlayerGroundedCoroutine());
                        }
                    }
                }
                else
                {
                    switch (type)
                    {
                        case PlayerEnterExit.TransitionType.ToDungeonInterior:
                            if (!pee.IsPlayerInsideDungeon)
                            {
                                pee.TransitionDungeonInterior(null, new StaticDoor(), GameManager.Instance.PlayerGPS.CurrentLocation, true);
                            }
                            break;

                        case PlayerEnterExit.TransitionType.ToDungeonExterior:
                            if (pee.IsPlayerInsideDungeon)
                            {
                                pee.TransitionDungeonExterior(true);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DFU Transition] Error during party cell transition: {ex.Message}");
            }
            finally
            {
                isSyncingTransition = false;
                RefreshAllRemotePlayerPuppets();
            }

            if (remotePlayerPuppets.TryGetValue(msg.connectionId, out NativePlayerPuppet puppet) && puppet != null && msg.playerPosition != Vector3.zero)
            {
                puppet.ApplyTransform(msg.playerPosition, msg.playerYaw, false, false, false, (int)WeaponTypes.None);
            }
        }

        public void GatherPartyToHost()
        {
            if (!NetworkSession.IsHost) return;

            RefreshAllRemotePlayerPuppets();

            PlayerEnterExit pee = GameManager.Instance.PlayerEnterExit;
            if (pee == null) return;

            PlayerEnterExit.TransitionType type = PlayerEnterExit.TransitionType.ToBuildingExterior;
            int bKey = 0;

            if (pee.IsPlayerInsideDungeon)
                type = PlayerEnterExit.TransitionType.ToDungeonInterior;
            else if (pee.IsPlayerInsideBuilding)
            {
                type = PlayerEnterExit.TransitionType.ToBuildingInterior;
                bKey = pee.BuildingDiscoveryData.buildingKey;
            }

            BroadcastTransition(type, bKey, true);
            LogTransition($"[DFU Party] Host issued 'Gather Party' command to all connected peers (Type: {type}, BuildingKey: {bKey}).");
        }

        #endregion

        #region Debug GUI & DM Spawner

        /// <summary>
        /// Authoritatively spawns an enemy 2 meters in front of the local player.
        /// Broadcasts spawn message to all connected clients.
        /// </summary>
        public GameObject SpawnEnemyInFrontOfPlayer(MobileTypes enemyType)
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogSpawn("[DM Tool] Only the Server / Host can spawn authoritative enemies.");
                return null;
            }

            GameObject player = GameManager.Instance.PlayerObject;
            if (player == null)
            {
                LogSpawn("[DM Tool] PlayerObject not found.");
                return null;
            }

            Transform playerTrans = player.transform;
            // Place 2 meters forward in front of the player (in Unity, 1 unit = 1 meter)
            Vector3 spawnPos = playerTrans.position + (playerTrans.forward * 2.0f);
            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.CreateEnemy(
                enemyType.ToString(),
                enemyType,
                spawnPos,
                MobileGender.Unspecified,
                parent,
                MobileReactions.Hostile
            );

            if (go != null)
            {
                go.transform.position = spawnPos;
                Vector3 lookDirection = playerTrans.position - go.transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                    go.transform.rotation = Quaternion.LookRotation(lookDirection);

                SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();
                if (setupEnemy != null)
                {
                    MobileUnit mobileUnit = setupEnemy.GetMobileBillboardChild();
                    if (mobileUnit != null && mobileUnit.Enemy.Behaviour != MobileBehaviour.Flying)
                    {
                        GameObjectHelper.AlignControllerToGround(go.GetComponent<CharacterController>());
                    }
                }

                NativeNetworkEntity netEntity = go.GetComponent<NativeNetworkEntity>();
                if (netEntity != null && netEntity.entityId != Guid.Empty)
                {
                    spawnedEnemyHistory.Add(netEntity.entityId);
                }

                LogSpawn($"[DM Tool] Authoritatively spawned {enemyType} 2m in front of player at {go.transform.position}.");
            }

            return go;
        }

        public GameObject SpawnEnemyAtCursor(MobileTypes enemyType, float hpMultiplier = 1.0f)
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogSpawn("[DM Tool] Only the Server / Host can spawn authoritative enemies.");
                return null;
            }

            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam == null)
                return SpawnEnemyInFrontOfPlayer(enemyType);

            Ray ray;
            if (Cursor.lockState != CursorLockMode.Locked)
                ray = mainCam.ScreenPointToRay(Input.mousePosition);
            else
                ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                Vector3 spawnPos = hit.point + Vector3.up * 0.15f;
                Transform parent = GameObjectHelper.GetBestParent();

                GameObject go = GameObjectHelper.CreateEnemy(
                    enemyType.ToString(),
                    enemyType,
                    spawnPos,
                    MobileGender.Unspecified,
                    parent,
                    MobileReactions.Hostile
                );

                if (go != null)
                {
                    go.transform.position = spawnPos;
                    if (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                    {
                        Vector3 lookDirection = GameManager.Instance.PlayerObject.transform.position - go.transform.position;
                        lookDirection.y = 0;
                        if (lookDirection != Vector3.zero)
                            go.transform.rotation = Quaternion.LookRotation(lookDirection);
                    }

                    SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();
                    if (setupEnemy != null)
                    {
                        MobileUnit mobileUnit = setupEnemy.GetMobileBillboardChild();
                        if (mobileUnit != null && mobileUnit.Enemy.Behaviour != MobileBehaviour.Flying)
                        {
                            GameObjectHelper.AlignControllerToGround(go.GetComponent<CharacterController>());
                        }
                    }

                    if (hpMultiplier > 1.05f)
                    {
                        DaggerfallEntityBehaviour behaviour = go.GetComponent<DaggerfallEntityBehaviour>();
                        if (behaviour != null && behaviour.Entity is EnemyEntity enemyEntity)
                        {
                            enemyEntity.MaxHealth = (int)(enemyEntity.MaxHealth * hpMultiplier);
                            enemyEntity.CurrentHealth = enemyEntity.MaxHealth;
                        }
                    }

                    NativeNetworkEntity netEntity = go.GetComponent<NativeNetworkEntity>();
                    if (netEntity != null && netEntity.entityId != Guid.Empty)
                        spawnedEnemyHistory.Add(netEntity.entityId);

                    DaggerfallUI.AddHUDText($"[DM Spawner] Invocado {enemyType} ({hpMultiplier:F1}x HP) en terreno ({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1}).");
                }
                return go;
            }

            return SpawnEnemyInFrontOfPlayer(enemyType);
        }

        public void TriggerEarthquake(float duration = 4.0f, float magnitude = 0.35f)
        {
            StartCoroutine(EarthquakeCoroutine(duration, magnitude));
        }

        private System.Collections.IEnumerator EarthquakeCoroutine(float duration, float magnitude)
        {
            Camera cam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (cam == null) yield break;

            DaggerfallUI.AddHUDText("[DM World Event] ¡TERREMOTO EN CURSO!");
            if (DaggerfallUI.Instance != null)
                DaggerfallUI.Instance.PlayOneShot(SoundClips.StormThunderRoll);

            Transform camTransform = cam.transform;
            Vector3 originalPos = camTransform.localPosition;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float strength = Mathf.Lerp(magnitude, 0f, timer / duration);
                Vector3 offset = UnityEngine.Random.insideUnitSphere * strength;
                camTransform.localPosition = originalPos + offset;
                yield return null;
            }

            camTransform.localPosition = originalPos;
            DaggerfallUI.AddHUDText("[DM World Event] El terremoto ha cesado.");
        }

        public void ForgeCustomWeapon(Weapons weaponType, WeaponMaterialTypes material, bool directToInventory)
        {
            if (GameManager.Instance == null || GameManager.Instance.PlayerEntity == null) return;
            DaggerfallUnityItem item = ItemBuilder.CreateWeapon(weaponType, material);
            if (item == null) return;

            if (directToInventory)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(item);
                DaggerfallUI.AddHUDText($"[DM Loot Forge] Forjado {item.ItemName} y guardado en tu inventario.");
            }
            else
            {
                DaggerfallLoot container = GameObjectHelper.CreateDroppedLootContainer(GameManager.Instance.PlayerObject, DaggerfallUnity.NextUID);
                if (container != null && container.Items != null)
                {
                    Vector3 dropPos = GameManager.Instance.PlayerObject.transform.position + GameManager.Instance.PlayerObject.transform.forward * 1.5f;
                    container.transform.position = new Vector3(dropPos.x, container.transform.position.y, dropPos.z);
                    container.Items.AddItem(item);
                    DaggerfallUI.AddHUDText($"[DM Loot Forge] Forjado {item.ItemName} y soltado en el suelo.");
                }
            }
        }

        public void ForgeCustomArmor(Armor armorType, ArmorMaterialTypes material, bool directToInventory)
        {
            if (GameManager.Instance == null || GameManager.Instance.PlayerEntity == null) return;
            DaggerfallUnityItem item = ItemBuilder.CreateArmor(GameManager.Instance.PlayerEntity.Gender, GameManager.Instance.PlayerEntity.Race, armorType, material);
            if (item == null) return;

            if (directToInventory)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(item);
                DaggerfallUI.AddHUDText($"[DM Loot Forge] Forjado {item.ItemName} y guardado en tu inventario.");
            }
            else
            {
                DaggerfallLoot container = GameObjectHelper.CreateDroppedLootContainer(GameManager.Instance.PlayerObject, DaggerfallUnity.NextUID);
                if (container != null && container.Items != null)
                {
                    Vector3 dropPos = GameManager.Instance.PlayerObject.transform.position + GameManager.Instance.PlayerObject.transform.forward * 1.5f;
                    container.transform.position = new Vector3(dropPos.x, container.transform.position.y, dropPos.z);
                    container.Items.AddItem(item);
                    DaggerfallUI.AddHUDText($"[DM Loot Forge] Forjado {item.ItemName} y soltado en el suelo.");
                }
            }
        }

        public void PardonAllCrimes()
        {
            if (GameManager.Instance == null || GameManager.Instance.PlayerEntity == null) return;
            GameManager.Instance.PlayerEntity.CrimeCommitted = PlayerEntity.Crimes.None;
            int region = GameManager.Instance.PlayerGPS != null ? GameManager.Instance.PlayerGPS.CurrentRegionIndex : -1;
            if (region >= 0 && region < GameManager.Instance.PlayerEntity.RegionData.Length)
            {
                GameManager.Instance.PlayerEntity.RegionData[region].LegalRep = 50;
            }
            DespawnAllSpawnedEnemies();
            DaggerfallUI.AddHUDText("[DM Ley] Todos los crímenes perdonados y reputación legal restaurada.");
        }

        private readonly List<Guid> spawnedEnemyHistory = new List<Guid>();

        /// <summary>
        /// Authoritatively despawns the most recently spawned enemy on Server/Host.
        /// Broadcasts MsgDespawnEnemy to all connected clients.
        /// </summary>
        public bool DespawnLastEnemy()
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogSpawn("[DM Tool] Only the Server / Host can despawn authoritative enemies.");
                return false;
            }

            // 1. Check spawned history first
            while (spawnedEnemyHistory.Count > 0)
            {
                Guid targetId = spawnedEnemyHistory[spawnedEnemyHistory.Count - 1];
                spawnedEnemyHistory.RemoveAt(spawnedEnemyHistory.Count - 1);

                if (DFUNetworkRegistry.TryGetEntity(targetId, out GameObject go) && go != null)
                {
                    LogSpawn($"[DM Tool] Despawning enemy {go.name} (GUID: {targetId}).");
                    Destroy(go);
                    return true;
                }
            }

            // 2. Fall back to any authoritative enemy in registry
            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value != null)
                {
                    NativeNetworkEntity netEnt = kvp.Value.GetComponent<NativeNetworkEntity>();
                    if (netEnt != null && !netEnt.isClientProxy)
                    {
                        LogSpawn($"[DM Tool] Despawning authoritative enemy {kvp.Value.name} (GUID: {kvp.Key}).");
                        Destroy(kvp.Value);
                        return true;
                    }
                }
            }

            LogSpawn("[DM Tool] No authoritative enemies found to despawn.");
            return false;
        }

        /// <summary>
        /// Despawns all tracked authoritative enemies spawned by DM / scene.
        /// </summary>
        public int DespawnAllSpawnedEnemies()
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogSpawn("[DM Tool] Only the Server / Host can despawn authoritative enemies.");
                return 0;
            }

            int count = 0;
            while (spawnedEnemyHistory.Count > 0)
            {
                Guid targetId = spawnedEnemyHistory[spawnedEnemyHistory.Count - 1];
                spawnedEnemyHistory.RemoveAt(spawnedEnemyHistory.Count - 1);

                if (DFUNetworkRegistry.TryGetEntity(targetId, out GameObject go) && go != null)
                {
                    Destroy(go);
                    count++;
                }
            }

            List<GameObject> toDestroy = new List<GameObject>();
            foreach (var kvp in DFUNetworkRegistry.GetAllEntities())
            {
                if (kvp.Value != null)
                {
                    NativeNetworkEntity netEnt = kvp.Value.GetComponent<NativeNetworkEntity>();
                    if (netEnt != null && !netEnt.isClientProxy)
                    {
                        toDestroy.Add(kvp.Value);
                    }
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null)
                {
                    Destroy(go);
                    count++;
                }
            }

            LogSpawn($"[DM Tool] Despawned {count} enemies.");
            return count;
        }

        private string guiAddress = "127.0.0.1";
        private string guiPort = "7777";

        private Rect windowRect = new Rect(20, 20, 280, 420);
        private Rect dmWindowRect = new Rect(340, 10, 320, 620);
        private int selectedDmIndex = 0;
        private string dmTpInputX = "109";
        private string dmTpInputY = "158";
        private static readonly MobileTypes[] dmEnemies = new MobileTypes[]
        {
            MobileTypes.Rat,
            MobileTypes.GiantBat,
            MobileTypes.SkeletalWarrior,
            MobileTypes.Zombie,
            MobileTypes.Orc,
            MobileTypes.Thief,
            MobileTypes.GrizzlyBear,
            MobileTypes.Spider,
            MobileTypes.Werewolf,
            MobileTypes.Spellsword,
            MobileTypes.Daedroth
        };

        // Phase 15: Bestiary Catalog
        private string dmBestiarySearchFilter = "";
        private int dmBestiaryCategoryIndex = 0;
        private static readonly string[] dmBestiaryCategories = new string[]
        {
            "Todos", "No-Muertos", "Monstruos", "Daedra", "Humanoides", "Fauna"
        };
        private float dmSpawnHpMultiplier = 1.0f;
        private Vector2 dmBestiaryScrollPos = Vector2.zero;

        private static readonly MobileTypes[] dmUndead = new MobileTypes[]
        {
            MobileTypes.SkeletalWarrior, MobileTypes.Zombie, MobileTypes.Ghost,
            MobileTypes.Mummy, MobileTypes.Wraith, MobileTypes.Vampire,
            MobileTypes.VampireAncient, MobileTypes.Lich, MobileTypes.AncientLich
        };

        private static readonly MobileTypes[] dmMonsters = new MobileTypes[]
        {
            MobileTypes.Spriggan, MobileTypes.Gargoyle, MobileTypes.Centaur,
            MobileTypes.Harpy, MobileTypes.Werewolf, MobileTypes.Wereboar,
            MobileTypes.Spider, MobileTypes.GiantScorpion, MobileTypes.Giant,
            MobileTypes.Dragonling, MobileTypes.Dragonling_Alternate, MobileTypes.Lamia, MobileTypes.Dreugh
        };

        private static readonly MobileTypes[] dmDaedra = new MobileTypes[]
        {
            MobileTypes.Imp, MobileTypes.Daedroth, MobileTypes.FireDaedra,
            MobileTypes.FrostDaedra, MobileTypes.DaedraSeducer, MobileTypes.DaedraLord,
            MobileTypes.FireAtronach, MobileTypes.IronAtronach, MobileTypes.FleshAtronach, MobileTypes.IceAtronach
        };

        private static readonly MobileTypes[] dmHumanoids = new MobileTypes[]
        {
            MobileTypes.Knight_CityWatch, MobileTypes.Mage, MobileTypes.Spellsword,
            MobileTypes.Battlemage, MobileTypes.Sorcerer, MobileTypes.Healer,
            MobileTypes.Nightblade, MobileTypes.Bard, MobileTypes.Burglar,
            MobileTypes.Rogue, MobileTypes.Acrobat, MobileTypes.Thief,
            MobileTypes.Assassin, MobileTypes.Monk, MobileTypes.Archer,
            MobileTypes.Ranger, MobileTypes.Barbarian, MobileTypes.Warrior,
            MobileTypes.Knight, MobileTypes.Orc, MobileTypes.OrcSergeant,
            MobileTypes.OrcShaman, MobileTypes.OrcWarlord
        };

        private static readonly MobileTypes[] dmFauna = new MobileTypes[]
        {
            MobileTypes.Rat, MobileTypes.GiantBat, MobileTypes.GrizzlyBear,
            MobileTypes.SabertoothTiger, MobileTypes.Slaughterfish
        };

        // Phase 15: Loot Forge fields
        private int dmForgeGroupIndex = 0;
        private static readonly string[] dmForgeGroups = new string[] { "Armas", "Armaduras", "Gemas", "Pociones" };
        private int dmForgeWeaponIndex = 0;
        private static readonly Weapons[] dmForgeWeapons = new Weapons[]
        {
            Weapons.Dagger, Weapons.Shortsword, Weapons.Broadsword, Weapons.Longsword, Weapons.Claymore,
            Weapons.Battle_Axe, Weapons.War_Axe, Weapons.Short_Bow, Weapons.Long_Bow,
            Weapons.Mace, Weapons.Flail, Weapons.Warhammer, Weapons.Staff, Weapons.Katana, Weapons.Dai_Katana
        };
        private int dmForgeMaterialIndex = 1;
        private static readonly WeaponMaterialTypes[] dmForgeMaterials = new WeaponMaterialTypes[]
        {
            WeaponMaterialTypes.Iron, WeaponMaterialTypes.Steel, WeaponMaterialTypes.Silver,
            WeaponMaterialTypes.Elven, WeaponMaterialTypes.Dwarven, WeaponMaterialTypes.Mithril,
            WeaponMaterialTypes.Adamantium, WeaponMaterialTypes.Ebony, WeaponMaterialTypes.Orcish, WeaponMaterialTypes.Daedric
        };
        private int dmForgeArmorIndex = 0;
        private static readonly Armor[] dmForgeArmors = new Armor[]
        {
            Armor.Cuirass, Armor.Gauntlets, Armor.Greaves, Armor.Boots, Armor.Left_Pauldron, Armor.Right_Pauldron, Armor.Helm
        };
        private static readonly ArmorMaterialTypes[] dmForgeArmorMaterials = new ArmorMaterialTypes[]
        {
            ArmorMaterialTypes.Iron, ArmorMaterialTypes.Steel, ArmorMaterialTypes.Silver,
            ArmorMaterialTypes.Elven, ArmorMaterialTypes.Dwarven, ArmorMaterialTypes.Mithril,
            ArmorMaterialTypes.Adamantium, ArmorMaterialTypes.Ebony, ArmorMaterialTypes.Orcish, ArmorMaterialTypes.Daedric
        };

        private void OnGUI()
        {
            GUISkin prevSkin = GUI.skin;
            GUISkin darkFantasySkin = DarkFantasyPixelArtTheme.GetOrCreateImGuiSkin(prevSkin);
            if (darkFantasySkin != null)
                GUI.skin = darkFantasySkin;

            try
            {
                if (isPartyRestingBlackout)
                {
                    GUI.depth = -2000;
                    Color prevColor = GUI.color;
                    GUI.color = Color.black;
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    if (blackoutStyle == null)
                    {
                        blackoutStyle = new GUIStyle(GUI.skin.label);
                        blackoutStyle.alignment = TextAnchor.MiddleCenter;
                        blackoutStyle.fontSize = 22;
                        blackoutStyle.normal.textColor = DarkFantasyPixelArtTheme.GoldHighlight;
                    }
                    string actionText = partyRestIsLoiter ? "Loitering" : "Resting";
                    GUI.Label(new Rect(0, Screen.height / 2 - 30, Screen.width, 60), $"{actionText} with the party... ({partyRestHours} hours)", blackoutStyle);
                    GUI.color = prevColor;
                }

                if (showTtrpgSuite)
                {
                    GUI.depth = -999;
                    ttrpgSuiteRect = GUI.Window(987653, ttrpgSuiteRect, DrawTtrpgMasterWindow, "✦ DFU TTRPG MASTER SUITE (Ctrl+F11) ✦");
                }

                if (showDebugGui)
                {
                    GUI.depth = -999;
                    windowRect = GUI.Window(987654, windowRect, DrawNetworkWindow, "✦ DFU Native Multiplayer (Ctrl+F10) ✦");
                }

                if (showDmGui)
                {
                    GUI.depth = -999;
                    dmWindowRect = GUI.Window(987655, dmWindowRect, DrawDmWindow, "✦ DM Tool (Spawns, Teleport & Weather) ✦");
                }

                if (showLootGui)
                {
                    GUI.depth = -999;
                    lootWindowRect = GUI.Window(987656, lootWindowRect, DrawLootWindow, "✦ DM Loot Tool (Ctrl+F12) ✦");
                }

                if (showQuestGui)
                {
                    GUI.depth = -999;
                    questWindowRect = GUI.Window(987657, questWindowRect, DrawQuestWindow, "✦ Quest Creator / Test Menu (Ctrl+F7) ✦");
                }

                if (showPlayerInteractionWindow && interactionTargetPuppet != null && interactionTargetPuppet.gameObject != null)
                {
                    GUI.depth = -1000;
                    playerInteractionWindowRect = GUI.Window(987658, playerInteractionWindowRect, DrawPlayerInteractionWindow, $"✦ Aventurero: {interactionTargetPuppet.playerName} ✦");
                }

                if (showTradeInvitePrompt)
                {
                    GUI.depth = -1001;
                    tradeInviteRect = GUI.Window(987659, tradeInviteRect, DrawTradeInvitePromptWindow, "✦ Solicitud de Comercio ✦");
                }

                if (showTradeWindow)
                {
                    GUI.depth = -1000;
                    tradeWindowRect = GUI.Window(987660, tradeWindowRect, DrawTradeWindow, $"✦ Comercio Bilateral — {activeTradePartnerName} ✦");
                }

                GUI.depth = -999;
                DrawDialogueBubbles();
            }
            finally
            {
                GUI.skin = prevSkin;
            }
        }

        private void DrawTtrpgMasterWindow(int windowId)
        {
            DarkFantasyPixelArtTheme.DrawImGuiDivider();

            bool canUseDm = NetworkSession.IsServer || localLobbyIsDm;
            if (!canUseDm && ttrpgSelectedTab != 0)
            {
                ttrpgSelectedTab = 0;
            }

            // Tab bar with Dark Fantasy Pixel-Art beveled tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < ttrpgTabNames.Length; i++)
            {
                bool isDmTab = (i > 0);
                bool isTabEnabled = canUseDm || !isDmTab;
                bool isSelected = (ttrpgSelectedTab == i);

                GUIStyle tabStyle = isSelected ? DarkFantasyPixelArtTheme.TabActiveStyle : DarkFantasyPixelArtTheme.TabInactiveStyle;
                string tabTitle = isSelected ? $"✦ {ttrpgTabNames[i]}" : ttrpgTabNames[i];
                if (!isTabEnabled)
                {
                    tabTitle = $"{ttrpgTabNames[i]} <color=#706050>(DM)</color>";
                }

                bool prevEnabled = GUI.enabled;
                if (!isTabEnabled)
                    GUI.enabled = false;

                if (GUILayout.Button(tabTitle, tabStyle, GUILayout.Height(30)))
                {
                    if (isTabEnabled)
                    {
                        ttrpgSelectedTab = i;
                    }
                }

                GUI.enabled = prevEnabled;
            }
            GUILayout.EndHorizontal();

            DarkFantasyPixelArtTheme.DrawImGuiDivider();

            // Recessed Dark Fantasy container for tab content
            GUILayout.BeginVertical(GUI.skin.box);
            ttrpgSuiteScrollPos = GUILayout.BeginScrollView(ttrpgSuiteScrollPos);

            switch (ttrpgSelectedTab)
            {
                case 0:
                    DrawNetworkWindowContent();
                    break;
                case 1:
                    DrawSpawnsTabContent();
                    break;
                case 2:
                    DrawWorldTabContent();
                    break;
                case 3:
                    DrawQuestWindowContent();
                    break;
                case 4:
                    DrawLootWindowContent();
                    break;
                case 5:
                    DrawPartyDmTabContent();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=#C6AC71><b>Rol:</b> {NetworkSession.GetStatusString()}</color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Cerrar (Esc / Ctrl+F11)", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(175), GUILayout.Height(26)))
            {
                showTtrpgSuite = false;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void DrawNetworkWindow(int windowId)
        {
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            DrawNetworkWindowContent();
            GUILayout.Space(8);
            if (GUILayout.Button("Quest Creator / Test Menu (Ctrl+F7)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
            {
                showQuestGui = !showQuestGui;
            }
            GUI.DragWindow();
        }

        private void DrawNetworkWindowContent()
        {
            GUILayout.Space(4);
            GUILayout.Label("✦ ESTADO DE CONEXIÓN & SESIÓN ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            GUILayout.Label($"<b>Estado actual:</b> <color=#FFDF78>{NetworkSession.GetStatusString()}</color>");

            if (NetworkSession.IsSinglePlayer)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label("IP:", GUILayout.Width(40));
                guiAddress = GUILayout.TextField(guiAddress);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Port:", GUILayout.Width(40));
                guiPort = GUILayout.TextField(guiPort);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                ushort.TryParse(guiPort, out ushort port);
                if (port == 0) port = defaultPort;

                if (GUILayout.Button("Start Host (Server + Client)", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
                {
                    StartHosting(port);
                }

                if (GUILayout.Button("Connect as Client", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
                {
                    ConnectToServer(guiAddress, port);
                }
            }
            else
            {
                GUILayout.Space(5);
                if (NetworkSession.IsServer)
                {
                    GUILayout.Label($"Connected Peers: <b>{connectedClients.Count}</b>");
                }

                if (GUILayout.Button("Disconnect Session", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(28)))
                {
                    DisconnectSession();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("✦ FILTROS DE REGISTRO (CONSOLA) ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            LogTimeEnabled = GUILayout.Toggle(LogTimeEnabled, " Time Sync ([DFU Time])");
            LogWeatherEnabled = GUILayout.Toggle(LogWeatherEnabled, " Weather ([DFU Weather])");
            LogEntitiesEnabled = GUILayout.Toggle(LogEntitiesEnabled, " Entities / Puppets");
            LogSpawnsEnabled = GUILayout.Toggle(LogSpawnsEnabled, " Spawns / Despawns");
            LogLootEnabled = GUILayout.Toggle(LogLootEnabled, " Loot Containers");
            LogTransitionsEnabled = GUILayout.Toggle(LogTransitionsEnabled, " Transitions");
        }

        public void PacifySceneEnemies()
        {
            EnemyMotor[] enemyMotors = UnityEngine.Object.FindObjectsOfType<EnemyMotor>();
            if (enemyMotors != null)
            {
                foreach (var motor in enemyMotors)
                {
                    if (motor != null)
                        motor.ClearTarget();
                }
            }
        }

        public void ToggleDmInvisibility()
        {
            isDmInvisible = !isDmInvisible;

            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.NoTargetMode = isDmInvisible;
                if (isDmInvisible)
                    GameManager.Instance.PlayerEntity.MagicalConcealmentFlags |= MagicalConcealmentFlags.InvisibleTrue;
                else
                    GameManager.Instance.PlayerEntity.MagicalConcealmentFlags &= ~MagicalConcealmentFlags.InvisibleTrue;
            }

            // Pacify any currently targeting enemies if becoming invisible
            if (isDmInvisible)
            {
                PacifySceneEnemies();
            }

            if (FreeCamController.Instance != null)
            {
                FreeCamController.Instance.UpdatePuppetVisibility();
            }

            // Immediately broadcast new invisibility state across the network
            lastLocalPlayerPos = Vector3.zero;
            StreamLocalPlayerTransform();

            DaggerfallUI.AddHUDText(isDmInvisible ? "[DM] Invisibility: ON (True Invisible)" : "[DM] Invisibility: OFF");
            LogNetwork($"[DFU DM] Invisibility toggled: {isDmInvisible}");
        }

        /// <summary>
        /// Toggles between Spectator Spirit Mode (Zeus 6-DOF noclip flight, intangible, invisible)
        /// and Physical Admin Avatar Mode (visible in 3D world, physical collider, golden DM nametag).
        /// </summary>
        public void ToggleDmEntityMode()
        {
            if (currentDmEntityMode == DMEntityMode.PhysicalAvatar)
                SetDmEntityMode(DMEntityMode.SpectatorSpirit);
            else
                SetDmEntityMode(DMEntityMode.PhysicalAvatar);
        }

        /// <summary>
        /// Sets the operational DM entity mode cleanly, configuring colliders, motor, invisibility, and network sync.
        /// </summary>
        public void SetDmEntityMode(DMEntityMode newMode)
        {
            currentDmEntityMode = newMode;

            if (newMode == DMEntityMode.SpectatorSpirit)
            {
                isDmInvisible = true;
                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.NoTargetMode = true;
                    GameManager.Instance.PlayerEntity.MagicalConcealmentFlags |= MagicalConcealmentFlags.InvisibleTrue;
                    GameManager.Instance.PlayerEntity.GodMode = true;
                }

                PacifySceneEnemies();

                if (FreeCamController.Instance != null && !FreeCamController.Instance.IsActive)
                {
                    FreeCamController.Instance.Activate();
                }

                if (FreeCamController.Instance != null)
                {
                    FreeCamController.Instance.UpdatePuppetVisibility();
                }

                lastLocalPlayerPos = Vector3.zero;
                StreamLocalPlayerTransform();

                DaggerfallUI.AddHUDText("[DM DUAL MODE] Spectator Spirit (Zeus) ACTIVE - Invisible, Noclip Flight, No Aggro");
                LogNetwork("[DFU DM] Switched to Spectator Spirit Mode (Zeus)");
            }
            else
            {
                isDmInvisible = false;

                if (FreeCamController.Instance != null && FreeCamController.Instance.IsActive)
                {
                    Vector3 camPos = FreeCamController.Instance.CameraPosition;
                    float camYaw = FreeCamController.Instance.CameraYaw;

                    RelocateLocalPlayerToCamera(camPos, camYaw);

                    FreeCamController.Instance.Deactivate();
                }

                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.NoTargetMode = false;
                    GameManager.Instance.PlayerEntity.MagicalConcealmentFlags &= ~MagicalConcealmentFlags.InvisibleTrue;

                    if (GameManager.Instance.PlayerController != null)
                    {
                        GameManager.Instance.PlayerController.enabled = true;
                        GameManager.Instance.PlayerController.detectCollisions = true;
                    }

                    if (GameManager.Instance.PlayerMotor != null)
                    {
                        GameManager.Instance.PlayerMotor.FreezeMotor = 0f;
                        GameManager.Instance.PlayerMotor.CancelMovement = true;
                    }
                }

                if (FreeCamController.Instance != null)
                {
                    FreeCamController.Instance.UpdatePuppetVisibility();
                }

                lastLocalPlayerPos = Vector3.zero;
                StreamLocalPlayerTransform();

                DaggerfallUI.AddHUDText("[DM DUAL MODE] Physical Admin Avatar ACTIVE - Visible, Physical Collider Restored");
                LogNetwork("[DFU DM] Switched to Physical Admin Avatar Mode");
            }
        }

        public void RelocateLocalPlayerToCamera(Vector3 camPos, float camYaw)
        {
            GameObject player = GameManager.Instance.PlayerObject;
            if (player == null)
                return;

            CharacterController cc = GameManager.Instance.PlayerController;
            if (cc != null)
                cc.enabled = false;

            // Raycast downward to locate safe ground surface
            Vector3 targetPos = camPos;
            if (Physics.Raycast(camPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 50f))
            {
                targetPos = hit.point + Vector3.up * 0.05f;
            }

            player.transform.position = targetPos;
            player.transform.rotation = Quaternion.Euler(0, camYaw, 0);

            if (GameManager.Instance.PlayerMouseLook != null)
            {
                GameManager.Instance.PlayerMouseLook.SetFacing(camYaw, 0f);
            }

            if (cc != null)
            {
                cc.enabled = true;
                GameObjectHelper.AlignControllerToGround(cc);
            }
        }

        public void ToggleDmGodMode()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.GodMode = !GameManager.Instance.PlayerEntity.GodMode;
                bool isGod = GameManager.Instance.PlayerEntity.GodMode;
                DaggerfallUI.AddHUDText($"[DM Tool] God Mode: {(isGod ? "ENABLED (Invincible)" : "DISABLED")}");
                LogNetwork($"[DFU DM] God Mode toggled: {isGod}");
            }
        }

        public void TeleportLocalPlayer(int mapPixelX, int mapPixelY)
        {
            LogNetwork($"[DFU Teleport] Teleporting local player to ({mapPixelX}, {mapPixelY})");

            // Transition out of interior or dungeon to prevent black void
            if (GameManager.Instance.PlayerEnterExit != null && GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    GameManager.Instance.PlayerEnterExit.TransitionDungeonExteriorImmediate();
                    GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
                }
                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
                {
                    GameManager.Instance.PlayerEnterExit.TransitionExterior(false);
                }
                else
                {
                    GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
                }
            }

            if (GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.FreezeMotor = 1.5f;
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }
            if (GameManager.Instance.PlayerController != null)
            {
                GameManager.Instance.PlayerController.enabled = false;
            }

            GameManager.Instance.StreamingWorld.RestoreWorldCompensationHeight(0);
            GameManager.Instance.StreamingWorld.TeleportToCoordinates(mapPixelX, mapPixelY, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

            if (GameManager.Instance.PlayerEnterExit != null)
            {
                GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
            }

            if (GameManager.Instance.AcrobatMotor != null)
            {
                GameManager.Instance.AcrobatMotor.ClearFallingDamage();
            }

            RefreshAllRemotePlayerPuppets();
            lastLocalPlayerPos = Vector3.zero;

            StartCoroutine(EnsurePlayerGroundedCoroutine());
        }

        private System.Collections.IEnumerator EnsurePlayerGroundedCoroutine()
        {
            // Wait until StreamingWorld has completed all terrain chunk updates
            while (GameManager.Instance.StreamingWorld != null && GameManager.Instance.StreamingWorld.IsTerrainUpdateRunning)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            yield return new WaitForEndOfFrame();

            GameObject player = GameManager.Instance.PlayerObject;
            CharacterController cc = GameManager.Instance.PlayerController;
            if (player == null || cc == null) yield break;

            // Only apply when player is outdoors
            if (GameManager.Instance.PlayerEnterExit != null && GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                cc.enabled = true;
                if (GameManager.Instance.PlayerMotor != null)
                {
                    GameManager.Instance.PlayerMotor.FreezeMotor = 0f;
                    GameManager.Instance.PlayerMotor.CancelMovement = true;
                }
                yield break;
            }

            Vector3 currentPos = player.transform.position;
            // Raycast down from high altitude (1500m) to reliably catch the ground / terrain surface
            Ray ray = new Ray(new Vector3(currentPos.x, 1500f, currentPos.z), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 3000f))
            {
                float surfaceY = hit.point.y + (cc.height * 0.52f);
                cc.enabled = false;
                player.transform.position = new Vector3(currentPos.x, surfaceY, currentPos.z);
                if (GameManager.Instance.AcrobatMotor != null)
                    GameManager.Instance.AcrobatMotor.ClearFallingDamage();
                LogNetwork($"[DFU Teleport] Elevation safeguard smoothly aligned player to terrain surface: {surfaceY:F2} (hit {hit.point.y:F2}).");
            }

            // Safely re-enable controller and clear movement
            cc.enabled = true;
            if (GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.FreezeMotor = 0f;
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }
            if (GameManager.Instance.AcrobatMotor != null)
            {
                GameManager.Instance.AcrobatMotor.ClearFallingDamage();
            }
        }

        private void DrawDmWindow(int windowId)
        {
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            dmScrollPosition = GUILayout.BeginScrollView(dmScrollPosition);

            GUILayout.Space(5);
            GUILayout.Label($"Role: {NetworkSession.GetStatusString()}");

            if (!NetworkSession.ShouldHandleSpawns)
            {
                GUILayout.Space(5);
                GUILayout.Label("DM controls & spawning are only allowed on the Host/Server. Pure clients cannot modify world state.", GUI.skin.box);
            }
            else
            {
                DrawSpawnsTabContent();
                GUILayout.Space(10);
                DrawWorldTabContent();
                GUILayout.Space(10);
                DrawPartyDmTabContent();
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Quest Creator / Test Menu (Ctrl+F7)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(24)))
            {
                showQuestGui = !showQuestGui;
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Close (dm_gui)", DarkFantasyPixelArtTheme.CrimsonButtonStyle))
            {
                showDmGui = false;
            }

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void DrawSpawnsTabContent()
        {
            GUILayout.Space(4);
            if (!NetworkSession.ShouldHandleSpawns)
            {
                GUILayout.Label("Spawns & NPC controls are only allowed on the Host/Server (or DM).", GUI.skin.box);
                return;
            }

            // --- PILAR 1: POSESIÓN DIRECTA DE ENTIDAD (INHABIT) ---
            GUILayout.Label("✦ POSESIÓN DIRECTA DE ENTIDAD (INHABIT) ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            if (DMNpcController.Instance != null && DMNpcController.Instance.IsPossessing)
            {
                GUILayout.Label($"<color=#78FF78><b>POSESIÓN ACTIVA:</b></color> {DMNpcController.Instance.PossessedName}");
                GUILayout.Label("<color=#AAAAAA>WASD: Mover | Shift: Correr | Click Izq: Atacar | Tab: Cámara 1ª/3ª</color>");
                GUILayout.Space(4);
                if (GUILayout.Button("✦ Liberar Posesión (Esc / F8) ✦", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(28)))
                {
                    DMNpcController.Instance.ReleasePossession();
                }
            }
            else
            {
                if (GUILayout.Button("✦ Poseer Objetivo Apuntado (Aim & Inhabit) ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
                {
                    if (DMNpcController.Instance != null)
                        DMNpcController.Instance.PossessTargetUnderCrosshair();
                }
                GUILayout.Label("<color=#888888><i>Apuntá con la retícula a cualquier enemigo o NPC y pulsa el botón para habitarlo</i></color>");
            }

            GUILayout.Space(8);

            // --- PILAR 2: BESTIARIO COMPLETO & INVOCADOR ---
            GUILayout.Label("✦ BESTIARIO COMPLETO & INVOCADOR ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            // Category Toolbar
            dmBestiaryCategoryIndex = GUILayout.Toolbar(dmBestiaryCategoryIndex, dmBestiaryCategories);
            GUILayout.Space(4);

            // Search Filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Buscar:", GUILayout.Width(50));
            dmBestiarySearchFilter = GUILayout.TextField(dmBestiarySearchFilter ?? "", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(24))) dmBestiarySearchFilter = "";
            GUILayout.EndHorizontal();

            // HP Multiplier
            GUILayout.Space(2);
            GUILayout.Label($"Escala de Vida (HP Scale): <color=#FFDF78><b>{dmSpawnHpMultiplier:F1}x</b></color>");
            dmSpawnHpMultiplier = GUILayout.HorizontalSlider(dmSpawnHpMultiplier, 1.0f, 5.0f);

            // Select active pool based on category
            MobileTypes[] pool;
            switch (dmBestiaryCategoryIndex)
            {
                case 1: pool = dmUndead; break;
                case 2: pool = dmMonsters; break;
                case 3: pool = dmDaedra; break;
                case 4: pool = dmHumanoids; break;
                case 5: pool = dmFauna; break;
                default:
                    List<MobileTypes> allList = new List<MobileTypes>();
                    allList.AddRange(dmUndead);
                    allList.AddRange(dmMonsters);
                    allList.AddRange(dmDaedra);
                    allList.AddRange(dmHumanoids);
                    allList.AddRange(dmFauna);
                    pool = allList.ToArray();
                    break;
            }

            // Filter by search text
            List<MobileTypes> filteredPool = new List<MobileTypes>();
            string filterLower = (dmBestiarySearchFilter ?? "").Trim().ToLowerInvariant();
            foreach (var m in pool)
            {
                if (string.IsNullOrEmpty(filterLower) || m.ToString().ToLowerInvariant().Contains(filterLower))
                    filteredPool.Add(m);
            }

            if (selectedDmIndex >= filteredPool.Count)
                selectedDmIndex = Mathf.Max(0, filteredPool.Count - 1);

            MobileTypes currentEnemy = filteredPool.Count > 0 ? filteredPool[selectedDmIndex] : MobileTypes.Rat;

            GUILayout.Space(4);
            GUILayout.Label($"Criatura Seleccionada: <color=#FFDF78><b>{currentEnemy}</b></color> ({filteredPool.Count} disponibles)");

            // Scrollable list of matches
            dmBestiaryScrollPos = GUILayout.BeginScrollView(dmBestiaryScrollPos, GUILayout.Height(90));
            int colCount = 0;
            GUILayout.BeginHorizontal();
            for (int i = 0; i < filteredPool.Count; i++)
            {
                bool isSel = (i == selectedDmIndex);
                if (GUILayout.Button(filteredPool[i].ToString(), isSel ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : GUI.skin.button, GUILayout.Width(130)))
                {
                    selectedDmIndex = i;
                }
                colCount++;
                if (colCount % 2 == 0 && i < filteredPool.Count - 1)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"✦ Invocación en Frente (2m) ✦", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
            {
                GameObject spawned = SpawnEnemyInFrontOfPlayer(currentEnemy);
                if (spawned != null && dmSpawnHpMultiplier > 1.05f)
                {
                    DaggerfallEntityBehaviour b = spawned.GetComponent<DaggerfallEntityBehaviour>();
                    if (b != null && b.Entity is EnemyEntity ee)
                    {
                        ee.MaxHealth = (int)(ee.MaxHealth * dmSpawnHpMultiplier);
                        ee.CurrentHealth = ee.MaxHealth;
                    }
                }
            }
            if (GUILayout.Button($"✦ Invocación en Terreno ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
            {
                SpawnEnemyAtCursor(currentEnemy, dmSpawnHpMultiplier);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("✦ DESPAWN AUTORITATIVO ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Despawn Last", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
            {
                DespawnLastEnemy();
            }
            if (GUILayout.Button("Despawn All", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
            {
                DespawnAllSpawnedEnemies();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawWorldTabContent()
        {
            GUILayout.Space(4);
            if (!NetworkSession.ShouldHandleSpawns)
            {
                GUILayout.Label("World controls & DM teleportation are only allowed on the Host/Server (or DM).", GUI.skin.box);
                return;
            }

            // World Time controls
            GUILayout.Label("✦ TIEMPO DEL MUNDO ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
            {
                var wt = DaggerfallUnity.Instance.WorldTime;
                GUILayout.Label($"Time: <color=#FFDF78>{wt.Now.ShortTimeString()} {wt.Now.DateString()}</color> ({wt.TimeScale}x)");
            }
            else
            {
                GUILayout.Label("WorldTime unavailable");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dawn (06:00)")) SetWorldTime(6, 0);
            if (GUILayout.Button("Noon (12:00)")) SetWorldTime(12, 0);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dusk (18:00)")) SetWorldTime(18, 0);
            if (GUILayout.Button("Midnight (00:00)")) SetWorldTime(0, 0);
            GUILayout.EndHorizontal();

            GUILayout.Label("Time Scale:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause (0x)")) SetWorldTimeScale(0f);
            if (GUILayout.Button("Real (1x)")) SetWorldTimeScale(1f);
            if (GUILayout.Button("Norm (12x)")) SetWorldTimeScale(12f);
            if (GUILayout.Button("Fast (60x)")) SetWorldTimeScale(60f);
            GUILayout.EndHorizontal();

            // Weather controls
            GUILayout.Space(8);
            GUILayout.Label("✦ CLIMA & ATMÓSFERA ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            WeatherManager wm = GameManager.Instance != null ? GameManager.Instance.WeatherManager : null;
            string weatherName = wm != null ? wm.CurrentWeather.ToString() : "Unknown";
            GUILayout.Label($"Current Weather: <color=#FFDF78><b>{weatherName}</b></color>");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sunny")) SetWorldWeather(WeatherType.Sunny);
            if (GUILayout.Button("Cloudy")) SetWorldWeather(WeatherType.Cloudy);
            if (GUILayout.Button("Overcast")) SetWorldWeather(WeatherType.Overcast);
            if (GUILayout.Button("Fog")) SetWorldWeather(WeatherType.Fog);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rain")) SetWorldWeather(WeatherType.Rain);
            if (GUILayout.Button("Thunder")) SetWorldWeather(WeatherType.Thunder);
            if (GUILayout.Button("Snow")) SetWorldWeather(WeatherType.Snow);
            GUILayout.EndHorizontal();

            // DM Teleportation
            GUILayout.Space(8);
            GUILayout.Label("✦ TELETRANSPORTE DM & PARTY ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            DFPosition curPixel = GameManager.Instance.PlayerGPS != null ? GameManager.Instance.PlayerGPS.CurrentMapPixel : new DFPosition(0, 0);
            string curLoc = GameManager.Instance.PlayerGPS != null && GameManager.Instance.PlayerGPS.CurrentLocation.Loaded
                ? GameManager.Instance.PlayerGPS.CurrentLocation.Name
                : "Wilderness";
            GUILayout.Label($"Current: <b>({curPixel.X}, {curPixel.Y}) - {curLoc}</b>");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Map X:", GUILayout.Width(45));
            dmTpInputX = GUILayout.TextField(dmTpInputX, GUILayout.Width(45));
            GUILayout.Label("Y:", GUILayout.Width(20));
            dmTpInputY = GUILayout.TextField(dmTpInputY, GUILayout.Width(45));
            if (GUILayout.Button("Here", GUILayout.Width(45)))
            {
                dmTpInputX = curPixel.X.ToString();
                dmTpInputY = curPixel.Y.ToString();
            }
            GUILayout.EndHorizontal();

            int.TryParse(dmTpInputX, out int dmTargetX);
            int.TryParse(dmTpInputY, out int dmTargetY);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"TP Me ({dmTargetX}, {dmTargetY})", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
            {
                TeleportLocalPlayer(dmTargetX, dmTargetY);
                DaggerfallUI.AddHUDText($"[DM TP] Teleported to ({dmTargetX}, {dmTargetY})");
            }
            if (GUILayout.Button($"Warp Party ({dmTargetX}, {dmTargetY})", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(26)))
            {
                if (NetworkSession.IsHost)
                {
                    ExecuteHostAuthoritativeTravel(dmTargetX, dmTargetY, false, $"Coordinates ({dmTargetX}, {dmTargetY})|Iliac Bay|0|0");
                }
                else
                {
                    InitiatePartyTravelVote($"Coords ({dmTargetX}, {dmTargetY})", "Iliac Bay", dmTargetX, dmTargetY, 0, 0, 0, false, false, false);
                }
            }
            GUILayout.EndHorizontal();

            // Quick Quest Teleports
            ulong[] activeQuests = QuestMachine.Instance != null ? QuestMachine.Instance.GetAllActiveQuests() : null;
            if (activeQuests != null && activeQuests.Length > 0)
            {
                Quest firstQuest = QuestMachine.Instance.GetQuest(activeQuests[0]);
                if (firstQuest != null)
                {
                    QuestResource[] places = firstQuest.GetAllResources(typeof(Place));
                    Place objPlace = null;
                    if (places != null)
                    {
                        foreach (var pRes in places)
                        {
                            Place p = pRes as Place;
                            if (p != null && !string.IsNullOrEmpty(p.SiteDetails.locationName))
                            {
                                objPlace = p;
                                break;
                            }
                        }
                    }

                    if (objPlace != null)
                    {
                        SiteDetails site = objPlace.SiteDetails;
                        DFLocation dfLoc = new DFLocation();
                        if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.ContentReader != null &&
                            DaggerfallUnity.Instance.ContentReader.GetLocation(site.regionName, site.locationName, out dfLoc))
                        {
                            DFPosition objCoords = MapsFile.LongitudeLatitudeToMapPixel(dfLoc.MapTableData.Longitude, dfLoc.MapTableData.Latitude);
                            GUILayout.Space(2);
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button($"TP Quest: {site.locationName}", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(24)))
                            {
                                TeleportLocalPlayer(objCoords.X, objCoords.Y);
                                DaggerfallUI.AddHUDText($"[DM TP] Teleported to {site.locationName} ({objCoords.X}, {objCoords.Y})");
                            }
                            if (GUILayout.Button("Warp Party", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Width(90), GUILayout.Height(24)))
                            {
                                if (NetworkSession.IsHost)
                                {
                                    ExecuteHostAuthoritativeTravel(objCoords.X, objCoords.Y, false, $"{site.locationName}|{site.regionName}|0|0");
                                }
                                else
                                {
                                    InitiatePartyTravelVote(site.locationName, site.regionName, objCoords.X, objCoords.Y, 0, 0, 0, false, false, false);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("✦ ENTIDAD DUAL DM & MODO DIOS ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            bool isSpectator = IsSpectatorSpirit;
            string dualModeLabel = isSpectator
                ? "✦ Modo Actual: [ESPÍRITU ESPECTADOR (ZEUS)] (Ctrl+F8)"
                : "✦ Modo Actual: [AVATAR FÍSICO ADMIN] (Ctrl+F8)";

            if (GUILayout.Button(dualModeLabel, isSpectator ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
            {
                ToggleDmEntityMode();
            }

            GUILayout.Space(4);
            bool godOn = GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.GodMode;
            string godLabel = godOn ? "✦ God Mode (Invincible): [ON]" : "God Mode (Invincible): [OFF]";
            if (GUILayout.Button(godLabel, godOn ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : GUI.skin.button, GUILayout.Height(26)))
            {
                ToggleDmGodMode();
            }

            // --- PILAR 4: EVENTOS DEL MUNDO & CATACLISMOS ---
            GUILayout.Space(8);
            GUILayout.Label("✦ EVENTOS DEL MUNDO & CATACLISMOS ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            if (GUILayout.Button("✦ Provocar Terremoto (Earthquake) ✦", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(28)))
            {
                TriggerEarthquake(4.0f, 0.35f);
            }

            GUILayout.Space(4);
            GUILayout.Label("Disparadores de Audio Ambiental:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trueno")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.StormLightningThunder);
            if (GUILayout.Button("Rugido Monstruo")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.AmbientMonsterRoar);
            if (GUILayout.Button("Aullido")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.AmbientDistantHowl);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Grito")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.HighPitchScream);
            if (GUILayout.Button("Tambores")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.AmbientDrums);
            if (GUILayout.Button("Lamento")) DaggerfallUI.Instance?.PlayOneShot(SoundClips.ArenaGhost);
            GUILayout.EndHorizontal();

            // --- PILAR 5: ECONOMÍA & LEYES DM ---
            GUILayout.Space(8);
            GUILayout.Label("✦ ECONOMÍA & LEYES DM ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            GUILayout.Label("Acuñar Oro (Mint Gold):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1,000 Oro"))
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.GoldPieces += 1000;
                    DaggerfallUI.AddHUDText("[DM Oro] +1,000 piezas de oro acuñadas.");
                }
            }
            if (GUILayout.Button("+10,000 Oro"))
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.GoldPieces += 10000;
                    DaggerfallUI.AddHUDText("[DM Oro] +10,000 piezas de oro acuñadas.");
                }
            }
            if (GUILayout.Button("+50,000 Oro"))
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.PlayerEntity.GoldPieces += 50000;
                    DaggerfallUI.AddHUDText("[DM Oro] +50,000 piezas de oro acuñadas.");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("✦ Perdonar Crímenes & Recompensas (Pardon Bounties) ✦", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
            {
                PardonAllCrimes();
            }
        }

        private void DrawPartyDmTabContent()
        {
            GUILayout.Space(4);
            GUILayout.Label("✦ ENTIDAD DUAL DM & SIGILO ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            bool isSpectator = IsSpectatorSpirit;
            string dualModeLabel = isSpectator
                ? "✦ Modo Actual: [ESPÍRITU ESPECTADOR (ZEUS)] (Ctrl+F8)"
                : "✦ Modo Actual: [AVATAR FÍSICO ADMIN] (Ctrl+F8)";

            if (GUILayout.Button(dualModeLabel, isSpectator ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
            {
                ToggleDmEntityMode();
            }

            GUILayout.Space(4);
            string invisLabel = isDmInvisible ? "✦ DM Invisibility: [ON]" : "DM Invisibility: [OFF]";
            if (GUILayout.Button(invisLabel, isDmInvisible ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : GUI.skin.button, GUILayout.Height(26)))
            {
                ToggleDmInvisibility();
            }

            GUILayout.Space(4);
            if (GUILayout.Button("✦ Gather Party to Host ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
            {
                GatherPartyToHost();
            }

            if (FreeCamController.Instance != null && FreeCamController.Instance.IsActive)
            {
                GUILayout.Space(4);
                bool isTopDown = FreeCamController.Instance.IsTopDownView;
                string topDownLabel = isTopDown ? "✦ Vista Cenital / Top-Down: [ACTIVA] (B)" : "Vista Cenital / Top-Down: [INACTIVA] (B)";
                if (GUILayout.Button(topDownLabel, isTopDown ? DarkFantasyPixelArtTheme.EmeraldButtonStyle : GUI.skin.button, GUILayout.Height(26)))
                {
                    FreeCamController.Instance.ToggleTopDownView();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("✦ DISTANCIA FALLOFF DE NAMETAGS ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(2);
            GUILayout.Label($"Nametag Fadeout: {nametagMaxDistance:F0}m (DM siempre visible)");
            nametagMaxDistance = GUILayout.HorizontalSlider(nametagMaxDistance, 5f, 100f);

            GUILayout.Space(8);
            GUILayout.Label("✦ COMBATE PVP & FUEGO AMIGO ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            bool newPvp = GUILayout.Toggle(isPvpAllowed, " Friendly Fire / PvP (net_pvp)");
            if (newPvp != isPvpAllowed)
            {
                SetPvpAllowed(newPvp);
            }

            GUILayout.Space(8);
            GUILayout.Label("✦ GRIMORIO DM: ENSEÑAR HECHIZOS ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            GUILayout.Label("Teach spells to yourself or connected party members:");

            List<int> targetOptions = new List<int> { 0 };
            List<string> targetNames = new List<string> { "Self (Host/DM)" };
            foreach (var kvp in connectedPlayerProfiles)
            {
                if (kvp.Key != 0)
                {
                    targetOptions.Add(kvp.Key);
                    targetNames.Add($"{kvp.Value.playerName} (ID {kvp.Key})");
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target:", GUILayout.Width(45));
            if (GUILayout.Button("<", GUILayout.Width(24)) && selectedDmSpellTargetIndex > 0)
                selectedDmSpellTargetIndex--;
            selectedDmSpellTargetIndex = Mathf.Clamp(selectedDmSpellTargetIndex, 0, targetOptions.Count - 1);
            GUILayout.Label($"<color=#FFDF78><b>{targetNames[selectedDmSpellTargetIndex]}</b></color>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(24)) && selectedDmSpellTargetIndex < targetOptions.Count - 1)
                selectedDmSpellTargetIndex++;
            GUILayout.EndHorizontal();

            int targetConnId = targetOptions[selectedDmSpellTargetIndex];

            GUILayout.Space(4);
            GUILayout.Label("Quick Spells:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fireball")) TeachDmSpellToTarget(targetConnId, "Fireball");
            if (GUILayout.Button("Wizard's Fire")) TeachDmSpellToTarget(targetConnId, "Wizard's Fire");
            if (GUILayout.Button("Shock")) TeachDmSpellToTarget(targetConnId, "Shock");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ice Storm")) TeachDmSpellToTarget(targetConnId, "Ice Storm");
            if (GUILayout.Button("Toxic Cloud")) TeachDmSpellToTarget(targetConnId, "Toxic Cloud");
            if (GUILayout.Button("Levitate")) TeachDmSpellToTarget(targetConnId, "Levitate");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Free Action")) TeachDmSpellToTarget(targetConnId, "Free Action");
            if (GUILayout.Button("Heal")) TeachDmSpellToTarget(targetConnId, "Heal");
            if (GUILayout.Button("Invisibility")) TeachDmSpellToTarget(targetConnId, "Invisibility (Normal)");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cure Poison")) TeachDmSpellToTarget(targetConnId, "Cure Poison");
            if (GUILayout.Button("Light")) TeachDmSpellToTarget(targetConnId, "Light");
            if (GUILayout.Button("Open")) TeachDmSpellToTarget(targetConnId, "Open");
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            dmSpellFilter = GUILayout.TextField(dmSpellFilter ?? "", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(24))) dmSpellFilter = "";
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(dmSpellFilter) && dmSpellFilter.Length >= 2 && GameManager.Instance.EntityEffectBroker != null)
            {
                dmSpellListScrollPos = GUILayout.BeginScrollView(dmSpellListScrollPos, GUILayout.Height(100));
                int matches = 0;
                foreach (var s in GameManager.Instance.EntityEffectBroker.StandardSpells)
                {
                    if (!string.IsNullOrEmpty(s.spellName) && !s.spellName.StartsWith("!") &&
                        s.spellName.IndexOf(dmSpellFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matches++;
                        if (GUILayout.Button($"✦ Teach: {s.spellName}", DarkFantasyPixelArtTheme.EmeraldButtonStyle))
                        {
                            TeachDmSpellToTarget(targetConnId, s.spellName, s.index);
                        }
                        if (matches >= 20) break;
                    }
                }
                if (matches == 0)
                    GUILayout.Label("<color=#888888><i>No matching spells found.</i></color>");
                GUILayout.EndScrollView();
            }
        }

        private void DrawLootWindow(int windowId)
        {
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            dmLootScrollPosition = GUILayout.BeginScrollView(dmLootScrollPosition);
            DrawLootWindowContent();
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            if (GUILayout.Button("Close (Ctrl+F12 / loot_gui)", DarkFantasyPixelArtTheme.CrimsonButtonStyle))
            {
                showLootGui = false;
            }
            GUI.DragWindow();
        }

        private void DrawLootWindowContent()
        {
            GUILayout.Space(4);
            GUILayout.Label($"Role: <color=#FFDF78>{NetworkSession.GetStatusString()}</color>");

            if (!NetworkSession.ShouldHandleSpawns)
            {
                GUILayout.Space(5);
                GUILayout.Label("Loot spawning is only allowed on the Host/Server. Pure clients cannot spawn loot containers.", GUI.skin.box);
            }
            else
            {
                // Section 1: Spawn Corpse
                GUILayout.Space(5);
                GUILayout.Label("✦ 1. GENERADOR DE CADÁVERES CON BOTÍN ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(4);
                MobileTypes currentEnemy = lootEnemies[selectedLootEnemyIndex];
                GUILayout.Label($"Corpse: <color=#FFDF78><b>{currentEnemy}</b></color>");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("< Prev") && selectedLootEnemyIndex > 0)
                {
                    selectedLootEnemyIndex--;
                }
                if (GUILayout.Button("Next >") && selectedLootEnemyIndex < lootEnemies.Length - 1)
                {
                    selectedLootEnemyIndex++;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (GUILayout.Button($"✦ Spawn Corpse: {currentEnemy} ✦", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(30)))
                {
                    SpawnCorpseInFrontOfPlayer(currentEnemy);
                }

                GUILayout.Space(5);
                GUILayout.Label("Quick Corpse Spawns:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rat"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.Rat);
                if (GUILayout.Button("Skeleton"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.SkeletalWarrior);
                if (GUILayout.Button("Zombie"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.Zombie);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Thief"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.Thief);
                if (GUILayout.Button("Bear"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.GrizzlyBear);
                if (GUILayout.Button("Orc"))
                    SpawnCorpseInFrontOfPlayer(MobileTypes.Orc);
                GUILayout.EndHorizontal();

                // Section 2: Spawn Chests & Treasures
                GUILayout.Space(8);
                GUILayout.Label("✦ 2. COFRES & TESOROS ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(4);
                if (GUILayout.Button("Spawn Dungeon Chest (Treasure Mix)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
                {
                    SpawnChestInFrontOfPlayer("Dungeon Chest");
                }
                if (GUILayout.Button("Spawn Gold Pile (500 Gold)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
                {
                    SpawnChestInFrontOfPlayer("Gold Pile");
                }
                if (GUILayout.Button("Spawn Weapon Cache (Assorted Weapons)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
                {
                    SpawnChestInFrontOfPlayer("Weapon Cache");
                }
                if (GUILayout.Button("Spawn Magic & Potions Stash", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
                {
                    SpawnChestInFrontOfPlayer("Magic & Potions");
                }

                // Section 3: Despawn Spawned Loot
                GUILayout.Space(8);
                GUILayout.Label("✦ 3. LIMPIEZA DE BOTÍN ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Despawn Last", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
                {
                    DespawnLastLoot();
                }
                if (GUILayout.Button("Despawn All", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
                {
                    DespawnAllLoot();
                }
                GUILayout.EndHorizontal();

                // Section 4: Loot Forge
                GUILayout.Space(8);
                GUILayout.Label("✦ 4. FORJA DE OBJETOS PERSONALIZADOS (LOOT FORGE) ✦", DarkFantasyPixelArtTheme.SectionBannerStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(4);

                dmForgeGroupIndex = GUILayout.Toolbar(dmForgeGroupIndex, new string[] { "Armas", "Armaduras" });
                GUILayout.Space(4);

                if (dmForgeGroupIndex == 0) // Armas
                {
                    GUILayout.Label($"Arma: <color=#FFDF78><b>{dmForgeWeapons[dmForgeWeaponIndex]}</b></color>");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("< Prev Arma") && dmForgeWeaponIndex > 0)
                        dmForgeWeaponIndex--;
                    if (GUILayout.Button("Next Arma >") && dmForgeWeaponIndex < dmForgeWeapons.Length - 1)
                        dmForgeWeaponIndex++;
                    GUILayout.EndHorizontal();

                    GUILayout.Space(3);
                    GUILayout.Label($"Material: <color=#66DDFF><b>{dmForgeMaterials[dmForgeMaterialIndex]}</b></color>");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("< Prev Material") && dmForgeMaterialIndex > 0)
                        dmForgeMaterialIndex--;
                    if (GUILayout.Button("Next Material >") && dmForgeMaterialIndex < dmForgeMaterials.Length - 1)
                        dmForgeMaterialIndex++;
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("✦ En Inventario ✦", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
                    {
                        ForgeCustomWeapon(dmForgeWeapons[dmForgeWeaponIndex], dmForgeMaterials[dmForgeMaterialIndex], true);
                    }
                    if (GUILayout.Button("✦ En Suelo ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
                    {
                        ForgeCustomWeapon(dmForgeWeapons[dmForgeWeaponIndex], dmForgeMaterials[dmForgeMaterialIndex], false);
                    }
                    GUILayout.EndHorizontal();
                }
                else // Armaduras
                {
                    GUILayout.Label($"Pieza: <color=#FFDF78><b>{dmForgeArmors[dmForgeArmorIndex]}</b></color>");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("< Prev Pieza") && dmForgeArmorIndex > 0)
                        dmForgeArmorIndex--;
                    if (GUILayout.Button("Next Pieza >") && dmForgeArmorIndex < dmForgeArmors.Length - 1)
                        dmForgeArmorIndex++;
                    GUILayout.EndHorizontal();

                    int safeMatIndex = Mathf.Clamp(dmForgeMaterialIndex, 0, dmForgeArmorMaterials.Length - 1);
                    GUILayout.Space(3);
                    GUILayout.Label($"Material: <color=#66DDFF><b>{dmForgeArmorMaterials[safeMatIndex]}</b></color>");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("< Prev Material") && dmForgeMaterialIndex > 0)
                        dmForgeMaterialIndex--;
                    if (GUILayout.Button("Next Material >") && dmForgeMaterialIndex < dmForgeArmorMaterials.Length - 1)
                        dmForgeMaterialIndex++;
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("✦ En Inventario ✦", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
                    {
                        ForgeCustomArmor(dmForgeArmors[dmForgeArmorIndex], dmForgeArmorMaterials[safeMatIndex], true);
                    }
                    if (GUILayout.Button("✦ En Suelo ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(28)))
                    {
                        ForgeCustomArmor(dmForgeArmors[dmForgeArmorIndex], dmForgeArmorMaterials[safeMatIndex], false);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RefreshCachedSettlementsIfNeeded()
        {
            if (cachedSettlementRegionIndex == questSelectedRegionIndex && cachedSettlements.Count > 0)
                return;

            cachedSettlements.Clear();
            cachedSettlementRegionIndex = questSelectedRegionIndex;
            questSelectedSettlementIndex = 0;

            if (DaggerfallUnity.Instance == null || DaggerfallUnity.Instance.ContentReader == null || DaggerfallUnity.Instance.ContentReader.MapFileReader == null)
                return;

            DFRegion regionData = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegion(questSelectedRegionIndex);
            if (regionData.LocationCount == 0 || regionData.MapTable == null || regionData.MapNames == null)
                return;

            for (int i = 0; i < regionData.LocationCount; i++)
            {
                var entry = regionData.MapTable[i];
                if (entry.LocationType == DFRegion.LocationTypes.TownCity ||
                    entry.LocationType == DFRegion.LocationTypes.TownHamlet ||
                    entry.LocationType == DFRegion.LocationTypes.TownVillage ||
                    entry.LocationType == DFRegion.LocationTypes.Tavern)
                {
                    DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel(entry.Longitude, entry.Latitude);
                    cachedSettlements.Add(new SettlementItem
                    {
                        name = regionData.MapNames[i],
                        type = entry.LocationType,
                        locationIndex = i,
                        mapPixelX = pos.X,
                        mapPixelY = pos.Y
                    });
                }
            }
        }

        private void DrawQuestWindow(int windowId)
        {
            GUILayout.Space(4);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            questScrollPos = GUILayout.BeginScrollView(questScrollPos);
            DrawQuestWindowContent();
            GUILayout.Space(8);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            if (GUILayout.Button("Close (Ctrl+F7 / quest_gui)", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
            {
                showQuestGui = false;
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawQuestWindowContent()
        {
            GUILayout.Space(5);
            GUILayout.Label($"<b>Status:</b> {NetworkSession.GetStatusString()}");
            if (NetworkSession.IsActive)
            {
                GUILayout.Label("<color=#E8C266>Party Quests use Opt-In voting. Members who accept start the quest.</color>");
            }
            else
            {
                GUILayout.Label("<color=#D4C8AA>Singleplayer mode: quests start locally on your character.</color>");
            }

            // Section 1: Presets
            GUILayout.Space(8);
            GUILayout.Label("I. QUICK TEST PRESETS", DarkFantasyPixelArtTheme.SectionBannerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fighters: Giant Hunt\n(M0B00Y00)"))
            {
                questInputName = "M0B00Y00";
                questInputFaction = "41";
            }
            if (GUILayout.Button("Fighters: Dungeon Hunt\n(M0C00Y11)"))
            {
                questInputName = "M0C00Y11";
                questInputFaction = "41";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mages Guild: Spell\n(N0B00Y00)"))
            {
                questInputName = "N0B00Y00";
                questInputFaction = "40";
            }
            if (GUILayout.Button("Mages Guild: Errand\n(N0C00Y11)"))
            {
                questInputName = "N0C00Y11";
                questInputFaction = "40";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Thieves Guild: Job\n(T0C00Y11)"))
            {
                questInputName = "T0C00Y11";
                questInputFaction = "42";
            }
            if (GUILayout.Button("Dark Brotherhood: Kill\n(D0C00Y00)"))
            {
                questInputName = "D0C00Y00";
                questInputFaction = "67";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Commoner: Social\n(A0C00Y00)"))
            {
                questInputName = "A0C00Y00";
                questInputFaction = "0";
            }
            if (GUILayout.Button("Nobles: Court Quest\n(B0C00Y00)"))
            {
                questInputName = "B0C00Y00";
                questInputFaction = "82";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Brisienna Letter\n(_BRISIEN)"))
            {
                questInputName = "_BRISIEN";
                questInputFaction = "0";
            }
            if (GUILayout.Button("Tutorial Quest\n(_TUTOR__)"))
            {
                questInputName = "_TUTOR__";
                questInputFaction = "0";
            }
            GUILayout.EndHorizontal();

            // Section 2: Deterministic Seed & Geolocation Targeting
            GUILayout.Space(8);
            GUILayout.Label("II. DETERMINISTIC SEED & GEOLOCATION", DarkFantasyPixelArtTheme.SectionBannerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Quest Seed:", GUILayout.Width(80));
            questCustomSeedText = GUILayout.TextField(questCustomSeedText);
            if (GUILayout.Button("Random", GUILayout.Width(60)))
            {
                questCustomSeedText = UnityEngine.Random.Range(100000, 999999).ToString();
            }
            if (GUILayout.Button("0 (Auto)", GUILayout.Width(65)))
            {
                questCustomSeedText = "0";
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("<size=10><color=#AAAAAA>Seed = 0 generates an authoritative seed synchronized to all peers.</color></size>");

            GUILayout.Space(4);
            GUILayout.Label("<b>Origin & Destination Mode:</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cercanía (Local Town)", questCreatorMode == 0 ? DarkFantasyPixelArtTheme.TabActiveStyle : DarkFantasyPixelArtTheme.TabInactiveStyle))
                questCreatorMode = 0;
            if (GUILayout.Button("Destino Remoto", questCreatorMode == 1 ? DarkFantasyPixelArtTheme.TabActiveStyle : DarkFantasyPixelArtTheme.TabInactiveStyle))
                questCreatorMode = 1;
            GUILayout.EndHorizontal();

            if (questCreatorMode == 0)
            {
                GUILayout.Space(4);
                if (GameManager.Instance.PlayerGPS != null && GameManager.Instance.PlayerGPS.CurrentLocation.Loaded)
                {
                    GUILayout.Label($"<color=#90EE90>Settlement:</color> <b>{GameManager.Instance.PlayerGPS.CurrentLocation.Name}</b> ({GameManager.Instance.PlayerGPS.CurrentRegionName})");
                    GUILayout.Label("<size=10>Binds Emissary/Questgiver to closest Tavern/Guild in this settlement.</size>");
                }
                else
                {
                    string regionName = GameManager.Instance.PlayerGPS != null ? GameManager.Instance.PlayerGPS.CurrentRegionName : "Wilderness";
                    GUILayout.Label($"<color=#FFA500>Wilderness / Traveling</color> ({regionName})");
                    GUILayout.Label("<size=10>Quests with local return sites require an active settlement.</size>");
                }
            }
            else
            {
                GUILayout.Space(4);
                RefreshCachedSettlementsIfNeeded();

                string currentProvinceName = (questSelectedRegionIndex >= 0 && questSelectedRegionIndex < MapsFile.RegionNames.Length)
                    ? MapsFile.RegionNames[questSelectedRegionIndex]
                    : $"Province {questSelectedRegionIndex}";

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(28)))
                {
                    questSelectedRegionIndex = (questSelectedRegionIndex - 1 + MapsFile.RegionNames.Length) % MapsFile.RegionNames.Length;
                    RefreshCachedSettlementsIfNeeded();
                }
                GUILayout.Label($"<b>Province:</b> {currentProvinceName} ({questSelectedRegionIndex})", GUILayout.ExpandWidth(true));
                if (GUILayout.Button(">", GUILayout.Width(28)))
                {
                    questSelectedRegionIndex = (questSelectedRegionIndex + 1) % MapsFile.RegionNames.Length;
                    RefreshCachedSettlementsIfNeeded();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Search Town:", GUILayout.Width(85));
                questTownSearchFilter = GUILayout.TextField(questTownSearchFilter);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                    questTownSearchFilter = "";
                GUILayout.EndHorizontal();

                List<SettlementItem> filtered = cachedSettlements;
                if (!string.IsNullOrEmpty(questTownSearchFilter))
                    filtered = cachedSettlements.FindAll(s => s.name.IndexOf(questTownSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

                if (filtered.Count == 0)
                {
                    GUILayout.Label("<color=#FF8888>No settlements with taverns found.</color>");
                }
                else
                {
                    if (questSelectedSettlementIndex >= filtered.Count)
                        questSelectedSettlementIndex = 0;

                    SettlementItem sel = filtered[questSelectedSettlementIndex];
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", GUILayout.Width(28)))
                    {
                        questSelectedSettlementIndex = (questSelectedSettlementIndex - 1 + filtered.Count) % filtered.Count;
                    }
                    GUILayout.Label($"<b>Town:</b> {sel.name} <size=10>({sel.type})</size> [{questSelectedSettlementIndex + 1}/{filtered.Count}]", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(">", GUILayout.Width(28)))
                    {
                        questSelectedSettlementIndex = (questSelectedSettlementIndex + 1) % filtered.Count;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button($"Warp Party to {sel.name}", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(24)))
                    {
                        if (NetworkSession.IsHost)
                        {
                            ExecuteHostAuthoritativeTravel(sel.mapPixelX, sel.mapPixelY, false, $"{sel.name}|{currentProvinceName}|0|0");
                        }
                        else
                        {
                            InitiatePartyTravelVote(sel.name, currentProvinceName, sel.mapPixelX, sel.mapPixelY, 0, 0, 0, false, false, false);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            // Section 3: Custom Selection & Trigger
            GUILayout.Space(8);
            GUILayout.Label("III. QUEST OFFER & START", DarkFantasyPixelArtTheme.SectionBannerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Quest Name:", GUILayout.Width(80));
            questInputName = GUILayout.TextField(questInputName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Faction ID:", GUILayout.Width(80));
            questInputFaction = GUILayout.TextField(questInputFaction);
            GUILayout.EndHorizontal();

            // Friendly Faction selector buttons
            GUILayout.Space(3);
            GUILayout.Label("<b>Quick Faction Selector:</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Commoner\n(0)")) questInputFaction = "0";
            if (GUILayout.Button("Fighters\n(41)")) questInputFaction = "41";
            if (GUILayout.Button("Mages\n(40)")) questInputFaction = "40";
            if (GUILayout.Button("Thieves\n(42)")) questInputFaction = "42";
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dark Bro\n(67)")) questInputFaction = "67";
            if (GUILayout.Button("Nobles\n(82)")) questInputFaction = "82";
            if (GUILayout.Button("Temples\n(88)")) questInputFaction = "88";
            GUILayout.EndHorizontal();

            int.TryParse(questInputFaction, out int parsedFactionId);
            int.TryParse(questCustomSeedText, out int customSeed);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Offer to Party (Vote)", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(30)))
            {
                if (QuestMachine.Instance != null && QuestMachine.Instance.LastNPCClicked == null)
                {
                    StaticNPC[] npcs = UnityEngine.Object.FindObjectsOfType<StaticNPC>();
                    if (npcs != null && npcs.Length > 0)
                        QuestMachine.Instance.SetLastNPCClicked(npcs[0]);
                }
                InitiatePartyQuestOffer(questInputName.Trim(), parsedFactionId, customSeed);
            }
            if (GUILayout.Button("Start Locally", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(30)))
            {
                try
                {
                    if (QuestMachine.Instance != null && QuestMachine.Instance.LastNPCClicked == null)
                    {
                        StaticNPC[] npcs = UnityEngine.Object.FindObjectsOfType<StaticNPC>();
                        if (npcs != null && npcs.Length > 0)
                            QuestMachine.Instance.SetLastNPCClicked(npcs[0]);
                    }
                    if (customSeed != 0)
                        QuestMachine.Instance.CustomQuestSeed = customSeed;

                    QuestMachine.Instance.StartQuest(questInputName.Trim(), parsedFactionId);
                    DaggerfallUI.AddHUDText($"[Quest] Started '{questInputName}' locally.");
                }
                catch (Exception ex)
                {
                    DaggerfallUI.AddHUDText($"[Quest] Error: {ex.Message}");
                }
            }
            GUILayout.EndHorizontal();

            // Section 4: Active Running Quests
            GUILayout.Space(8);
            GUILayout.Label("IV. ACTIVE RUNNING QUESTS", DarkFantasyPixelArtTheme.SectionBannerStyle);
            ulong[] activeQuestUIDs = QuestMachine.Instance != null ? QuestMachine.Instance.GetAllActiveQuests() : new ulong[0];
            if (activeQuestUIDs == null || activeQuestUIDs.Length == 0)
            {
                GUILayout.Label("<color=#888888>No quests currently running.</color>");
            }
            else
            {
                GUILayout.Label($"Running Quests: <b>{activeQuestUIDs.Length}</b>");
                foreach (ulong uid in activeQuestUIDs)
                {
                    Quest q = QuestMachine.Instance.GetQuest(uid);
                    if (q == null) continue;

                    string displayName = !string.IsNullOrEmpty(q.DisplayName) ? q.DisplayName : q.QuestName;
                    bool isExpanded = expandedQuestDetails.Contains(uid);

                    GUILayout.BeginVertical(GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{q.QuestName}</b> - {displayName}\n<size=9>UID: {uid} | Faction: {q.FactionId}</size>", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(isExpanded ? "Hide" : "View", GUILayout.Width(52), GUILayout.Height(28)))
                    {
                        if (isExpanded) expandedQuestDetails.Remove(uid);
                        else expandedQuestDetails.Add(uid);
                    }
                    if (GUILayout.Button("Cancel", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(72), GUILayout.Height(28)))
                    {
                        string qName = q.QuestName;
                        int fId = q.FactionId;
                        q.QuestSuccess = false;
                        q.EndQuest();
                        if (QuestMachine.Instance != null)
                            QuestMachine.Instance.TombstoneQuest(q);
                        expandedQuestDetails.Remove(uid);
                        DaggerfallUI.AddHUDText($"[Quest] Canceled '{qName}'.");
                        BroadcastPartyQuestComplete(qName, fId, false);
                    }
                    if (GUILayout.Button("Complete", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Width(84), GUILayout.Height(28)))
                    {
                        QuestResource[] items = q.GetAllResources(typeof(Item));
                        if (items != null)
                        {
                            foreach (var itmRes in items)
                            {
                                Item itm = itmRes as Item;
                                if (itm != null && itm.DaggerfallUnityItem != null && GameManager.Instance.PlayerEntity != null)
                                {
                                    GameManager.Instance.PlayerEntity.Items.AddItem(itm.DaggerfallUnityItem);
                                }
                            }
                        }
                        string qName = q.QuestName;
                        int fId = q.FactionId;
                        q.QuestSuccess = true;
                        q.EndQuest();
                        if (QuestMachine.Instance != null)
                            QuestMachine.Instance.TombstoneQuest(q);
                        expandedQuestDetails.Remove(uid);
                        DaggerfallUI.AddHUDText($"[DM] Completed '{qName}' successfully!");
                        BroadcastPartyQuestComplete(qName, fId, true);
                    }
                    GUILayout.EndHorizontal();

                    if (isExpanded)
                    {
                        GUILayout.Space(4);

                        // Time Remaining
                        QuestResource[] clocks = q.GetAllResources(typeof(Clock));
                        string timeRemainingStr = "Infinite / No timer";
                        if (clocks != null && clocks.Length > 0)
                        {
                            foreach (var clkRes in clocks)
                            {
                                Clock clk = clkRes as Clock;
                                if (clk != null && clk.Enabled && !clk.Finished)
                                {
                                    int sec = clk.RemainingTimeInSeconds;
                                    int d = sec / 86400;
                                    int h = (sec % 86400) / 3600;
                                    int m = (sec % 3600) / 60;
                                    timeRemainingStr = d > 0 ? $"{d}d {h}h {m}m remaining" : $"{h}h {m}m remaining";
                                    break;
                                }
                            }
                        }
                        GUILayout.Label($"<color=#ADD8E6>Time Remaining:</color> {timeRemainingStr}");

                        // Locations (Objective and Questor)
                        QuestResource[] places = q.GetAllResources(typeof(Place));
                        Place objectivePlace = null;
                        Place questorPlace = null;

                        if (places != null)
                        {
                            foreach (var pRes in places)
                            {
                                Place p = pRes as Place;
                                if (p != null && !string.IsNullOrEmpty(p.SiteDetails.locationName))
                                {
                                    if (p.SiteDetails.siteType == SiteTypes.Dungeon || objectivePlace == null)
                                        objectivePlace = p;
                                    if (p.SiteDetails.siteType == SiteTypes.Town || p.SiteDetails.siteType == SiteTypes.Building)
                                        questorPlace = p;
                                }
                            }
                        }

                        // Target Objective Location
                        if (objectivePlace != null)
                        {
                            SiteDetails objSite = objectivePlace.SiteDetails;
                            DFLocation loc = new DFLocation();
                            DFPosition objPos = new DFPosition(0, 0);
                            bool hasCoords = DaggerfallUnity.Instance != null &&
                                             DaggerfallUnity.Instance.ContentReader != null &&
                                             DaggerfallUnity.Instance.ContentReader.GetLocation(objSite.regionName, objSite.locationName, out loc);
                            if (hasCoords)
                                objPos = MapsFile.LongitudeLatitudeToMapPixel(loc.MapTableData.Longitude, loc.MapTableData.Latitude);

                            string locInfo = hasCoords ? $"Map: ({objPos.X}, {objPos.Y})" : "Coords: (Unknown)";
                            string bName = !string.IsNullOrEmpty(objSite.buildingName) ? $" - {objSite.buildingName}" : "";
                            GUILayout.Label($"<color=#90EE90>Objective Site:</color> <b>{objSite.locationName}</b>{bName} ({objSite.regionName}) [{objSite.siteType}]\n<size=9>{locInfo}</size>");

                            if (hasCoords)
                            {
                                if (GUILayout.Button($"TP to Objective ({objSite.locationName})", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(22)))
                                {
                                    TeleportLocalPlayer(objPos.X, objPos.Y);
                                    DaggerfallUI.AddHUDText($"[Quest TP] Teleported to {objSite.locationName} ({objPos.X}, {objPos.Y})");
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label("<color=#90EE90>Objective Site:</color> No location assigned.");
                        }

                        // Questor Details
                        QuestResource[] persons = q.GetAllResources(typeof(Person));
                        Person questorPerson = null;
                        if (persons != null)
                        {
                            foreach (var perRes in persons)
                            {
                                Person p = perRes as Person;
                                if (p != null && p.IsQuestor)
                                {
                                    questorPerson = p;
                                    break;
                                }
                            }
                            if (questorPerson == null && persons.Length > 0)
                                questorPerson = persons[0] as Person;
                        }

                        string qName = questorPerson != null ? questorPerson.DisplayName : "Unknown";
                        string qFaction = questorPerson != null ? $" (Faction: {questorPerson.FactionData.id} - {questorPerson.FactionData.name})" : "";
                        GUILayout.Label($"<color=#FFD700>Questor / Turn-in:</color> <b>{qName}</b>{qFaction}");

                        if (questorPlace != null)
                        {
                            SiteDetails qSite = questorPlace.SiteDetails;
                            DFLocation qLoc = new DFLocation();
                            if (DaggerfallUnity.Instance != null &&
                                DaggerfallUnity.Instance.ContentReader != null &&
                                DaggerfallUnity.Instance.ContentReader.GetLocation(qSite.regionName, qSite.locationName, out qLoc))
                            {
                                DFPosition qPos = MapsFile.LongitudeLatitudeToMapPixel(qLoc.MapTableData.Longitude, qLoc.MapTableData.Latitude);
                                string qBName = !string.IsNullOrEmpty(qSite.buildingName) ? $" - {qSite.buildingName}" : "";
                                GUILayout.Label($"<size=9>Location: {qSite.locationName}{qBName} ({qSite.regionName}) at ({qPos.X}, {qPos.Y})</size>");

                                if (GUILayout.Button($"TP to Questor ({qSite.locationName})", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(22)))
                                {
                                    TeleportLocalPlayer(qPos.X, qPos.Y);
                                    DaggerfallUI.AddHUDText($"[Quest TP] Teleported to Questor at {qSite.locationName} ({qPos.X}, {qPos.Y})");
                                }
                            }
                        }
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.Space(4);
                if (GUILayout.Button("End All Active Quests", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(24)))
                {
                    foreach (ulong uid in activeQuestUIDs)
                    {
                        Quest q = QuestMachine.Instance.GetQuest(uid);
                        if (q != null) q.EndQuest();
                    }
                    expandedQuestDetails.Clear();
                    DaggerfallUI.AddHUDText("[Quest] Ended all active quests.");
                }
            }

            // Section 5: Guild & Faction Reputation
            GUILayout.Space(8);
            GUILayout.Label("V. GUILD & FACTION REPUTATION", DarkFantasyPixelArtTheme.SectionBannerStyle);

            // Build list of connected party members
            List<int> repPartyIds = new List<int>();
            if (NetworkSession.IsHost || connectedPlayerProfiles.ContainsKey(0))
                repPartyIds.Add(0); // Host

            foreach (var kvp in connectedPlayerProfiles)
            {
                if (kvp.Key != 0 && !repPartyIds.Contains(kvp.Key))
                    repPartyIds.Add(kvp.Key);
            }

            if (repPartyIds.Count == 0)
                repPartyIds.Add(0);

            if (selectedRepPlayerIndex >= repPartyIds.Count)
                selectedRepPlayerIndex = repPartyIds.Count - 1;
            if (selectedRepPlayerIndex < 0)
                selectedRepPlayerIndex = 0;

            int targetConnId = repPartyIds[selectedRepPlayerIndex];
            string targetPlayerName = "Host / Local";
            if (targetConnId == 0)
            {
                targetPlayerName = (GameManager.Instance.PlayerEntity != null) ? $"{GameManager.Instance.PlayerEntity.Name} (Host)" : "Host";
            }
            else if (connectedPlayerProfiles.TryGetValue(targetConnId, out var prof))
            {
                targetPlayerName = $"{prof.playerName} (Conn {targetConnId})";
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(24)))
            {
                selectedRepPlayerIndex = (selectedRepPlayerIndex - 1 + repPartyIds.Count) % repPartyIds.Count;
            }
            GUILayout.Label($"<b>Personaje:</b> <color=#ADD8E6>{targetPlayerName}</color> [{selectedRepPlayerIndex + 1}/{repPartyIds.Count}]", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(24)))
            {
                selectedRepPlayerIndex = (selectedRepPlayerIndex + 1) % repPartyIds.Count;
            }
            if (NetworkSession.IsHost && GUILayout.Button("Sync", GUILayout.Width(45), GUILayout.Height(24)))
            {
                RequestPartyReputations();
            }
            GUILayout.EndHorizontal();

            var factionList = new (string name, int id)[]
            {
                ("Fighters Guild", 41),
                ("Mages Guild", 40),
                ("Thieves Guild", 42),
                ("Dark Brotherhood", 67),
                ("Temples (Generic)", 88),
                ("Nobility (Generic)", 82),
                ("Commoners (Generic)", 0)
            };

            foreach (var fac in factionList)
            {
                int curRep = 0;
                if (targetConnId == 0 || targetConnId == localConnectionId)
                {
                    if (GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.FactionData != null)
                        curRep = GameManager.Instance.PlayerEntity.FactionData.GetReputation(fac.id);
                }
                else
                {
                    if (remotePlayerReputations.TryGetValue(targetConnId, out var facDict) && facDict.TryGetValue(fac.id, out int r))
                        curRep = r;
                }

                string colorTag = curRep > 0 ? "#88FF88" : (curRep < 0 ? "#FF8888" : "#FFFFFF");

                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{fac.name}:</b> <color={colorTag}>{curRep}</color>", GUILayout.Width(150));

                if (NetworkSession.IsHost)
                {
                    if (GUILayout.Button("-5", GUILayout.Width(30), GUILayout.Height(22)))
                    {
                        AdjustPlayerFactionReputation(targetConnId, fac.id, fac.name, -5);
                    }
                    if (GUILayout.Button("-1", GUILayout.Width(30), GUILayout.Height(22)))
                    {
                        AdjustPlayerFactionReputation(targetConnId, fac.id, fac.name, -1);
                    }
                    if (GUILayout.Button("+1", GUILayout.Width(30), GUILayout.Height(22)))
                    {
                        AdjustPlayerFactionReputation(targetConnId, fac.id, fac.name, 1);
                    }
                    if (GUILayout.Button("+5", GUILayout.Width(30), GUILayout.Height(22)))
                    {
                        AdjustPlayerFactionReputation(targetConnId, fac.id, fac.name, 5);
                    }
                    if (GUILayout.Button("+10", GUILayout.Width(35), GUILayout.Height(22)))
                    {
                        AdjustPlayerFactionReputation(targetConnId, fac.id, fac.name, 10);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        public MsgSpawnLoot CreateSpawnLootMessage(DaggerfallLoot loot)
        {
            string itemsJson = string.Empty;
            if (loot != null && loot.Items != null)
            {
                ItemData_v1[] itemArray = loot.Items.SerializeItems();
                itemsJson = SaveLoadManager.Serialize(typeof(ItemData_v1[]), itemArray, false);
            }

            return new MsgSpawnLoot
            {
                loadID = loot != null ? loot.LoadID : 0,
                containerType = loot != null ? (int)loot.ContainerType : 0,
                containerImage = loot != null ? (int)loot.ContainerImage : 0,
                position = loot != null ? loot.transform.position : Vector3.zero,
                textureArchive = loot != null ? loot.TextureArchive : 0,
                textureRecord = loot != null ? loot.TextureRecord : 0,
                entityName = loot != null && loot.entityName != null ? loot.entityName : string.Empty,
                isEnemyClass = loot != null && loot.isEnemyClass,
                itemsJson = itemsJson
            };
        }

        public void BroadcastLootSpawn(DaggerfallLoot loot)
        {
            if (loot == null)
                return;

            if (NetworkServer.active)
            {
                MsgSpawnLoot msg = CreateSpawnLootMessage(loot);
                NetworkServer.SendToAll(msg);
            }
        }

        public DaggerfallLoot SpawnCorpseInFrontOfPlayer(MobileTypes mobileType)
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogLoot("[DM Tool] Only the Server / Host can spawn corpses.");
                return null;
            }

            GameObject player = GameManager.Instance.PlayerObject;
            if (player == null)
            {
                LogLoot("[DM Tool] PlayerObject not found.");
                return null;
            }

            Transform playerTrans = player.transform;
            Vector3 spawnPos = playerTrans.position + (playerTrans.forward * 2.0f);
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 1.5f, Vector3.down, out hit, 10f))
            {
                spawnPos = hit.point;
            }

            Transform parent = GameManager.Instance.IsPlayerInside ?
                (GameManager.Instance.IsPlayerInsideDungeon ? GameManager.Instance.PlayerEnterExit.Dungeon?.transform : GameManager.Instance.PlayerEnterExit.Interior?.transform) :
                GameManager.Instance.StreamingTarget?.transform;

            int archive = 401;
            int record = 1;
            string entityName = mobileType.ToString();
            bool isEnemyClass = (int)mobileType >= 128;

            if (GameObjectHelper.EnemyDict.TryGetValue((int)mobileType, out MobileEnemy mobileEnemy))
            {
                EnemyBasics.ReverseCorpseTexture(mobileEnemy.CorpseTexture, out archive, out record);
                entityName = TextManager.Instance.GetLocalizedEnemyName(mobileEnemy.ID);
            }

            ulong loadID = DaggerfallUnity.NextUID;
            DaggerfallLoot loot = GameObjectHelper.CreateLootContainer(
                LootContainerTypes.CorpseMarker,
                InventoryContainerImages.Corpse2,
                spawnPos,
                parent,
                archive,
                record,
                loadID);

            if (loot != null)
            {
                loot.LoadID = loadID;
                loot.entityName = entityName;
                loot.isEnemyClass = isEnemyClass;
                loot.customDrop = true;
                loot.playerOwned = false;
                loot.WorldContext = GameManager.Instance.PlayerEnterExit.WorldContext;

                if (!GameManager.Instance.IsPlayerInside && GameManager.Instance.StreamingWorld != null)
                {
                    GameManager.Instance.StreamingWorld.TrackLooseObject(loot.gameObject, true);
                }

                if (GameObjectHelper.EnemyDict.TryGetValue((int)mobileType, out MobileEnemy me) && !string.IsNullOrEmpty(me.LootTableKey))
                {
                    DaggerfallLoot.GenerateItems(me.LootTableKey, loot.Items);
                }

                if (loot.Items.Count == 0)
                {
                    loot.Items.AddItem(ItemBuilder.CreateGoldPieces(UnityEngine.Random.Range(5, 50)));
                }

                spawnedLootContainers.Add(loot);
                BroadcastLootSpawn(loot);
                LogLoot($"[DM Loot] Authoritatively spawned corpse for {entityName} at {spawnPos} (LoadID: {loadID}).");
            }

            return loot;
        }

        public DaggerfallLoot SpawnChestInFrontOfPlayer(string chestType)
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogLoot("[DM Tool] Only the Server / Host can spawn loot containers.");
                return null;
            }

            GameObject player = GameManager.Instance.PlayerObject;
            if (player == null)
            {
                LogLoot("[DM Tool] PlayerObject not found.");
                return null;
            }

            Transform playerTrans = player.transform;
            Vector3 spawnPos = playerTrans.position + (playerTrans.forward * 2.0f);
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 1.5f, Vector3.down, out hit, 10f))
            {
                spawnPos = hit.point;
            }

            Transform parent = GameManager.Instance.IsPlayerInside ?
                (GameManager.Instance.IsPlayerInsideDungeon ? GameManager.Instance.PlayerEnterExit.Dungeon?.transform : GameManager.Instance.PlayerEnterExit.Interior?.transform) :
                GameManager.Instance.StreamingTarget?.transform;

            int archive = DaggerfallLootDataTables.randomTreasureArchive;
            int record = 0;
            InventoryContainerImages containerImage = InventoryContainerImages.Chest;
            string entityName = chestType;

            switch (chestType)
            {
                case "Gold Pile":
                    record = 20;
                    containerImage = InventoryContainerImages.Ground;
                    break;
                case "Weapon Cache":
                    record = 28;
                    containerImage = InventoryContainerImages.Chest;
                    break;
                case "Magic & Potions":
                    record = 43;
                    containerImage = InventoryContainerImages.Magic;
                    break;
                case "Dungeon Chest":
                default:
                    record = 0;
                    containerImage = InventoryContainerImages.Chest;
                    break;
            }

            ulong loadID = DaggerfallUnity.NextUID;
            DaggerfallLoot loot = GameObjectHelper.CreateLootContainer(
                LootContainerTypes.DroppedLoot,
                containerImage,
                spawnPos,
                parent,
                archive,
                record,
                loadID);

            if (loot != null)
            {
                loot.LoadID = loadID;
                loot.entityName = entityName;
                loot.isEnemyClass = false;
                loot.customDrop = true;
                loot.playerOwned = false;
                loot.WorldContext = GameManager.Instance.PlayerEnterExit.WorldContext;

                if (!GameManager.Instance.IsPlayerInside && GameManager.Instance.StreamingWorld != null)
                {
                    GameManager.Instance.StreamingWorld.TrackLooseObject(loot.gameObject, true);
                }

                switch (chestType)
                {
                    case "Gold Pile":
                        loot.Items.AddItem(ItemBuilder.CreateGoldPieces(500));
                        break;
                    case "Weapon Cache":
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Longsword, WeaponMaterialTypes.Steel));
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Claymore, WeaponMaterialTypes.Silver));
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Dagger, WeaponMaterialTypes.Elven));
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Battle_Axe, WeaponMaterialTypes.Dwarven));
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Short_Bow, WeaponMaterialTypes.Iron));
                        loot.Items.AddItem(ItemBuilder.CreateGoldPieces(50));
                        break;
                    case "Magic & Potions":
                        loot.Items.AddItem(ItemBuilder.CreateRandomPotion());
                        loot.Items.AddItem(ItemBuilder.CreateRandomPotion());
                        loot.Items.AddItem(ItemBuilder.CreateRandomPotion());
                        loot.Items.AddItem(ItemBuilder.CreateGoldPieces(250));
                        break;
                    case "Dungeon Chest":
                    default:
                        loot.Items.AddItem(ItemBuilder.CreateGoldPieces(UnityEngine.Random.Range(100, 300)));
                        loot.Items.AddItem(ItemBuilder.CreateArmor(Genders.Male, Races.Breton, Armor.Cuirass, ArmorMaterialTypes.Iron));
                        loot.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Longsword, WeaponMaterialTypes.Steel));
                        loot.Items.AddItem(ItemBuilder.CreateRandomPotion());
                        break;
                }

                spawnedLootContainers.Add(loot);
                BroadcastLootSpawn(loot);
                LogLoot($"[DM Loot] Authoritatively spawned {entityName} at {spawnPos} (LoadID: {loadID}).");
            }

            return loot;
        }

        public bool DespawnLastLoot()
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogLoot("[DM Tool] Only the Server / Host can despawn loot containers.");
                return false;
            }

            while (spawnedLootContainers.Count > 0)
            {
                int lastIdx = spawnedLootContainers.Count - 1;
                DaggerfallLoot loot = spawnedLootContainers[lastIdx];
                spawnedLootContainers.RemoveAt(lastIdx);

                if (loot != null && loot.gameObject != null)
                {
                    ulong loadID = loot.LoadID;
                    DFUNetworkLootRegistry.Unregister(loadID);

                    if (NetworkServer.active)
                    {
                        NetworkServer.SendToAll(new MsgDespawnLoot { loadID = loadID });
                    }

                    Destroy(loot.gameObject);
                    LogLoot($"[DM Loot] Despawned loot container {loadID}.");
                    return true;
                }
            }

            return false;
        }

        public int DespawnAllLoot()
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                LogLoot("[DM Tool] Only the Server / Host can despawn loot containers.");
                return 0;
            }

            int count = 0;
            for (int i = spawnedLootContainers.Count - 1; i >= 0; i--)
            {
                DaggerfallLoot loot = spawnedLootContainers[i];
                if (loot != null && loot.gameObject != null)
                {
                    ulong loadID = loot.LoadID;
                    DFUNetworkLootRegistry.Unregister(loadID);

                    if (NetworkServer.active)
                    {
                        NetworkServer.SendToAll(new MsgDespawnLoot { loadID = loadID });
                    }

                    Destroy(loot.gameObject);
                    count++;
                }
            }
            spawnedLootContainers.Clear();
            LogLoot($"[DM Loot] Despawned all ({count}) spawned loot containers.");
            return count;
        }

        #endregion

        #region World Time & Weather Synchronization

        public void BroadcastWorldTime(bool isImmediate = false)
        {
            if (!NetworkServer.active) return;
            if (DaggerfallUnity.Instance == null || DaggerfallUnity.Instance.WorldTime == null) return;

            ulong currentSeconds = DaggerfallUnity.Instance.WorldTime.Now.ToSeconds();
            float timeScale = DaggerfallUnity.Instance.WorldTime.TimeScale;

            MsgWorldTimeSync msg = new MsgWorldTimeSync
            {
                timeInSeconds = currentSeconds,
                timeScale = timeScale,
                isImmediate = isImmediate
            };

            NetworkServer.SendToAll(msg);
            lastBroadcastTimeInSeconds = currentSeconds;
            worldTimeSyncTimer = 0f;

            if (LogTimeEnabled)
                LogTime($"[DFU Time] Host broadcasting authoritative world time: {DaggerfallUnity.Instance.WorldTime.Now.ShortTimeString()} {DaggerfallUnity.Instance.WorldTime.Now.DateString()} (scale: {timeScale}x, immediate: {isImmediate})");
        }

        public void BroadcastWeather(WeatherType weather, bool isImmediate = false)
        {
            if (!NetworkServer.active) return;

            MsgWeatherSync msg = new MsgWeatherSync
            {
                weatherType = (byte)weather,
                isImmediate = isImmediate
            };

            NetworkServer.SendToAll(msg);
        }

        private void OnServerWeatherChanged(WeatherType weather)
        {
            if (!NetworkServer.active) return;

            LogWeather($"[DFU Weather] Host changed weather to {weather}. Broadcasting to peers.");
            BroadcastWeather(weather, false);
        }

        private void OnClientReceiveWorldTimeSync(MsgWorldTimeSync msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (DaggerfallUnity.Instance == null || DaggerfallUnity.Instance.WorldTime == null)
                return;

            WorldTime worldTime = DaggerfallUnity.Instance.WorldTime;
            ulong clientSeconds = worldTime.Now.ToSeconds();
            worldTime.TimeScale = msg.timeScale;

            long diff = (long)msg.timeInSeconds - (long)clientSeconds;

            if (msg.isImmediate || Math.Abs(diff) > 2)
            {
                worldTime.Now.FromSeconds(msg.timeInSeconds);
                LogTime($"[DFU Time] Client synced authoritative world time: {worldTime.Now.ShortTimeString()} {worldTime.Now.DateString()} (drift: {diff}s, immediate: {msg.isImmediate})");
            }
        }

        private void OnClientReceiveWeatherSync(MsgWeatherSync msg)
        {
            if (NetworkSession.IsHost)
                return;

            WeatherType targetWeather = (WeatherType)msg.weatherType;
            LogWeather($"[DFU Weather] Client received authoritative weather: {targetWeather} (immediate={msg.isImmediate})");

            WeatherManager wm = GameManager.Instance != null ? GameManager.Instance.WeatherManager : null;
            if (wm != null)
            {
                wm.SetWeather(targetWeather);
            }
        }

        public void SetWorldTime(int hour, int minute = 0)
        {
            if (!NetworkSession.IsHost && NetworkSession.IsActive)
            {
                LogTime("[DFU Time] Only the host can set world time.");
                return;
            }

            if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
            {
                DaggerfallDateTime dt = DaggerfallUnity.Instance.WorldTime.Now;
                dt.Hour = Mathf.Clamp(hour, 0, 23);
                dt.Minute = Mathf.Clamp(minute, 0, 59);
                dt.Second = 0f;

                BroadcastWorldTime(true);
                LogTime($"[DFU Time] World time set to {dt.ShortTimeString()} {dt.DateString()}.");
            }
        }

        public void SetWorldTimeScale(float scale)
        {
            if (!NetworkSession.IsHost && NetworkSession.IsActive)
            {
                LogTime("[DFU Time] Only the host can set time scale.");
                return;
            }

            if (DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
            {
                DaggerfallUnity.Instance.WorldTime.TimeScale = Mathf.Max(0f, scale);
                BroadcastWorldTime(true);
                LogTime($"[DFU Time] World time scale set to {DaggerfallUnity.Instance.WorldTime.TimeScale}.");
            }
        }

        public void SetWorldWeather(WeatherType weather)
        {
            if (!NetworkSession.IsHost && NetworkSession.IsActive)
            {
                LogWeather("[DFU Weather] Only the host can set weather.");
                return;
            }

            WeatherManager wm = GameManager.Instance != null ? GameManager.Instance.WeatherManager : null;
            if (wm != null)
            {
                wm.SetWeather(weather);
            }
        }

        #endregion

        #region Party Decision & Voting System

        /// <summary>
        /// Initiates a party-wide vote (Rest, Loiter, Fast Travel, Quest, etc.).
        /// Works symmetrically for both Host and pure Clients.
        /// </summary>
        public bool InitiatePartyVote(PartyVoteType voteType, VoteConsensusPolicy policy, string title, string details, int intParam1, int intParam2, bool boolParam1, bool boolParam2, string stringParam)
        {
            if (!HasMultiplePlayers)
                return false;

            if (NetworkServer.active)
            {
                if (isVoteActive)
                {
                    DaggerfallUI.AddHUDText("[Party] Another vote is already active.");
                    return true;
                }

                isVoteActive = true;
                currentVoteType = voteType;
                currentVotePolicy = policy;
                currentVoteTitle = title;
                currentVoteDetails = details;
                currentVoteInitiator = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Host";
                currentVoteInitiatorConnId = 0;
                currentVoteApprovedClients.Clear();
                currentVoteApprovedClients.Add(0);
                currentVoteTimer = DefaultVoteTimeout;
                currentVoteBroadcastTimer = VoteBroadcastInterval;
                localClientHasVoted = true;

                voteIntParam1 = intParam1;
                voteIntParam2 = intParam2;
                voteBoolParam1 = boolParam1;
                voteBoolParam2 = boolParam2;
                voteStringParam = stringParam;
                currentVoteRequiredGold = 0;
                if (voteType == PartyVoteType.FastTravel && !string.IsNullOrEmpty(stringParam))
                {
                    string[] parts = stringParam.Split('|');
                    if (parts.Length > 3 && int.TryParse(parts[3], out int cost))
                        currentVoteRequiredGold = cost;
                }

                // Initialize party members: Host is already "Yes", other members are "Pending"
                currentVoteMemberStatuses.Clear();
                currentVoteMemberStatuses[0] = "Yes";
                foreach (var c in connectedClients)
                {
                    if (c != null && c.connectionId != 0)
                        currentVoteMemberStatuses[c.connectionId] = "Pending";
                }
                foreach (var kvp in connectedPlayerProfiles)
                {
                    if (kvp.Key != 0 && !currentVoteMemberStatuses.ContainsKey(kvp.Key))
                        currentVoteMemberStatuses[kvp.Key] = "Pending";
                }

                BroadcastVoteUpdate();
                DaggerfallUI.AddHUDText($"[Party] Waiting for party consensus on {title}...");
                return true;
            }

            if (NetworkClient.active && !NetworkServer.active)
            {
                localClientHasVoted = false;
                NetworkClient.Send(new MsgPartyVoteRequest
                {
                    voteType = (byte)voteType,
                    policy = (byte)policy,
                    initiatorName = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player",
                    title = title,
                    details = details,
                    intParam1 = intParam1,
                    intParam2 = intParam2,
                    boolParam1 = boolParam1,
                    boolParam2 = boolParam2,
                    stringParam = stringParam
                });
                DaggerfallUI.AddHUDText($"[Party] Requested party consensus on {title}...");
                return true;
            }

            return false;
        }

        public bool InitiatePartyRestVote(int hours, bool isLoiter)
        {
            PartyVoteType type = isLoiter ? PartyVoteType.Loiter : PartyVoteType.Rest;
            string title = isLoiter ? "[ Party Loiter ]" : "[ Party Rest ]";
            string details = isLoiter ? $"Loiter {hours}h" : $"Rest {hours}h";
            return InitiatePartyVote(type, VoteConsensusPolicy.Unanimous, title, details, hours, 0, isLoiter, false, "");
        }

        public bool InitiatePartyTravelVote(string destinationName, string regionName, int mapPixelX, int mapPixelY, int travelDays, int travelMins, int totalCost, bool speedCautious, bool sleepModeInn, bool travelShip)
        {
            string title = "[ Fast Travel ]";
            string details = $"{destinationName} ({travelDays}d, {totalCost}g)";
            string packed = $"{destinationName}|{regionName}|{travelMins}|{totalCost}";
            return InitiatePartyVote(PartyVoteType.FastTravel, VoteConsensusPolicy.Unanimous, title, details, mapPixelX, mapPixelY, speedCautious, sleepModeInn, packed);
        }

        public bool InitiatePartyQuestOffer(string questName, int factionId = 0, int customSeed = 0)
        {
            if (string.IsNullOrEmpty(questName))
            {
                DaggerfallUI.AddHUDText("[Quest] No quest name specified.");
                return false;
            }

            int seed = customSeed != 0 ? customSeed : UnityEngine.Random.Range(1, int.MaxValue);

            if (!NetworkSession.IsActive || !HasMultiplePlayers)
            {
                try
                {
                    QuestMachine.Instance.CustomQuestSeed = seed;
                    QuestMachine.Instance.StartQuest(questName, factionId);
                    DaggerfallUI.AddHUDText($"[Quest] Started quest '{questName}' locally (Seed: {seed}).");
                    return true;
                }
                catch (Exception ex)
                {
                    DaggerfallUI.AddHUDText($"[Quest] Error starting quest: {ex.Message}");
                    return false;
                }
            }

            string initiator = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player";
            string title = "[ Quest Offer ]";
            string details = $"Join quest '{questName}'?";
            string packedQuest = $"{questName}|{seed}";

            int questorNameSeed = QuestMachine.Instance != null && QuestMachine.Instance.LastNPCClicked != null ? QuestMachine.Instance.LastNPCClicked.Data.nameSeed : 0;
            int questorFactionId = QuestMachine.Instance != null && QuestMachine.Instance.LastNPCClicked != null ? QuestMachine.Instance.LastNPCClicked.Data.factionID : factionId;

            if (NetworkSession.IsHost)
            {
                if (isVoteActive)
                {
                    DaggerfallUI.AddHUDText("[Party] Another vote is already active.");
                    return false;
                }

                return InitiatePartyVote(PartyVoteType.QuestOffer, VoteConsensusPolicy.OptIn, title, details, questorFactionId, questorNameSeed, false, false, packedQuest);
            }
            else
            {
                NetworkClient.Send(new MsgPartyVoteRequest
                {
                    voteType = (byte)PartyVoteType.QuestOffer,
                    policy = (byte)VoteConsensusPolicy.OptIn,
                    initiatorName = initiator,
                    title = title,
                    details = details,
                    intParam1 = questorFactionId,
                    intParam2 = questorNameSeed,
                    boolParam1 = false,
                    boolParam2 = false,
                    stringParam = packedQuest
                });
                DaggerfallUI.AddHUDText($"[Party] Proposed quest '{questName}' to party...");
                return true;
            }
        }

        private void OnServerReceivePartyVoteRequest(NetworkConnection conn, MsgPartyVoteRequest msg)
        {
            if (isVoteActive)
            {
                conn.Send(new MsgPartyVoteResult { voteType = msg.voteType, approved = false, message = "Another vote is already active." });
                return;
            }

            isVoteActive = true;
            currentVoteType = (PartyVoteType)msg.voteType;
            currentVotePolicy = (VoteConsensusPolicy)msg.policy;
            currentVoteTitle = msg.title;
            currentVoteDetails = msg.details;
            currentVoteInitiator = msg.initiatorName;
            currentVoteInitiatorConnId = conn.connectionId;
            currentVoteApprovedClients.Clear();
            currentVoteTimer = DefaultVoteTimeout;
            currentVoteBroadcastTimer = VoteBroadcastInterval;
            localClientHasVoted = false; // Host needs to vote

            voteIntParam1 = msg.intParam1;
            voteIntParam2 = msg.intParam2;
            voteBoolParam1 = msg.boolParam1;
            voteBoolParam2 = msg.boolParam2;
            voteStringParam = msg.stringParam;
            currentVoteRequiredGold = 0;
            if (currentVoteType == PartyVoteType.FastTravel && !string.IsNullOrEmpty(msg.stringParam))
            {
                string[] parts = msg.stringParam.Split('|');
                if (parts.Length > 3 && int.TryParse(parts[3], out int cost))
                    currentVoteRequiredGold = cost;
            }

            // Initialize all party members as "Pending", but initiating client is "Yes"
            currentVoteMemberStatuses.Clear();
            currentVoteMemberStatuses[conn.connectionId] = "Yes";
            currentVoteApprovedClients.Add(conn.connectionId);
            currentVoteMemberStatuses[0] = "Pending";
            foreach (var c in connectedClients)
            {
                if (c != null && c.connectionId != 0 && c.connectionId != conn.connectionId)
                    currentVoteMemberStatuses[c.connectionId] = "Pending";
            }
            foreach (var kvp in connectedPlayerProfiles)
            {
                if (!currentVoteMemberStatuses.ContainsKey(kvp.Key))
                    currentVoteMemberStatuses[kvp.Key] = "Pending";
            }

            BroadcastVoteUpdate();
        }

        private void OnServerReceivePartyVoteResponse(NetworkConnection conn, MsgPartyVoteResponse msg)
        {
            if (!isVoteActive)
                return;

            string playerName = GetPlayerNameForConnection(conn.connectionId);

            if (!msg.approved)
            {
                currentVoteMemberStatuses[conn.connectionId] = "No";
                BroadcastVoteUpdate();

                if (currentVotePolicy == VoteConsensusPolicy.Unanimous)
                {
                    isVoteActive = false;
                    ClosePartyVoteWindow();
                    NetworkServer.SendToAll(new MsgPartyVoteResult
                    {
                        voteType = (byte)currentVoteType,
                        approved = false,
                        message = $"{playerName} declined the {currentVoteTitle} proposal."
                    });
                    DaggerfallUI.AddHUDText($"[Party] {playerName} declined the proposal.");
                    return;
                }
                else
                {
                    CheckVoteConsensus();
                    return;
                }
            }

            currentVoteApprovedClients.Add(conn.connectionId);
            currentVoteMemberStatuses[conn.connectionId] = "Yes";
            BroadcastVoteUpdate();
            CheckVoteConsensus();
        }

        // Legacy handlers forwarding to generic implementation
        private void OnServerReceivePartyRestVoteRequest(NetworkConnection conn, MsgPartyRestVoteRequest msg)
        {
            InitiatePartyRestVote(msg.hours, msg.isLoiter);
        }

        private void OnServerReceivePartyRestVoteResponse(NetworkConnection conn, MsgPartyRestVoteResponse msg)
        {
            OnServerReceivePartyVoteResponse(conn, new MsgPartyVoteResponse { approved = msg.approved });
        }

        private int GetApprovedVoteCount()
        {
            int count = 0;
            foreach (var status in currentVoteMemberStatuses.Values)
            {
                if (status == "Yes")
                    count++;
            }
            return count;
        }

        private void CheckVoteConsensus()
        {
            int totalMembers = currentVoteMemberStatuses.Count > 0 ? currentVoteMemberStatuses.Count : GetTotalPartyMembers();
            if (totalMembers <= 1)
                totalMembers = 2;

            LogNetwork($"[DFU Vote] Consensus check ({currentVoteType}, {currentVotePolicy}): approved={currentVoteApprovedClients.Count}/{totalMembers}");

            if (currentVotePolicy == VoteConsensusPolicy.Unanimous)
            {
                if (currentVoteApprovedClients.Count >= totalMembers)
                {
                    isVoteActive = false;
                    ClosePartyVoteWindow();
                    LogNetwork($"[DFU Vote] Unanimous consensus achieved for {currentVoteTitle}.");

                    NetworkServer.SendToAll(new MsgPartyVoteResult
                    {
                        voteType = (byte)currentVoteType,
                        approved = true,
                        message = $"{currentVoteTitle} approved by all members!",
                        intParam1 = voteIntParam1,
                        intParam2 = voteIntParam2,
                        boolParam1 = voteBoolParam1,
                        stringParam = voteStringParam
                    });

                    ExecuteApprovedVoteAction();
                }
            }
            else if (currentVotePolicy == VoteConsensusPolicy.OptIn)
            {
                bool allResponded = true;
                foreach (var status in currentVoteMemberStatuses.Values)
                {
                    if (status == "Pending")
                    {
                        allResponded = false;
                        break;
                    }
                }

                if (allResponded)
                {
                    isVoteActive = false;
                    ClosePartyVoteWindow();
                    ExecuteOptInResolution();
                }
            }
        }

        private void HandleVoteTimeout()
        {
            isVoteActive = false;
            ClosePartyVoteWindow();

            if (currentVotePolicy == VoteConsensusPolicy.Unanimous)
            {
                NetworkServer.SendToAll(new MsgPartyVoteResult
                {
                    voteType = (byte)currentVoteType,
                    approved = false,
                    message = $"{currentVoteTitle} timed out."
                });
                DaggerfallUI.AddHUDText($"[Party] {currentVoteTitle} timed out.");
            }
            else if (currentVotePolicy == VoteConsensusPolicy.OptIn)
            {
                ExecuteOptInResolution();
            }
        }

        private void ExecuteApprovedVoteAction()
        {
            switch (currentVoteType)
            {
                case PartyVoteType.Rest:
                case PartyVoteType.RestUntilHealed:
                    StartHostAuthoritativeRest(voteIntParam1, isLoiter: false);
                    break;

                case PartyVoteType.Loiter:
                    StartHostAuthoritativeRest(voteIntParam1, isLoiter: true);
                    break;

                case PartyVoteType.FastTravel:
                    ExecuteHostAuthoritativeTravel(voteIntParam1, voteIntParam2, voteBoolParam1, voteStringParam);
                    break;

                default:
                    LogNetwork($"[DFU Vote] Unhandled approved vote action: {currentVoteType}");
                    break;
            }
        }

        private void ExecuteOptInResolution()
        {
            List<int> approvedMembers = new List<int>(currentVoteApprovedClients);
            LogNetwork($"[DFU Vote] OptIn vote finished: {approvedMembers.Count} members accepted {currentVoteTitle}.");

            NetworkServer.SendToAll(new MsgPartyVoteResult
            {
                voteType = (byte)currentVoteType,
                approved = approvedMembers.Count > 0,
                message = $"{currentVoteTitle} accepted by {approvedMembers.Count} member(s).",
                intParam1 = voteIntParam1,
                intParam2 = voteIntParam2,
                boolParam1 = voteBoolParam1,
                stringParam = voteStringParam
            });

            if (currentVoteType == PartyVoteType.QuestOffer)
            {
                string rawQuestName = voteStringParam;
                string questName = rawQuestName;
                int questSeed = 0;
                if (!string.IsNullOrEmpty(rawQuestName) && rawQuestName.Contains("|"))
                {
                    string[] parts = rawQuestName.Split('|');
                    questName = parts[0];
                    int.TryParse(parts[1], out questSeed);
                }
                if (questSeed == 0)
                    questSeed = UnityEngine.Random.Range(1, int.MaxValue);

                int factionId = voteIntParam1;
                int questorNameSeed = voteIntParam2;

                if (approvedMembers.Count == 0)
                {
                    DaggerfallUI.AddHUDText($"[Party] Quest '{questName}' was not accepted by any members.");
                }
                else
                {
                    // Start on Host if Host approved (connId 0)
                    if (approvedMembers.Contains(0))
                    {
                        try
                        {
                            if (QuestMachine.Instance.LastNPCClicked == null && (questorNameSeed != 0 || factionId != 0))
                            {
                                StaticNPC[] npcs = UnityEngine.Object.FindObjectsOfType<StaticNPC>();
                                if (npcs != null)
                                {
                                    foreach (var npc in npcs)
                                    {
                                        if (questorNameSeed != 0 && npc.Data.nameSeed == questorNameSeed)
                                        {
                                            QuestMachine.Instance.SetLastNPCClicked(npc);
                                            break;
                                        }
                                        else if (factionId != 0 && npc.Data.factionID == factionId)
                                        {
                                            QuestMachine.Instance.SetLastNPCClicked(npc);
                                            break;
                                        }
                                    }
                                }
                            }

                            QuestMachine.Instance.CustomQuestSeed = questSeed;
                            QuestMachine.Instance.StartQuest(questName, factionId);
                            DaggerfallUI.AddHUDText($"[Party Quest] Accepted quest '{questName}'!");
                            LogNetwork($"[DFU Quest] Host started party quest: {questName} (Seed: {questSeed})");
                        }
                        catch (Exception ex)
                        {
                            LogNetwork($"[DFU Quest] Host failed to start quest '{questName}': {ex.Message}");
                        }
                    }

                    // Dispatch MsgPartyQuestStart to each approved client
                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && approvedMembers.Contains(conn.connectionId))
                        {
                            conn.Send(new MsgPartyQuestStart
                            {
                                questName = questName,
                                factionId = factionId,
                                questorNameSeed = questorNameSeed,
                                questorFactionId = factionId,
                                questSeed = questSeed
                            });
                            LogNetwork($"[DFU Quest] Dispatched MsgPartyQuestStart for '{questName}' (Seed: {questSeed}) to conn {conn.connectionId}");
                        }
                    }
                }
            }
        }

        private void ExecuteHostAuthoritativeTravel(int mapPixelX, int mapPixelY, bool speedCautious, string packedInfo)
        {
            string[] parts = packedInfo.Split('|');
            string destName = parts.Length > 0 ? parts[0] : "Destination";
            int travelTimeMins = parts.Length > 2 ? int.Parse(parts[2]) : 0;
            int totalCost = parts.Length > 3 ? int.Parse(parts[3]) : 0;

            // Deduct individual travel cost from Host
            if (totalCost > 0 && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.DeductGoldAmount(totalCost);
                DaggerfallUI.AddHUDText($"[Party] Paid {totalCost} gold for fast travel.");
            }

            NetworkServer.SendToAll(new MsgPartyTravelExecute
            {
                mapPixelX = mapPixelX,
                mapPixelY = mapPixelY,
                travelTimeMins = travelTimeMins,
                speedCautious = speedCautious,
                destinationName = destName,
                totalCost = totalCost,
                initiatorConnId = currentVoteInitiatorConnId
            });

            PerformPartyWarp(mapPixelX, mapPixelY, travelTimeMins, speedCautious, destName);
        }

        public void PerformPartyWarp(int mapPixelX, int mapPixelY, int travelTimeMins, bool speedCautious, string destinationName)
        {
            LogNetwork($"[DFU Travel] Performing party warp to '{destinationName}' ({mapPixelX}, {mapPixelY}) - {travelTimeMins} mins, cautious: {speedCautious}");
            DaggerfallUI.AddHUDText($"[Party] Traveling to {destinationName}...");

            // Smash HUD to black for transition feel
            if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.FadeBehaviour != null)
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();

            // Cache scene first if on ship
            if (GameManager.Instance.TransportManager.IsOnShip())
                SaveLoadManager.CacheScene(GameManager.Instance.StreamingWorld.SceneName);

            // Pre-warp: safely reparent all active puppets to persistent StreamingTarget so they aren't destroyed with old terrain
            Transform persistentParent = GameManager.Instance.StreamingWorld?.StreamingTarget;
            if (persistentParent != null)
            {
                foreach (var kvp in remotePlayerPuppets)
                {
                    if (kvp.Value != null && kvp.Value.gameObject != null)
                    {
                        kvp.Value.transform.SetParent(persistentParent, true);
                    }
                }
            }

            // Caso 12 fix: Transition out of interior or dungeon to prevent black void bug
            if (GameManager.Instance.PlayerEnterExit != null && GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    GameManager.Instance.PlayerEnterExit.TransitionDungeonExteriorImmediate();
                    GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
                }
                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
                {
                    GameManager.Instance.PlayerEnterExit.TransitionExterior(false);
                }
                else
                {
                    GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
                }
            }

            // Freeze motor and temporarily disable CharacterController so gravity does not pull player through ungenerated terrain
            if (GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.FreezeMotor = 1.5f;
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }
            if (GameManager.Instance.PlayerController != null)
            {
                GameManager.Instance.PlayerController.enabled = false;
            }

            GameManager.Instance.StreamingWorld.RestoreWorldCompensationHeight(0);
            GameManager.Instance.StreamingWorld.TeleportToCoordinates(mapPixelX, mapPixelY, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

            // Ensure exterior parent is active and player cleared of falling damage
            if (GameManager.Instance.PlayerEnterExit != null)
            {
                GameManager.Instance.PlayerEnterExit.EnableExteriorParent();
            }

            if (GameManager.Instance.AcrobatMotor != null)
            {
                GameManager.Instance.AcrobatMotor.ClearFallingDamage();
            }

            // Post-warp: ensure all party member puppets are alive, active, and parented correctly at destination
            RefreshAllRemotePlayerPuppets();
            lastLocalPlayerPos = Vector3.zero;

            StartCoroutine(EnsurePlayerGroundedCoroutine());

            // If cautious, restore vitals
            if (speedCautious && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.CurrentHealth = GameManager.Instance.PlayerEntity.MaxHealth;
                GameManager.Instance.PlayerEntity.CurrentFatigue = GameManager.Instance.PlayerEntity.MaxFatigue;
                if (!GameManager.Instance.PlayerEntity.Career.NoRegenSpellPoints)
                    GameManager.Instance.PlayerEntity.CurrentMagicka = GameManager.Instance.PlayerEntity.MaxMagicka;
            }

            // Prevent enemy spawns on arrival so party isn't overwhelmed
            if (GameManager.Instance.PlayerEntity != null)
                GameManager.Instance.PlayerEntity.PreventEnemySpawns = true;

            // On Host: advance authoritative world time and broadcast
            if (NetworkSession.IsHost && DaggerfallUnity.Instance != null && DaggerfallUnity.Instance.WorldTime != null)
            {
                DaggerfallDateTime dfDateTime = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime;
                dfDateTime.RaiseTime(travelTimeMins * 60);

                // Sunlight / vampire adjustment
                if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                {
                    if (dfDateTime.IsDay)
                    {
                        dfDateTime.RaiseTime((DaggerfallDateTime.DuskHour - dfDateTime.Hour) * 3600);
                    }
                }
                else if (speedCautious)
                {
                    if ((dfDateTime.Hour < 7) || ((dfDateTime.Hour == 7) && (dfDateTime.Minute < 10)))
                    {
                        float raiseTime = (((7 - dfDateTime.Hour) * 3600)
                                            + ((10 - dfDateTime.Minute) * 60)
                                            - dfDateTime.Second);
                        dfDateTime.RaiseTime(raiseTime);
                    }
                    else if (dfDateTime.Hour > 17)
                    {
                        float raiseTime = (((31 - dfDateTime.Hour) * 3600)
                                            + ((10 - dfDateTime.Minute) * 60)
                                            - dfDateTime.Second);
                        dfDateTime.RaiseTime(raiseTime);
                    }
                }

                BroadcastWorldTime(true);
            }

            // Raise skills after travel
            if (GameManager.Instance.PlayerEntity != null)
                GameManager.Instance.PlayerEntity.RaiseSkills();

            // Fade in from black
            if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.FadeBehaviour != null)
                DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();
        }

        private int GetTotalPartyMembers()
        {
            if (connectedPlayerProfiles.Count > 0)
                return connectedPlayerProfiles.Count;

            int count = 0;
            bool hasHost = false;
            foreach (var conn in connectedClients)
            {
                if (conn == null) continue;
                count++;
                if (conn.connectionId == 0)
                    hasHost = true;
            }

            if (NetworkSession.IsHost && !hasHost)
                count++;

            return Mathf.Max(count, 1);
        }

        private string GetPlayerNameForConnection(int connId)
        {
            if (connId == 0)
            {
                return (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && !string.IsNullOrEmpty(GameManager.Instance.PlayerEntity.Name))
                    ? GameManager.Instance.PlayerEntity.Name
                    : "Host";
            }

            if (connectedPlayerProfiles.TryGetValue(connId, out var profile) && !string.IsNullOrEmpty(profile.playerName))
            {
                return profile.playerName;
            }

            return $"Player {connId}";
        }

        private string BuildVoterStatusesString()
        {
            List<string> list = new List<string>();
            foreach (var kvp in currentVoteMemberStatuses)
            {
                string name = GetPlayerNameForConnection(kvp.Key);
                list.Add($"{name}:{kvp.Value}");
            }
            return string.Join(",", list.ToArray());
        }

        private MsgPartyRestVoteUpdate BuildCurrentVoteUpdateMsg()
        {
            int total = currentVoteMemberStatuses.Count > 0 ? currentVoteMemberStatuses.Count : GetTotalPartyMembers();
            int approved = GetApprovedVoteCount();

            return new MsgPartyRestVoteUpdate
            {
                initiatorName = currentVoteInitiator,
                initiatorConnId = currentVoteInitiatorConnId,
                hours = voteIntParam1,
                isLoiter = voteBoolParam1,
                approvedCount = approved,
                totalMembers = total,
                remainingSeconds = Mathf.Max(0f, currentVoteTimer),
                voterStatuses = BuildVoterStatusesString()
            };
        }

        private void OnServerReceivePlayerCrime(NetworkConnection conn, MsgPlayerCrime msg)
        {
            LogNetwork($"[DFU Crime] Received crime event from client {conn.connectionId}: type={msg.crimeType} at {msg.playerPosition}");
            LastCrimeConnId = conn.connectionId;
            if (GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.SpawnCityGuardsAtPosition(msg.playerPosition, true);
            }
        }

        private void BroadcastVoteUpdate()
        {
            if (!isVoteActive || !NetworkServer.active)
                return;

            int approved = GetApprovedVoteCount();
            int total = currentVoteMemberStatuses.Count > 0 ? currentVoteMemberStatuses.Count : GetTotalPartyMembers();
            float remaining = Mathf.Max(0f, currentVoteTimer);
            string statuses = BuildVoterStatusesString();

            MsgPartyVoteUpdate msg = new MsgPartyVoteUpdate
            {
                voteType = (byte)currentVoteType,
                policy = (byte)currentVotePolicy,
                initiatorName = currentVoteInitiator,
                initiatorConnId = currentVoteInitiatorConnId,
                title = currentVoteTitle,
                details = currentVoteDetails,
                approvedCount = approved,
                totalMembers = total,
                remainingSeconds = remaining,
                voterStatuses = statuses,
                requiredGold = currentVoteRequiredGold
            };

            NetworkServer.SendToAll(msg);

            // Also send legacy MsgPartyRestVoteUpdate for backwards compatibility
            NetworkServer.SendToAll(new MsgPartyRestVoteUpdate
            {
                initiatorName = currentVoteInitiator,
                initiatorConnId = currentVoteInitiatorConnId,
                hours = voteIntParam1,
                isLoiter = voteBoolParam1,
                approvedCount = approved,
                totalMembers = total,
                remainingSeconds = remaining,
                voterStatuses = statuses
            });

            if (NetworkSession.IsHost)
            {
                // Only show/open vote popup on Host if Host needs to vote (i.e. Client initiated vote)
                if (currentVoteInitiatorConnId != 0 || !localClientHasVoted)
                {
                    OpenOrUpdatePartyVoteWindow(currentVoteTitle, currentVoteDetails, approved, total, remaining, statuses);
                }
            }
        }

        private void BroadcastRestVoteUpdate()
        {
            BroadcastVoteUpdate();
        }

        private void ClosePartyVoteWindow()
        {
            if (partyVoteWindow != null)
            {
                try
                {
                    partyVoteWindow.CloseWindow();
                }
                catch (Exception ex)
                {
                    LogNetwork($"[DFU Vote] Error closing party vote window: {ex.Message}");
                }
                partyVoteWindow = null;
            }
            localClientHasVoted = false;
        }

        private void OpenOrUpdatePartyVoteWindow(string title, string details, int approvedCount, int totalMembers, float remainingSeconds, string voterStatuses)
        {
            IUserInterfaceManager uiMgr = DaggerfallUI.Instance != null ? DaggerfallUI.Instance.UserInterfaceManager : null;
            if (uiMgr == null)
                return;

            if (partyVoteWindow == null)
            {
                localClientHasVoted = false;

                IUserInterfaceWindow prevWin = uiMgr.TopWindow ?? DaggerfallUI.Instance.DaggerfallHUD;
                partyVoteWindow = new DaggerfallPartyVoteWindow(uiMgr, prevWin);
                partyVoteWindow.OnVoteSubmitted += (approved) =>
                {
                    localClientHasVoted = true;

                    // Solvency check: if this vote requires gold, verify player can afford their share
                    if (approved && currentVoteRequiredGold > 0)
                    {
                        int currentGold = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.GetGoldAmount() : 0;
                        if (currentGold < currentVoteRequiredGold)
                        {
                            DaggerfallUI.AddHUDText($"[Party] Cannot travel: you need {currentVoteRequiredGold} gold to pay your share (you have {currentGold} gold).");
                            approved = false;
                        }
                    }

                    if (NetworkServer.active)
                    {
                        if (!approved)
                        {
                            if (currentVotePolicy == VoteConsensusPolicy.Unanimous)
                            {
                                isVoteActive = false;
                                currentVoteMemberStatuses[0] = "No";
                                BroadcastVoteUpdate();
                                ClosePartyVoteWindow();
                                string declineReason = (currentVoteRequiredGold > 0 && (GameManager.Instance.PlayerEntity?.GetGoldAmount() ?? 0) < currentVoteRequiredGold)
                                    ? $"{currentVoteTitle} declined: Host cannot afford the travel cost."
                                    : $"{currentVoteTitle} was declined.";
                                NetworkServer.SendToAll(new MsgPartyVoteResult { voteType = (byte)currentVoteType, approved = false, message = declineReason });
                                DaggerfallUI.AddHUDText($"[Party] {currentVoteTitle} declined.");
                            }
                            else
                            {
                                currentVoteMemberStatuses[0] = "No";
                                BroadcastVoteUpdate();
                                CheckVoteConsensus();
                            }
                        }
                        else
                        {
                            currentVoteApprovedClients.Add(0);
                            currentVoteMemberStatuses[0] = "Yes";
                            BroadcastVoteUpdate();
                            CheckVoteConsensus();
                        }
                    }
                    else
                    {
                        NetworkClient.Send(new MsgPartyVoteResponse { approved = approved });
                        if (!approved && currentVotePolicy == VoteConsensusPolicy.Unanimous)
                        {
                            DaggerfallUI.AddHUDText($"[Party] You declined the {currentVoteTitle} proposal.");
                            ClosePartyVoteWindow();
                        }
                    }
                };

                uiMgr.PushWindow(partyVoteWindow);
            }

            partyVoteWindow.UpdateVoteState(
                title,
                details,
                approvedCount,
                totalMembers,
                remainingSeconds,
                voterStatuses,
                localClientHasVoted
            );
        }

        private void OpenOrUpdatePartyVoteWindow(MsgPartyRestVoteUpdate msg)
        {
            string action = msg.isLoiter ? "Loiter" : "Rest";
            string title = msg.isLoiter ? "[ Party Loiter ]" : "[ Party Rest ]";
            string details = $"{action} {msg.hours}h ({msg.initiatorName})";
            OpenOrUpdatePartyVoteWindow(title, details, msg.approvedCount, msg.totalMembers, msg.remainingSeconds, msg.voterStatuses);
        }

        private void OnClientReceivePartyVoteUpdate(MsgPartyVoteUpdate msg)
        {
            if (NetworkSession.IsHost)
                return;

            currentVoteType = (PartyVoteType)msg.voteType;
            currentVotePolicy = (VoteConsensusPolicy)msg.policy;
            currentVoteTitle = msg.title;
            currentVoteRequiredGold = msg.requiredGold;
            if (msg.initiatorConnId == localConnectionId)
                localClientHasVoted = true;

            OpenOrUpdatePartyVoteWindow(msg.title, msg.details, msg.approvedCount, msg.totalMembers, msg.remainingSeconds, msg.voterStatuses);
        }

        private void OnClientReceivePartyVoteResult(MsgPartyVoteResult msg)
        {
            if (NetworkSession.IsHost)
                return;

            ClosePartyVoteWindow();
            DaggerfallUI.AddHUDText($"[Party] {msg.message}");
        }

        private void OnClientReceivePartyTravelExecute(MsgPartyTravelExecute msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (msg.totalCost > 0 && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.DeductGoldAmount(msg.totalCost);
                DaggerfallUI.AddHUDText($"[Party] Paid {msg.totalCost} gold for fast travel.");
            }
            PerformPartyWarp(msg.mapPixelX, msg.mapPixelY, msg.travelTimeMins, msg.speedCautious, msg.destinationName);
        }

        private void OnClientReceivePartyQuestStart(MsgPartyQuestStart msg)
        {
            if (NetworkSession.IsHost)
                return;

            try
            {
                if (QuestMachine.Instance.LastNPCClicked == null && (msg.questorNameSeed != 0 || msg.questorFactionId != 0))
                {
                    StaticNPC[] npcs = UnityEngine.Object.FindObjectsOfType<StaticNPC>();
                    if (npcs != null)
                    {
                        foreach (var npc in npcs)
                        {
                            if (msg.questorNameSeed != 0 && npc.Data.nameSeed == msg.questorNameSeed)
                            {
                                QuestMachine.Instance.SetLastNPCClicked(npc);
                                break;
                            }
                            else if (msg.questorFactionId != 0 && npc.Data.factionID == msg.questorFactionId)
                            {
                                QuestMachine.Instance.SetLastNPCClicked(npc);
                                break;
                            }
                        }
                    }
                }

                if (msg.questSeed != 0)
                    QuestMachine.Instance.CustomQuestSeed = msg.questSeed;

                QuestMachine.Instance.StartQuest(msg.questName, msg.factionId);
                DaggerfallUI.AddHUDText($"[Party Quest] Accepted quest '{msg.questName}'!");
                LogNetwork($"[DFU Quest] Client started party quest: {msg.questName} (Faction: {msg.factionId}, Seed: {msg.questSeed})");
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Quest] Failed to start quest '{msg.questName}': {ex.Message}");
            }
        }

        #region Party Quest Completion & Reputation Sync

        public void BroadcastPartyQuestComplete(string questName, int factionId, bool success)
        {
            if (!NetworkSession.IsHost || connectedClients == null)
                return;

            MsgPartyQuestComplete msg = new MsgPartyQuestComplete
            {
                questName = questName,
                factionId = factionId,
                success = success
            };

            foreach (NetworkConnectionToClient conn in connectedClients)
            {
                if (conn != null && conn.connectionId != localConnectionId)
                {
                    conn.Send(msg);
                    LogNetwork($"[DFU Quest] Broadcast MsgPartyQuestComplete for '{questName}' (success={success}) to conn {conn.connectionId}");
                }
            }
        }

        private void OnClientReceivePartyQuestComplete(MsgPartyQuestComplete msg)
        {
            if (NetworkSession.IsHost)
                return;

            try
            {
                bool questFound = false;
                ulong[] activeQuests = QuestMachine.Instance != null ? QuestMachine.Instance.GetAllActiveQuests() : null;
                if (activeQuests != null)
                {
                    foreach (ulong uid in activeQuests)
                    {
                        Quest q = QuestMachine.Instance.GetQuest(uid);
                        if (q != null && string.Equals(q.QuestName, msg.questName, StringComparison.OrdinalIgnoreCase))
                        {
                            questFound = true;
                            q.QuestSuccess = msg.success;
                            q.EndQuest();
                            if (QuestMachine.Instance != null)
                                QuestMachine.Instance.TombstoneQuest(q);

                            expandedQuestDetails.Remove(uid);
                            if (msg.success)
                            {
                                if (msg.factionId > 0 && q.FactionId == 0 && GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.FactionData != null)
                                {
                                    GameManager.Instance.PlayerEntity.FactionData.ChangeReputation(msg.factionId, 5, true);
                                }
                                DaggerfallUI.AddHUDText($"[Party Quest] '{msg.questName}' completada con éxito (+reputación gremial).");
                                LogNetwork($"[DFU Quest] Client successfully completed party quest '{msg.questName}'.");
                            }
                            else
                            {
                                DaggerfallUI.AddHUDText($"[Party Quest] Misión '{msg.questName}' cancelada.");
                                LogNetwork($"[DFU Quest] Client canceled party quest '{msg.questName}'.");
                            }
                        }
                    }
                }

                if (!questFound)
                {
                    LogNetwork($"[DFU Quest] Received MsgPartyQuestComplete for '{msg.questName}', but quest not found in active list.");
                }

                SendReputationReportToHost();
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Quest] Error processing MsgPartyQuestComplete: {ex.Message}");
            }
        }

        public void AdjustPlayerFactionReputation(int targetConnId, int factionId, string factionName, int delta)
        {
            if (!NetworkSession.IsHost)
                return;

            if (targetConnId == 0 || targetConnId == localConnectionId)
            {
                if (GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.FactionData != null)
                {
                    var fData = GameManager.Instance.PlayerEntity.FactionData;
                    int oldRep = fData.GetReputation(factionId);
                    fData.ChangeReputation(factionId, delta, true);
                    int newRep = fData.GetReputation(factionId);

                    DaggerfallUI.AddHUDText($"[Reputación Host] {factionName}: {oldRep} -> {newRep} ({(delta >= 0 ? "+" : "")}{delta})");
                    LogNetwork($"[DFU Rep] Host adjusted local reputation for {factionName} (ID {factionId}): {oldRep} -> {newRep}");
                }
            }
            else
            {
                string pName = $"Conn {targetConnId}";
                if (connectedPlayerProfiles.TryGetValue(targetConnId, out var prof))
                    pName = prof.playerName;

                if (!remotePlayerReputations.ContainsKey(targetConnId))
                    remotePlayerReputations[targetConnId] = new Dictionary<int, int>();

                int curVal = 0;
                if (remotePlayerReputations[targetConnId].TryGetValue(factionId, out int val))
                    curVal = val;
                int nextVal = curVal + delta;
                remotePlayerReputations[targetConnId][factionId] = nextVal;

                DaggerfallUI.AddHUDText($"[Reputación {pName}] {factionName}: {curVal} -> {nextVal} ({(delta >= 0 ? "+" : "")}{delta})");
                LogNetwork($"[DFU Rep] Host adjusted remote reputation for {pName} (conn {targetConnId}) {factionName} (ID {factionId}): {curVal} -> {nextVal}");

                if (connectedClients != null)
                {
                    MsgFactionRepSync syncMsg = new MsgFactionRepSync
                    {
                        targetConnectionId = targetConnId,
                        factionId = factionId,
                        repValue = nextVal,
                        delta = delta,
                        factionName = factionName
                    };

                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && conn.connectionId == targetConnId)
                        {
                            conn.Send(syncMsg);
                            break;
                        }
                    }
                }
            }
        }

        public void AdjustFactionReputation(int factionId, string factionName, int delta)
        {
            AdjustPlayerFactionReputation(0, factionId, factionName, delta);
        }

        public void RequestPartyReputations()
        {
            if (!NetworkSession.IsHost || connectedClients == null)
                return;

            MsgRequestReputationReport msg = new MsgRequestReputationReport();
            foreach (NetworkConnectionToClient conn in connectedClients)
            {
                if (conn != null && conn.connectionId != localConnectionId)
                {
                    conn.Send(msg);
                }
            }
            LogNetwork("[DFU Rep] Host requested reputation reports from all party members.");
        }

        public void SendReputationReportToHost()
        {
            if (NetworkSession.IsHost && !NetworkClient.isConnected)
                return;

            if (GameManager.Instance.PlayerEntity == null || GameManager.Instance.PlayerEntity.FactionData == null)
                return;

            var fData = GameManager.Instance.PlayerEntity.FactionData;
            int[] keyFactionIds = new int[] { 41, 40, 42, 67, 88, 82, 0 };

            FactionRepSnapshot snapshot = new FactionRepSnapshot();
            foreach (int fid in keyFactionIds)
            {
                snapshot.items.Add(new FactionRepPair
                {
                    factionId = fid,
                    rep = fData.GetReputation(fid)
                });
            }

            string json = SaveLoadManager.Serialize(typeof(FactionRepSnapshot), snapshot, false);
            string pName = GameManager.Instance.PlayerEntity.Name;

            MsgClientReputationReport msg = new MsgClientReputationReport
            {
                connectionId = localConnectionId,
                playerName = pName,
                reputationsJson = json
            };

            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
                LogNetwork($"[DFU Rep] Sent reputation report to host (conn {localConnectionId}, player '{pName}').");
            }
        }

        private void OnServerReceiveClientReputationReport(NetworkConnection conn, MsgClientReputationReport msg)
        {
            if (!NetworkSession.IsHost)
                return;

            try
            {
                int connId = conn != null ? conn.connectionId : msg.connectionId;
                if (!string.IsNullOrEmpty(msg.reputationsJson))
                {
                    FactionRepSnapshot snapshot = (FactionRepSnapshot)SaveLoadManager.Deserialize(typeof(FactionRepSnapshot), msg.reputationsJson);
                    if (snapshot != null && snapshot.items != null)
                    {
                        if (!remotePlayerReputations.ContainsKey(connId))
                            remotePlayerReputations[connId] = new Dictionary<int, int>();

                        foreach (var item in snapshot.items)
                        {
                            remotePlayerReputations[connId][item.factionId] = item.rep;
                        }
                        LogNetwork($"[DFU Rep] Received reputation report from '{msg.playerName}' (conn {connId}) with {snapshot.items.Count} entries.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Rep] Error processing MsgClientReputationReport: {ex.Message}");
            }
        }

        private void OnClientReceiveRequestReputationReport(MsgRequestReputationReport msg)
        {
            if (NetworkSession.IsHost)
                return;

            SendReputationReportToHost();
        }

        private void OnClientReceiveFactionRepSync(MsgFactionRepSync msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (msg.targetConnectionId != -1 && msg.targetConnectionId != localConnectionId)
                return;

            if (GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.FactionData != null)
            {
                var fData = GameManager.Instance.PlayerEntity.FactionData;
                int oldRep = fData.GetReputation(msg.factionId);
                fData.ChangeReputation(msg.factionId, msg.delta, true);
                int newRep = fData.GetReputation(msg.factionId);
                DaggerfallUI.AddHUDText($"[Reputación Party] {msg.factionName}: {oldRep} -> {newRep} ({(msg.delta >= 0 ? "+" : "")}{msg.delta})");
                LogNetwork($"[DFU Rep] Client synced reputation for {msg.factionName} (ID {msg.factionId}): {oldRep} -> {newRep}");

                SendReputationReportToHost();
            }
        }

        #endregion

        #region P2P Interactive Player Menu & Map Sharing

        public void TryInteractWithNearbyPlayer()
        {
            if (GameManager.Instance.PlayerObject == null)
                return;

            Vector3 playerPos = GameManager.Instance.PlayerObject.transform.position;
            Transform camTransform = GameManager.Instance.MainCamera != null ? GameManager.Instance.MainCamera.transform : GameManager.Instance.PlayerObject.transform;
            Vector3 camForward = camTransform.forward;
            Vector3 camPos = camTransform.position;

            NativePlayerPuppet bestPuppet = null;
            float bestScore = float.MinValue;
            const float maxInteractionDist = 4.0f;

            foreach (var kvp in remotePlayerPuppets)
            {
                NativePlayerPuppet puppet = kvp.Value;
                if (puppet == null || puppet.gameObject == null || !puppet.gameObject.activeInHierarchy)
                    continue;

                float dist = Vector3.Distance(playerPos, puppet.transform.position);
                if (dist > maxInteractionDist)
                    continue;

                Vector3 toPuppet = (puppet.transform.position + Vector3.up * 0.9f - camPos).normalized;
                float dot = Vector3.Dot(camForward, toPuppet);

                float score = (maxInteractionDist - dist) + (dot > 0 ? dot * 3.0f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPuppet = puppet;
                }
            }

            if (bestPuppet != null)
            {
                OpenPlayerInteractionMenu(bestPuppet);
            }
        }

        public void OpenPlayerInteractionMenu(NativePlayerPuppet puppet)
        {
            if (puppet == null)
                return;

            interactionTargetPuppet = puppet;
            showPlayerInteractionWindow = true;
            playerInteractionWindowRect.width = 380;
            playerInteractionWindowRect.height = 270;
            playerInteractionWindowRect.x = (Screen.width - playerInteractionWindowRect.width) * 0.5f;
            playerInteractionWindowRect.y = (Screen.height - playerInteractionWindowRect.height) * 0.5f;
        }

        private void DrawPlayerInteractionWindow(int windowId)
        {
            if (interactionTargetPuppet == null || interactionTargetPuppet.gameObject == null)
            {
                showPlayerInteractionWindow = false;
                return;
            }

            float dist = 0f;
            if (GameManager.Instance.PlayerObject != null)
                dist = Vector3.Distance(GameManager.Instance.PlayerObject.transform.position, interactionTargetPuppet.transform.position);

            GUILayout.Space(4);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"<b><color=#FFD96B>{interactionTargetPuppet.playerName}</color></b>", GUILayout.ExpandWidth(true));
            GUILayout.Label($"<size=11>Clase: <b>{interactionTargetPuppet.careerName}</b> | Distancia: <b>{dist:F1}m</b></size>");
            GUILayout.EndVertical();
            GUILayout.Space(8);

            if (GUILayout.Button("Compartir Mapa y Descubrimientos", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(34)))
            {
                SendMapDiscoveriesToTarget(interactionTargetPuppet);
            }

            GUILayout.Space(4);
            if (GUILayout.Button("Iniciar Comercio Bilateral (Trade)", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(34)))
            {
                InitiateTradeInvite(interactionTargetPuppet);
            }

            GUILayout.Space(8);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            if (GUILayout.Button("Cerrar", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(26)))
            {
                showPlayerInteractionWindow = false;
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public void SendMapDiscoveriesToTarget(NativePlayerPuppet target)
        {
            if (target == null)
                return;

            showPlayerInteractionWindow = false;

            PlayerGPS gps = GameManager.Instance.PlayerGPS;
            if (gps == null)
            {
                DaggerfallUI.AddHUDText("[Mapa] Error: PlayerGPS no disponible.");
                return;
            }

            var discDict = gps.GetDiscoverySaveData();
            SharedMapBundle bundle = new SharedMapBundle();

            int totalBuildings = 0;
            if (discDict != null)
            {
                foreach (var kvp in discDict)
                {
                    var dl = kvp.Value;
                    var locItem = new SharedLocationItem
                    {
                        mapID = dl.mapID,
                        mapPixelID = dl.mapPixelID,
                        regionName = dl.regionName,
                        locationName = dl.locationName
                    };

                    if (dl.discoveredBuildings != null)
                    {
                        foreach (var bKvp in dl.discoveredBuildings)
                        {
                            var db = bKvp.Value;
                            locItem.buildings.Add(new SharedBuildingItem
                            {
                                buildingKey = db.buildingKey,
                                displayName = db.displayName,
                                oldDisplayName = db.oldDisplayName,
                                isOverrideName = db.isOverrideName,
                                factionID = db.factionID,
                                quality = db.quality,
                                buildingType = (int)db.buildingType
                            });
                            totalBuildings++;
                        }
                    }
                    bundle.locations.Add(locItem);
                }
            }

            string json = JsonUtility.ToJson(bundle);
            string myName = (GameManager.Instance.PlayerEntity != null) ? GameManager.Instance.PlayerEntity.Name : "Jugador";

            MsgShareMapData msg = new MsgShareMapData
            {
                senderConnectionId = localConnectionId,
                senderName = myName,
                targetConnectionId = target.connectionId,
                discoveryDataJson = json,
                locationCount = bundle.locations.Count,
                buildingCount = totalBuildings
            };

            if (NetworkSession.IsHost)
            {
                if (target.connectionId == 0 || target.connectionId == localConnectionId)
                {
                    ApplySharedMapData(msg);
                }
                else if (connectedClients != null)
                {
                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && conn.connectionId == target.connectionId)
                        {
                            conn.Send(msg);
                            LogNetwork($"[DFU Map] Host sent MsgShareMapData directly to conn {target.connectionId}");
                            break;
                        }
                    }
                }
            }
            else
            {
                if (NetworkClient.active)
                {
                    NetworkClient.Send(msg);
                    LogNetwork($"[DFU Map] Client sent MsgShareMapData to server for target {target.connectionId}");
                }
            }

            DaggerfallUI.AddHUDText($"[Mapa] Compartiste tus descubrimientos ({bundle.locations.Count} locaciones, {totalBuildings} edificios) con {target.playerName}.");
        }

        private void OnServerReceiveShareMapData(NetworkConnection conn, MsgShareMapData msg)
        {
            if (msg.targetConnectionId == 0 || msg.targetConnectionId == localConnectionId)
            {
                ApplySharedMapData(msg);
            }
            else
            {
                if (connectedClients != null)
                {
                    foreach (NetworkConnectionToClient clientConn in connectedClients)
                    {
                        if (clientConn != null && clientConn.connectionId == msg.targetConnectionId)
                        {
                            clientConn.Send(msg);
                            LogNetwork($"[DFU Map] Forwarded MsgShareMapData from conn {conn.connectionId} to target conn {msg.targetConnectionId}");
                            return;
                        }
                    }
                }
                LogNetwork($"[DFU Map] Target conn {msg.targetConnectionId} not found to deliver MsgShareMapData.");
            }
        }

        private void OnClientReceiveShareMapData(MsgShareMapData msg)
        {
            if (NetworkSession.IsHost && msg.targetConnectionId == localConnectionId)
                return;

            ApplySharedMapData(msg);
        }

        private void ApplySharedMapData(MsgShareMapData msg)
        {
            try
            {
                if (string.IsNullOrEmpty(msg.discoveryDataJson))
                    return;

                SharedMapBundle bundle = JsonUtility.FromJson<SharedMapBundle>(msg.discoveryDataJson);
                if (bundle == null || bundle.locations == null)
                    return;

                PlayerGPS gps = GameManager.Instance.PlayerGPS;
                if (gps == null)
                    return;

                var currentDisc = gps.GetDiscoverySaveData();
                if (currentDisc == null)
                    currentDisc = new Dictionary<int, PlayerGPS.DiscoveredLocation>();

                int newLocations = 0;
                int newBuildings = 0;

                foreach (var loc in bundle.locations)
                {
                    if (!currentDisc.TryGetValue(loc.mapPixelID, out PlayerGPS.DiscoveredLocation dl))
                    {
                        dl = new PlayerGPS.DiscoveredLocation
                        {
                            mapID = loc.mapID,
                            mapPixelID = loc.mapPixelID,
                            regionName = loc.regionName,
                            locationName = loc.locationName,
                            discoveredBuildings = new Dictionary<int, PlayerGPS.DiscoveredBuilding>()
                        };
                        currentDisc[loc.mapPixelID] = dl;
                        newLocations++;
                    }

                    if (dl.discoveredBuildings == null)
                        dl.discoveredBuildings = new Dictionary<int, PlayerGPS.DiscoveredBuilding>();

                    if (loc.buildings != null)
                    {
                        foreach (var b in loc.buildings)
                        {
                            if (!dl.discoveredBuildings.ContainsKey(b.buildingKey))
                            {
                                dl.discoveredBuildings[b.buildingKey] = new PlayerGPS.DiscoveredBuilding
                                {
                                    buildingKey = b.buildingKey,
                                    displayName = b.displayName,
                                    oldDisplayName = b.oldDisplayName,
                                    isOverrideName = b.isOverrideName,
                                    factionID = b.factionID,
                                    quality = b.quality,
                                    buildingType = (DFLocation.BuildingTypes)b.buildingType
                                };
                                newBuildings++;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(loc.regionName) && !string.IsNullOrEmpty(loc.locationName))
                    {
                        try
                        {
                            if (!gps.HasDiscoveredLocation(loc.mapPixelID))
                                gps.DiscoverLocation(loc.regionName, loc.locationName);
                        }
                        catch
                        {
                        }
                    }
                }

                gps.RestoreDiscoveryData(currentDisc);

                DaggerfallUI.AddHUDText($"[Mapa] ¡{msg.senderName} compartió sus mapas! (+{newLocations} locaciones, +{newBuildings} edificios descubiertos)");
                LogNetwork($"[DFU Map] Applied shared map data from {msg.senderName}: +{newLocations} locs, +{newBuildings} buildings.");
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Map] Error applying shared map data: {ex.Message}");
            }
        }

        #endregion

        #region P2P Bilateral Trading System

        private void DrawTradeInvitePromptWindow(int windowId)
        {
            GUILayout.Space(4);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            GUILayout.Label($"<b><color=#FFD96B>{pendingTradeInviteSenderName}</color></b> desea iniciar un intercambio comercial contigo.", GUILayout.ExpandWidth(true));
            GUILayout.Space(4);
            GUILayout.Label($"<size=11>Tiempo restante: <b>{Mathf.CeilToInt(tradeInviteTimeout)}s</b></size>");
            GUILayout.Space(8);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Aceptar", DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(30)))
            {
                showTradeInvitePrompt = false;
                RespondToTradeInvite(pendingTradeInviteSenderConnId, true);
            }
            if (GUILayout.Button("Rechazar", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(30)))
            {
                showTradeInvitePrompt = false;
                RespondToTradeInvite(pendingTradeInviteSenderConnId, false);
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static string GetSafeItemName(DaggerfallUnityItem itm)
        {
            if (itm == null)
                return "Unknown Item";

            try
            {
                ItemTemplate template = itm.ItemTemplate;
                if (!string.IsNullOrEmpty(template.name))
                    return itm.ItemName;
            }
            catch
            {
            }

            return !string.IsNullOrEmpty(itm.shortName) ? itm.shortName : "Item";
        }

        private void DrawTradeWindow(int windowId)
        {
            var playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
            {
                CancelTrade("Error de entidad de jugador.");
                return;
            }

            int playerGold = playerEntity.GoldPieces;

            GUILayout.Space(4);
            DarkFantasyPixelArtTheme.DrawImGuiDivider();
            GUILayout.Label($"<b>COMERCIO BILATERAL</b> — Compañero: <color=#FFD96B>{activeTradePartnerName}</color>", GUILayout.ExpandWidth(true));
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            // ==========================================
            // LEFT COLUMN: TU OFERTA (345px)
            // ==========================================
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(345));
            GUILayout.Label("TU OFERTA", DarkFantasyPixelArtTheme.SectionBannerStyle);

            // Gold offer
            GUILayout.Label($"Oro ofrecido: <color=#FFD700>{localTradeGold} gp</color> <size=10>(Tienes: {playerGold} gp)</size>");
            if (!localTradeLocked)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+10", GUILayout.Width(38), GUILayout.Height(20))) AddLocalTradeGold(10, playerGold);
                if (GUILayout.Button("+100", GUILayout.Width(44), GUILayout.Height(20))) AddLocalTradeGold(100, playerGold);
                if (GUILayout.Button("+1000", GUILayout.Width(50), GUILayout.Height(20))) AddLocalTradeGold(1000, playerGold);
                if (GUILayout.Button("Max", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Width(40), GUILayout.Height(20))) SetLocalTradeGold(playerGold, playerGold);
                if (GUILayout.Button("0", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(25), GUILayout.Height(20))) SetLocalTradeGold(0, playerGold);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            float totalLocalWeight = 0f;
            foreach (var itm in localTradeItems) if (itm != null) totalLocalWeight += itm.weightInKg;
            GUILayout.Label($"<b>Ítems Ofertados:</b> {localTradeItems.Count} ({totalLocalWeight:F1} kg)");

            localOfferScrollPos = GUILayout.BeginScrollView(localOfferScrollPos, GUILayout.Height(130));
            if (localTradeItems.Count == 0)
            {
                GUILayout.Label("<color=#888888><i>Sin ítems ofrecidos</i></color>");
            }
            else
            {
                for (int i = 0; i < localTradeItems.Count; i++)
                {
                    var itm = localTradeItems[i];
                    if (itm == null) continue;

                    GUILayout.BeginHorizontal();
                    string safeName = GetSafeItemName(itm);
                    string itmTitle = itm.stackCount > 1 ? $"{safeName} x{itm.stackCount}" : safeName;
                    GUILayout.Label($"• {itmTitle} ({itm.weightInKg:F1}kg)", GUILayout.ExpandWidth(true));
                    if (!localTradeLocked && GUILayout.Button("X", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        localTradeItems.RemoveAt(i);
                        localTradeLocked = false;
                        localTradeAccepted = false;
                        SyncLocalTradeStateToPartner();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            if (!localTradeLocked)
            {
                if (GUILayout.Button(showInventoryPicker ? "▲ Ocultar Inventario" : "▼ + Agregar Ítems del Inventario", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(24)))
                {
                    showInventoryPicker = !showInventoryPicker;
                }

                if (showInventoryPicker)
                {
                    inventoryPickerScrollPos = GUILayout.BeginScrollView(inventoryPickerScrollPos, GUILayout.Height(100));
                    var inv = playerEntity.Items;
                    int addedAny = 0;
                    if (inv != null)
                    {
                        for (int i = 0; i < inv.Count; i++)
                        {
                            DaggerfallUnityItem itm = inv.GetItem(i);
                            if (itm != null && !itm.IsEquipped && !itm.IsQuestItem && itm.ItemGroup != ItemGroups.Currency && itm.ItemGroup != ItemGroups.None && !localTradeItems.Contains(itm))
                            {
                                addedAny++;
                                GUILayout.BeginHorizontal();
                                string safeName = GetSafeItemName(itm);
                                string itmTitle = itm.stackCount > 1 ? $"{safeName} x{itm.stackCount}" : safeName;
                                if (GUILayout.Button($"+ {itmTitle} ({itm.weightInKg:F1}kg)", GUILayout.Height(20)))
                                {
                                    localTradeItems.Add(itm);
                                    localTradeLocked = false;
                                    localTradeAccepted = false;
                                    SyncLocalTradeStateToPartner();
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    if (addedAny == 0)
                    {
                        GUILayout.Label("<color=#AAAAAA><i>No tienes más ítems transferibles.</i></color>");
                    }
                    GUILayout.EndScrollView();
                }
            }

            GUILayout.Space(6);
            string myLockStr = localTradeLocked 
                ? "<color=#88FF88><b>[x] OFERTA BLOQUEADA</b></color>" 
                : "<color=#FFDD44><b>[ ] MODIFICANDO OFERTA...</b></color>";
            GUILayout.Label(myLockStr);
            if (localTradeAccepted)
            {
                GUILayout.Label("<color=#44FF44><b>[v] HAS ACEPTADO EL INTERCAMBIO</b></color>");
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // ==========================================
            // RIGHT COLUMN: OFERTA DEL COMPAÑERO (345px)
            // ==========================================
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(345));
            GUILayout.Label($"OFERTA DE {activeTradePartnerName.ToUpper()}", DarkFantasyPixelArtTheme.SectionBannerStyle);

            // Remote gold
            GUILayout.Label($"Oro a recibir: <color=#FFD700>{remoteTradeGold} gp</color>");
            GUILayout.Space(26);

            float totalRemoteWeight = 0f;
            foreach (var itm in remoteTradeItems) if (itm != null) totalRemoteWeight += itm.weight;
            GUILayout.Label($"<b>Ítems Ofertados:</b> {remoteTradeItems.Count} ({totalRemoteWeight:F1} kg)");

            remoteOfferScrollPos = GUILayout.BeginScrollView(remoteOfferScrollPos, GUILayout.Height(130));
            if (remoteTradeItems.Count == 0)
            {
                GUILayout.Label("<color=#888888><i>El compañero no ha ofrecido ítems</i></color>");
            }
            else
            {
                foreach (var itm in remoteTradeItems)
                {
                    if (itm == null) continue;
                    string itmTitle = itm.stackCount > 1 ? $"{itm.name} x{itm.stackCount}" : itm.name;
                    GUILayout.Label($"• {itmTitle} ({itm.weight:F1}kg)");
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            GUILayout.Space(6);
            string theirLockStr = remoteTradeLocked 
                ? "<color=#88FF88><b>[x] OFERTA BLOQUEADA</b></color>" 
                : "<color=#FF8888><b>[ ] MODIFICANDO OFERTA...</b></color>";
            GUILayout.Label(theirLockStr);
            if (remoteTradeAccepted)
            {
                GUILayout.Label("<color=#44FF44><b>[v] COMPAÑERO LISTO PARA INTERCAMBIAR</b></color>");
            }
            else
            {
                GUILayout.Label("<color=#AAAAAA><i>Esperando confirmación...</i></color>");
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            DarkFantasyPixelArtTheme.DrawImGuiDivider();

            // ==========================================
            // BOTTOM ACTION BAR
            // ==========================================
            GUILayout.BeginHorizontal();

            if (!localTradeLocked)
            {
                if (GUILayout.Button("Bloquear Oferta", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(32)))
                {
                    localTradeLocked = true;
                    localTradeAccepted = false;
                    SyncLocalTradeStateToPartner();
                }
            }
            else if (!localTradeAccepted)
            {
                if (GUILayout.Button("Desbloquear Oferta", GUILayout.Height(32)))
                {
                    localTradeLocked = false;
                    localTradeAccepted = false;
                    SyncLocalTradeStateToPartner();
                }
            }

            GUI.enabled = (localTradeLocked && remoteTradeLocked && !localTradeAccepted);
            string acceptBtnText = localTradeAccepted ? "Esperando al Compañero..." : "Aceptar Intercambio";
            if (GUILayout.Button(acceptBtnText, DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(32)))
            {
                AcceptLocalTrade();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Cancelar Comercio", DarkFantasyPixelArtTheme.CrimsonButtonStyle, GUILayout.Height(32), GUILayout.Width(140)))
            {
                CancelTrade("Comercio cancelado por el jugador.");
            }

            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void AddLocalTradeGold(int amount, int maxGold)
        {
            localTradeGold = Mathf.Clamp(localTradeGold + amount, 0, maxGold);
            localTradeLocked = false;
            localTradeAccepted = false;
            SyncLocalTradeStateToPartner();
        }

        private void SetLocalTradeGold(int amount, int maxGold)
        {
            localTradeGold = Mathf.Clamp(amount, 0, maxGold);
            localTradeLocked = false;
            localTradeAccepted = false;
            SyncLocalTradeStateToPartner();
        }

        private void ResetTradeOfferState()
        {
            localTradeGold = 0;
            localTradeItems.Clear();
            localTradeLocked = false;
            localTradeAccepted = false;
            showInventoryPicker = false;

            remoteTradeGold = 0;
            remoteTradeItems.Clear();
            remoteTradeLocked = false;
            remoteTradeAccepted = false;

            activeTradePartnerConnId = -1;
            activeTradePartnerName = "";
            activeTradePartnerPuppet = null;
        }

        public void InitiateTradeInvite(NativePlayerPuppet targetPuppet)
        {
            if (targetPuppet == null)
                return;

            showPlayerInteractionWindow = false;

            activeTradePartnerConnId = targetPuppet.connectionId;
            activeTradePartnerName = targetPuppet.playerName;
            activeTradePartnerPuppet = targetPuppet;

            string myName = (GameManager.Instance.PlayerEntity != null) ? GameManager.Instance.PlayerEntity.Name : "Jugador";

            MsgTradeInvite msg = new MsgTradeInvite
            {
                senderConnectionId = localConnectionId,
                senderName = myName,
                targetConnectionId = targetPuppet.connectionId
            };

            if (NetworkSession.IsHost)
            {
                if (connectedClients != null)
                {
                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && conn.connectionId == targetPuppet.connectionId)
                        {
                            conn.Send(msg);
                            break;
                        }
                    }
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }

            DaggerfallUI.AddHUDText($"[Comercio] Solicitud enviada a {targetPuppet.playerName}. Esperando respuesta...");
        }

        public void RespondToTradeInvite(int senderConnId, bool accepted)
        {
            string myName = (GameManager.Instance.PlayerEntity != null) ? GameManager.Instance.PlayerEntity.Name : "Jugador";

            MsgTradeInviteResponse resp = new MsgTradeInviteResponse
            {
                senderConnectionId = localConnectionId,
                senderName = myName,
                targetConnectionId = senderConnId,
                accepted = accepted
            };

            if (NetworkSession.IsHost)
            {
                if (connectedClients != null)
                {
                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && conn.connectionId == senderConnId)
                        {
                            conn.Send(resp);
                            break;
                        }
                    }
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(resp);
            }

            if (accepted)
            {
                ResetTradeOfferState();
                activeTradePartnerConnId = senderConnId;
                activeTradePartnerName = pendingTradeInviteSenderName;
                remotePlayerPuppets.TryGetValue(senderConnId, out activeTradePartnerPuppet);

                showTradeWindow = true;
                tradeWindowRect.width = 720;
                tradeWindowRect.height = 560;
                tradeWindowRect.x = (Screen.width - tradeWindowRect.width) * 0.5f;
                tradeWindowRect.y = (Screen.height - tradeWindowRect.height) * 0.5f;

                DaggerfallUI.AddHUDText($"[Comercio] Iniciando comercio con {activeTradePartnerName}.");
            }
            else
            {
                DaggerfallUI.AddHUDText("[Comercio] Solicitud rechazada.");
            }
        }

        public void SyncLocalTradeStateToPartner()
        {
            TradeOfferBundle bundle = new TradeOfferBundle();
            foreach (var itm in localTradeItems)
            {
                if (itm != null)
                {
                    bundle.items.Add(new TradeItemDTO
                    {
                        uid = itm.UID,
                        name = GetSafeItemName(itm),
                        weight = itm.weightInKg,
                        stackCount = itm.stackCount,
                        value = itm.value
                    });
                }
            }

            string json = JsonUtility.ToJson(bundle);

            MsgTradeStateSync msg = new MsgTradeStateSync
            {
                senderConnectionId = localConnectionId,
                targetConnectionId = activeTradePartnerConnId,
                gold = localTradeGold,
                offerItemsJson = json,
                isLocked = localTradeLocked,
                isAccepted = localTradeAccepted
            };

            if (NetworkSession.IsHost)
            {
                if (activeTradePartnerConnId != 0 && connectedClients != null)
                {
                    foreach (NetworkConnectionToClient conn in connectedClients)
                    {
                        if (conn != null && conn.connectionId == activeTradePartnerConnId)
                        {
                            conn.Send(msg);
                            break;
                        }
                    }
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }
        }

        public void AcceptLocalTrade()
        {
            localTradeAccepted = true;
            SyncLocalTradeStateToPartner();

            ItemData_v1[] itemArray = new ItemData_v1[localTradeItems.Count];
            for (int i = 0; i < localTradeItems.Count; i++)
            {
                itemArray[i] = localTradeItems[i].GetSaveData();
            }
            string itemsJson = SaveLoadManager.Serialize(typeof(ItemData_v1[]), itemArray, false);

            MsgTradeCommit commit = new MsgTradeCommit
            {
                senderConnectionId = localConnectionId,
                targetConnectionId = activeTradePartnerConnId,
                offeredGold = localTradeGold,
                offeredItemsJson = itemsJson
            };

            if (NetworkSession.IsHost)
            {
                OnServerReceiveTradeCommit(null, commit);
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(commit);
            }

            DaggerfallUI.AddHUDText("[Comercio] Has aceptado el intercambio. Esperando confirmación del servidor...");
        }

        public void CancelTrade(string reason)
        {
            MsgTradeCancel msg = new MsgTradeCancel
            {
                senderConnectionId = localConnectionId,
                targetConnectionId = activeTradePartnerConnId,
                reason = reason
            };

            if (NetworkSession.IsHost)
            {
                OnServerReceiveTradeCancel(null, msg);
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }

            showTradeWindow = false;
            showTradeInvitePrompt = false;
            ResetTradeOfferState();
            DaggerfallUI.AddHUDText($"[Comercio] {reason}");
        }

        private void OnServerReceiveTradeInvite(NetworkConnection conn, MsgTradeInvite msg)
        {
            if (!NetworkSession.IsHost) return;

            int senderId = (conn != null) ? conn.connectionId : msg.senderConnectionId;
            msg.senderConnectionId = senderId;

            if (msg.targetConnectionId == 0)
            {
                OnClientReceiveTradeInvite(msg);
            }
            else
            {
                SendToConnection(msg.targetConnectionId, msg);
            }
        }

        private void OnServerReceiveTradeInviteResponse(NetworkConnection conn, MsgTradeInviteResponse msg)
        {
            if (!NetworkSession.IsHost) return;

            int senderId = (conn != null) ? conn.connectionId : msg.senderConnectionId;
            msg.senderConnectionId = senderId;

            if (msg.targetConnectionId == 0)
            {
                OnClientReceiveTradeInviteResponse(msg);
            }
            else
            {
                SendToConnection(msg.targetConnectionId, msg);
            }
        }

        private void OnServerReceiveTradeStateSync(NetworkConnection conn, MsgTradeStateSync msg)
        {
            if (!NetworkSession.IsHost) return;

            int senderId = (conn != null) ? conn.connectionId : msg.senderConnectionId;
            msg.senderConnectionId = senderId;

            if (msg.targetConnectionId == 0)
            {
                OnClientReceiveTradeStateSync(msg);
            }
            else
            {
                SendToConnection(msg.targetConnectionId, msg);
            }
        }

        private void OnServerReceiveTradeCancel(NetworkConnection conn, MsgTradeCancel msg)
        {
            if (!NetworkSession.IsHost) return;

            int senderId = (conn != null) ? conn.connectionId : msg.senderConnectionId;
            msg.senderConnectionId = senderId;

            serverPendingTradeCommits.Remove(senderId);
            serverPendingTradeCommits.Remove(msg.targetConnectionId);

            if (msg.targetConnectionId == 0)
            {
                OnClientReceiveTradeCancel(msg);
            }
            else
            {
                SendToConnection(msg.targetConnectionId, msg);
            }
        }

        private void OnServerReceiveTradeCommit(NetworkConnection conn, MsgTradeCommit msg)
        {
            if (!NetworkSession.IsHost)
                return;

            int senderId = (conn != null) ? conn.connectionId : msg.senderConnectionId;
            msg.senderConnectionId = senderId;
            serverPendingTradeCommits[senderId] = msg;

            LogNetwork($"[DFU Trade] Server received trade commit from conn {senderId} (target conn {msg.targetConnectionId}, gold {msg.offeredGold}).");

            if (serverPendingTradeCommits.TryGetValue(msg.targetConnectionId, out MsgTradeCommit partnerCommit))
            {
                if (partnerCommit.targetConnectionId == senderId)
                {
                    ServerExecuteTrade(msg, partnerCommit);
                    serverPendingTradeCommits.Remove(senderId);
                    serverPendingTradeCommits.Remove(msg.targetConnectionId);
                }
            }
        }

        private void ServerExecuteTrade(MsgTradeCommit commitA, MsgTradeCommit commitB)
        {
            LogNetwork($"[DFU Trade] Executing authoritative trade between conn {commitA.senderConnectionId} and conn {commitB.senderConnectionId}.");

            if (commitA.senderConnectionId == 0)
            {
                ApplyTradeToLocalPlayer(commitA.offeredGold, commitA.offeredItemsJson, commitB.offeredGold, commitB.offeredItemsJson);
            }
            else
            {
                MsgTradeCompleted compA = new MsgTradeCompleted
                {
                    success = true,
                    message = "¡Intercambio completado con éxito!",
                    receivedGold = commitB.offeredGold,
                    receivedItemsJson = commitB.offeredItemsJson
                };
                SendToConnection(commitA.senderConnectionId, compA);
            }

            if (commitB.senderConnectionId == 0)
            {
                ApplyTradeToLocalPlayer(commitB.offeredGold, commitB.offeredItemsJson, commitA.offeredGold, commitA.offeredItemsJson);
            }
            else
            {
                MsgTradeCompleted compB = new MsgTradeCompleted
                {
                    success = true,
                    message = "¡Intercambio completado con éxito!",
                    receivedGold = commitA.offeredGold,
                    receivedItemsJson = commitA.offeredItemsJson
                };
                SendToConnection(commitB.senderConnectionId, compB);
            }
        }

        private void ApplyTradeToLocalPlayer(int goldGiven, string itemsGivenJson, int goldReceived, string itemsReceivedJson)
        {
            var playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
                return;

            playerEntity.GoldPieces = Mathf.Max(0, playerEntity.GoldPieces - goldGiven);

            if (!string.IsNullOrEmpty(itemsGivenJson))
            {
                ItemData_v1[] givenArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), itemsGivenJson);
                if (givenArray != null)
                {
                    foreach (var itmData in givenArray)
                    {
                        var itm = playerEntity.Items.GetItem(itmData.uid);
                        if (itm != null)
                        {
                            playerEntity.Items.RemoveItem(itm);
                        }
                    }
                }
            }

            playerEntity.GoldPieces += goldReceived;

            if (!string.IsNullOrEmpty(itemsReceivedJson))
            {
                ItemData_v1[] receivedArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), itemsReceivedJson);
                if (receivedArray != null)
                {
                    foreach (var itmData in receivedArray)
                    {
                        DaggerfallUnityItem newItem = new DaggerfallUnityItem(itmData);
                        if (playerEntity.Items.Contains(newItem.UID))
                        {
                            newItem = newItem.Clone();
                        }
                        playerEntity.Items.AddItem(newItem);
                    }
                }
            }

            DaggerfallUI.AddHUDText("[Comercio] ¡Intercambio completado con éxito!");
            showTradeWindow = false;
            ResetTradeOfferState();
        }

        private void SendToConnection<T>(int connectionId, T msg) where T : struct, NetworkMessage
        {
            if (connectedClients != null)
            {
                foreach (NetworkConnectionToClient conn in connectedClients)
                {
                    if (conn != null && conn.connectionId == connectionId)
                    {
                        conn.Send(msg);
                        break;
                    }
                }
            }
        }

        private void OnClientReceiveTradeInvite(MsgTradeInvite msg)
        {
            pendingTradeInviteSenderConnId = msg.senderConnectionId;
            pendingTradeInviteSenderName = msg.senderName;
            tradeInviteTimeout = 25f;
            showTradeInvitePrompt = true;
            tradeInviteRect.width = 420;
            tradeInviteRect.height = 185;
            tradeInviteRect.x = (Screen.width - tradeInviteRect.width) * 0.5f;
            tradeInviteRect.y = 120;
            DaggerfallUI.AddHUDText($"[Comercio] Solicitud de intercambio recibida de {msg.senderName}.");
        }

        private void OnClientReceiveTradeInviteResponse(MsgTradeInviteResponse msg)
        {
            if (msg.accepted)
            {
                ResetTradeOfferState();
                activeTradePartnerConnId = msg.senderConnectionId;
                activeTradePartnerName = msg.senderName;
                remotePlayerPuppets.TryGetValue(msg.senderConnectionId, out activeTradePartnerPuppet);

                showTradeWindow = true;
                tradeWindowRect.width = 720;
                tradeWindowRect.height = 560;
                tradeWindowRect.x = (Screen.width - tradeWindowRect.width) * 0.5f;
                tradeWindowRect.y = (Screen.height - tradeWindowRect.height) * 0.5f;

                DaggerfallUI.AddHUDText($"[Comercio] ¡{msg.senderName} aceptó la solicitud de comercio!");
            }
            else
            {
                showTradeWindow = false;
                ResetTradeOfferState();
                DaggerfallUI.AddHUDText($"[Comercio] {msg.senderName} rechazó la solicitud de intercambio.");
            }
        }

        private void OnClientReceiveTradeStateSync(MsgTradeStateSync msg)
        {
            if (!showTradeWindow || msg.senderConnectionId != activeTradePartnerConnId)
                return;

            remoteTradeGold = msg.gold;
            remoteTradeLocked = msg.isLocked;
            remoteTradeAccepted = msg.isAccepted;

            remoteTradeItems.Clear();
            if (!string.IsNullOrEmpty(msg.offerItemsJson))
            {
                try
                {
                    TradeOfferBundle bundle = JsonUtility.FromJson<TradeOfferBundle>(msg.offerItemsJson);
                    if (bundle != null && bundle.items != null)
                    {
                        remoteTradeItems.AddRange(bundle.items);
                    }
                }
                catch (Exception ex)
                {
                    LogNetwork($"[DFU Trade] Error parsing remote trade items: {ex.Message}");
                }
            }

            if (!remoteTradeLocked)
            {
                localTradeAccepted = false;
            }
        }

        private void OnClientReceiveTradeCancel(MsgTradeCancel msg)
        {
            showTradeWindow = false;
            showTradeInvitePrompt = false;
            ResetTradeOfferState();
            DaggerfallUI.AddHUDText($"[Comercio] El compañero canceló el intercambio: {msg.reason}");
        }

        private void OnClientReceiveTradeCompleted(MsgTradeCompleted msg)
        {
            if (NetworkSession.IsHost && localConnectionId == 0)
                return;

            if (msg.success)
            {
                var playerEntity = GameManager.Instance.PlayerEntity;
                if (playerEntity != null)
                {
                    playerEntity.GoldPieces = Mathf.Max(0, playerEntity.GoldPieces - localTradeGold);

                    foreach (var itm in localTradeItems)
                    {
                        if (itm != null)
                            playerEntity.Items.RemoveItem(itm);
                    }

                    playerEntity.GoldPieces += msg.receivedGold;

                    if (!string.IsNullOrEmpty(msg.receivedItemsJson))
                    {
                        ItemData_v1[] receivedArray = (ItemData_v1[])SaveLoadManager.Deserialize(typeof(ItemData_v1[]), msg.receivedItemsJson);
                        if (receivedArray != null)
                        {
                            foreach (var itmData in receivedArray)
                            {
                                DaggerfallUnityItem newItem = new DaggerfallUnityItem(itmData);
                                if (playerEntity.Items.Contains(newItem.UID))
                                {
                                    newItem = newItem.Clone();
                                }
                                playerEntity.Items.AddItem(newItem);
                            }
                        }
                    }
                }

                DaggerfallUI.AddHUDText("[Comercio] ¡Intercambio completado con éxito!");
            }
            else
            {
                DaggerfallUI.AddHUDText($"[Comercio] Falló el intercambio: {msg.message}");
            }

            showTradeWindow = false;
            ResetTradeOfferState();
        }

        #endregion

        private void OnClientReceivePartyRestVoteUpdate(MsgPartyRestVoteUpdate msg)
        {
            if (NetworkSession.IsHost)
                return;

            OpenOrUpdatePartyVoteWindow(msg);
        }

        private void OnClientReceivePartyRestVotePrompt(MsgPartyRestVotePrompt msg)
        {
            // Backward compatibility
        }

        private void OnClientReceivePartyRestVoteResult(MsgPartyRestVoteResult msg)
        {
            if (NetworkSession.IsHost)
                return;

            ClosePartyVoteWindow();
            DaggerfallUI.AddHUDText($"[Party] {msg.message}");
        }

        private void StartHostAuthoritativeRest(int hours, bool isLoiter)
        {
            BroadcastPartyRest(true, hours, false, isLoiter: isLoiter);
            // Execute the rest by opening RestWindow and automatically executing the hours, or running rest progression
            IUserInterfaceManager uiMgr = DaggerfallUI.Instance.UserInterfaceManager;
            DaggerfallRestWindow restWin = new DaggerfallRestWindow(uiMgr);
            uiMgr.PushWindow(restWin);
            if (isLoiter)
                restWin.StartAuthoritativeLoiter(hours);
            else
                restWin.StartAuthoritativeRest(hours);
        }

        public void BroadcastPartyRest(bool isResting, int hours, bool wasInterrupted = false, bool isLoiter = false)
        {
            if (!NetworkServer.active)
                return;
            if (!isResting && !isPartyResting)
                return;
            isPartyResting = isResting;
            NetworkServer.SendToAll(new MsgPartyRestNotification {
                isResting = isResting,
                hours = hours,
                wasInterrupted = wasInterrupted,
                isLoiter = isLoiter
            });
        }

        private void OnClientReceivePartyRestNotification(MsgPartyRestNotification msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (msg.isResting)
            {
                isPartyRestingBlackout = true;
                partyRestHours = msg.hours;
                partyRestIsLoiter = msg.isLoiter;
                string action = msg.isLoiter ? "Loitering" : "Resting";
                DaggerfallUI.AddHUDText($"[Party] {action} with party ({msg.hours} hours)...");
            }
            else
            {
                isPartyRestingBlackout = false;
                if (!partyRestIsLoiter && !msg.isLoiter)
                    RecoverClientVitals(partyRestHours);

                // Show identical Daggerfall parchment message box to client
                if (msg.wasInterrupted)
                {
                    const int enemiesNearby = 354;
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(enemiesNearby);
                    mb.OnClose += () =>
                    {
                        DaggerfallUI.Instance.PopToHUD();
                        GameManager.Instance.PlayerEntity?.RaiseSkills();
                    };
                }
                else if (partyRestIsLoiter || msg.isLoiter)
                {
                    const int finishedLoiteringTextId = 349;
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(finishedLoiteringTextId);
                    mb.OnClose += () =>
                    {
                        DaggerfallUI.Instance.PopToHUD();
                        GameManager.Instance.PlayerEntity?.RaiseSkills();
                    };
                }
                else
                {
                    const int youWakeUpTextId = 353;
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(youWakeUpTextId);
                    mb.OnClose += () =>
                    {
                        DaggerfallUI.Instance.PopToHUD();
                        GameManager.Instance.PlayerEntity?.RaiseSkills();
                    };
                }
                partyRestHours = 0;
                partyRestIsLoiter = false;
            }
        }

        private void RecoverClientVitals(int hours)
        {
            if (hours <= 0 || GameManager.Instance == null || GameManager.Instance.PlayerEntity == null)
                return;

            PlayerEntity player = GameManager.Instance.PlayerEntity;
            for (int i = 0; i < hours; i++)
            {
                int healthRate = FormulaHelper.CalculateHealthRecoveryRate(player);
                int fatigueRate = FormulaHelper.CalculateFatigueRecoveryRate(player.MaxFatigue);
                int magickaRate = FormulaHelper.CalculateSpellPointRecoveryRate(player);
                player.CurrentHealth += healthRate;
                player.CurrentFatigue += fatigueRate;
                player.CurrentMagicka += magickaRate;
                player.TallySkill((short)DFCareer.Skills.Medical, 1);
            }
            if (player.CurrentHealth > player.MaxHealth) player.CurrentHealth = player.MaxHealth;
            if (player.CurrentFatigue > player.MaxFatigue) player.CurrentFatigue = player.MaxFatigue;
            if (player.CurrentMagicka > player.MaxMagicka) player.CurrentMagicka = player.MaxMagicka;
        }

        #endregion

        #region Overhead Dialogue Speech Bubble System

        public class DialogueBubble
        {
            public string key;                   // Unique key e.g. "Player_{connId}" or "NPC_{npcName}"
            public int speakerType;              // 0 = Player, 1 = NPC
            public int playerConnectionId;
            public string speakerName;
            public Vector3 worldPosition;
            public string text;
            public float activeTimer;
            public bool isEnding;
            public float lingerRemaining;
            public float fadeRemaining;

            public const float DefaultLinger = 3.5f;
            public const float FadeDuration = 1.5f;
            public const float MaxActiveWithoutInput = 14.0f;
        }

        private List<DialogueBubble> activeDialogueBubbles = new List<DialogueBubble>();
        private GUIStyle bubbleHeaderStyle;
        private GUIStyle bubbleTextStyle;
        private GUIStyle bubbleBoxStyle;

        /// <summary>
        /// Broadcasts an overhead dialogue speech bubble across the network.
        /// speakerType: 0 = Player, 1 = NPC
        /// </summary>
        public void BroadcastDialogueSpeech(int speakerType, string speakerName, Vector3 worldPosition, string text, float lingerSeconds = DialogueBubble.DefaultLinger)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int localConnId = NetworkSession.IsHost ? 0 : localConnectionId;

            MsgDialogueSpeech msg = new MsgDialogueSpeech
            {
                speakerType = speakerType,
                playerConnectionId = localConnId,
                speakerName = speakerName,
                worldPosition = worldPosition,
                text = text,
                lingerSeconds = lingerSeconds
            };

            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(msg);
                RegisterOrUpdateDialogueBubble(msg);
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(msg);
                RegisterOrUpdateDialogueBubble(msg);
            }
        }

        /// <summary>
        /// Broadcasts that a dialogue session ended, triggering linger and smooth fadeout.
        /// </summary>
        public void BroadcastDialogueEnded(string npcName)
        {
            int localConnId = NetworkSession.IsHost ? 0 : localConnectionId;

            MsgDialogueEnded msg = new MsgDialogueEnded
            {
                playerConnectionId = localConnId,
                npcName = npcName
            };

            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(msg);
                EndDialogueBubblesForPlayer(localConnId, npcName);
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(msg);
                EndDialogueBubblesForPlayer(localConnId, npcName);
            }
        }

        private void OnServerReceiveDialogueSpeech(NetworkConnection conn, MsgDialogueSpeech msg)
        {
            msg.playerConnectionId = conn.connectionId;
            if (msg.speakerType == 0 && connectedPlayerProfiles.TryGetValue(conn.connectionId, out MsgPlayerProfile prof))
            {
                msg.speakerName = prof.playerName;
            }
            NetworkServer.SendToAll(msg);
            RegisterOrUpdateDialogueBubble(msg);
        }

        private void OnServerReceiveDialogueEnded(NetworkConnection conn, MsgDialogueEnded msg)
        {
            msg.playerConnectionId = conn.connectionId;
            NetworkServer.SendToAll(msg);
            EndDialogueBubblesForPlayer(conn.connectionId, msg.npcName);
        }

        private void OnClientReceiveDialogueSpeech(MsgDialogueSpeech msg)
        {
            if (!NetworkServer.active)
                RegisterOrUpdateDialogueBubble(msg);
        }

        private void OnClientReceiveDialogueEnded(MsgDialogueEnded msg)
        {
            if (!NetworkServer.active)
                EndDialogueBubblesForPlayer(msg.playerConnectionId, msg.npcName);
        }

        private void RegisterOrUpdateDialogueBubble(MsgDialogueSpeech msg)
        {
            if (string.IsNullOrEmpty(msg.text))
                return;

            string key = msg.speakerType == 0
                ? $"Player_{msg.playerConnectionId}"
                : $"NPC_{msg.speakerName}";

            DialogueBubble bubble = activeDialogueBubbles.Find(b => b.key == key);
            if (bubble == null)
            {
                bubble = new DialogueBubble
                {
                    key = key,
                    speakerType = msg.speakerType,
                    playerConnectionId = msg.playerConnectionId,
                    speakerName = msg.speakerName,
                    worldPosition = msg.worldPosition,
                    text = msg.text,
                    activeTimer = 0f,
                    isEnding = false,
                    lingerRemaining = msg.lingerSeconds > 0 ? msg.lingerSeconds : DialogueBubble.DefaultLinger,
                    fadeRemaining = DialogueBubble.FadeDuration
                };
                activeDialogueBubbles.Add(bubble);
            }
            else
            {
                // Sequential replacement: old phrase immediately replaced
                bubble.speakerName = msg.speakerName;
                bubble.worldPosition = msg.worldPosition;
                bubble.text = msg.text;
                bubble.activeTimer = 0f;
                bubble.isEnding = false;
                bubble.lingerRemaining = msg.lingerSeconds > 0 ? msg.lingerSeconds : DialogueBubble.DefaultLinger;
                bubble.fadeRemaining = DialogueBubble.FadeDuration;
            }
        }

        private void EndDialogueBubblesForPlayer(int connId, string npcName)
        {
            string playerKey = $"Player_{connId}";
            string npcKey = $"NPC_{npcName}";

            for (int i = 0; i < activeDialogueBubbles.Count; i++)
            {
                DialogueBubble b = activeDialogueBubbles[i];
                if (b.key == playerKey || (!string.IsNullOrEmpty(npcName) && b.key == npcKey))
                {
                    b.isEnding = true;
                    b.lingerRemaining = DialogueBubble.DefaultLinger;
                    b.fadeRemaining = DialogueBubble.FadeDuration;
                }
            }
        }

        private void UpdateDialogueBubbles(float deltaTime)
        {
            if (activeDialogueBubbles.Count == 0)
                return;

            for (int i = activeDialogueBubbles.Count - 1; i >= 0; i--)
            {
                DialogueBubble b = activeDialogueBubbles[i];
                b.activeTimer += deltaTime;

                if (!b.isEnding && b.activeTimer >= DialogueBubble.MaxActiveWithoutInput)
                {
                    b.isEnding = true;
                    b.lingerRemaining = DialogueBubble.DefaultLinger;
                    b.fadeRemaining = DialogueBubble.FadeDuration;
                }

                if (b.isEnding)
                {
                    if (b.lingerRemaining > 0f)
                    {
                        b.lingerRemaining -= deltaTime;
                    }
                    else
                    {
                        b.fadeRemaining -= deltaTime;
                        if (b.fadeRemaining <= 0f)
                        {
                            activeDialogueBubbles.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void DrawDialogueBubbles()
        {
            if (activeDialogueBubbles.Count == 0)
                return;

            Camera cam = GameManager.Instance != null ? GameManager.Instance.MainCamera : Camera.main;
            if (cam == null)
                return;

            if (bubbleHeaderStyle == null)
            {
                bubbleHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (bubbleTextStyle == null)
            {
                bubbleTextStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 12,
                    alignment = TextAnchor.UpperCenter
                };
            }

            if (bubbleBoxStyle == null)
            {
                bubbleBoxStyle = new GUIStyle(GUI.skin.box);
            }

            int localConnId = NetworkSession.IsHost ? 0 : localConnectionId;

            Color prevGuiColor = GUI.color;
            Color prevContentColor = GUI.contentColor;

            for (int i = 0; i < activeDialogueBubbles.Count; i++)
            {
                DialogueBubble b = activeDialogueBubbles[i];

                Vector3 headWorldPos = b.worldPosition + Vector3.up * 2.2f;

                // Dynamically track living puppet transform if available
                if (b.speakerType == 0)
                {
                    if (b.playerConnectionId == localConnId && GameManager.Instance.PlayerObject != null)
                    {
                        headWorldPos = GameManager.Instance.PlayerObject.transform.position + Vector3.up * 2.45f;
                    }
                    else if (remotePlayerPuppets.TryGetValue(b.playerConnectionId, out NativePlayerPuppet puppet) && puppet != null && puppet.gameObject != null)
                    {
                        headWorldPos = puppet.transform.position + Vector3.up * 2.45f;
                    }
                }
                else
                {
                    // For NPC: if TalkManager is currently referencing this NPC locally, track live transform
                    if (TalkManager.Instance != null && TalkManager.Instance.NameNPC == b.speakerName)
                    {
                        if (TalkManager.Instance.CurrentNPCType == TalkManager.NPCType.Mobile && TalkManager.Instance.MobileNPC != null)
                            headWorldPos = TalkManager.Instance.MobileNPC.transform.position + Vector3.up * 2.2f;
                        else if (TalkManager.Instance.CurrentNPCType == TalkManager.NPCType.Static && TalkManager.Instance.StaticNPC != null)
                            headWorldPos = TalkManager.Instance.StaticNPC.transform.position + Vector3.up * 2.2f;
                    }
                }

                // Distance calculation
                float distance = Vector3.Distance(cam.transform.position, headWorldPos);
                if (distance > 16.0f)
                    continue; // Inaudible / beyond hearing range

                Vector3 screenPos = cam.WorldToScreenPoint(headWorldPos);
                if (screenPos.z <= 0.1f)
                    continue; // Behind camera

                // If local player in first person, don't obstruct their own eyes
                if (b.speakerType == 0 && b.playerConnectionId == localConnId && distance < 1.0f)
                    continue;

                // Acoustic falloff: readability decays with distance
                // <= 3.5m: 1.0 (crisp, easy to read)
                // 3.5m - 8m: 1.0 -> 0.70
                // 8m - 14m: 0.70 -> 0.18 (faint whisper, hard to read)
                // 14m - 16m: 0.18 -> 0.04 (barely discernible)
                float distanceAlpha;
                if (distance <= 3.5f)
                    distanceAlpha = 1.0f;
                else if (distance <= 8.0f)
                    distanceAlpha = Mathf.Lerp(1.0f, 0.70f, (distance - 3.5f) / 4.5f);
                else if (distance <= 14.0f)
                    distanceAlpha = Mathf.Lerp(0.70f, 0.18f, (distance - 8.0f) / 6.0f);
                else
                    distanceAlpha = Mathf.Lerp(0.18f, 0.04f, (distance - 14.0f) / 2.0f);

                // Smooth fadeout when conversation ended
                float fadeAlpha = 1.0f;
                if (b.isEnding && b.lingerRemaining <= 0f)
                {
                    fadeAlpha = Mathf.Clamp01(b.fadeRemaining / DialogueBubble.FadeDuration);
                }

                float finalAlpha = distanceAlpha * fadeAlpha;
                if (finalAlpha <= 0.01f)
                    continue;

                // Dynamic speech bubble dimensions
                float bubbleWidth = Mathf.Clamp(280f, 200f, Screen.width * 0.45f);
                GUIContent content = new GUIContent(b.text);
                float textHeight = bubbleTextStyle.CalcHeight(content, bubbleWidth - 16f);
                float totalHeight = textHeight + 28f;

                float rx = screenPos.x - (bubbleWidth * 0.5f);
                float ry = Screen.height - screenPos.y - totalHeight - 10f;
                Rect bubbleRect = new Rect(rx, ry, bubbleWidth, totalHeight);

                // Bubble box styling
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.94f * finalAlpha);
                GUI.Box(bubbleRect, GUIContent.none, DarkFantasyPixelArtTheme.SpeechBubbleBoxStyle);

                // Speaker Header styling
                if (b.speakerType == 0)
                    bubbleHeaderStyle.normal.textColor = new Color(1.0f, 0.85f, 0.42f, finalAlpha); // Gold for players
                else
                    bubbleHeaderStyle.normal.textColor = new Color(0.55f, 0.90f, 0.85f, finalAlpha); // Arcane teal for NPCs

                Rect headerRect = new Rect(bubbleRect.x + 8f, bubbleRect.y + 4f, bubbleWidth - 16f, 18f);
                GUI.color = Color.white;
                GUI.Label(headerRect, $"[{b.speakerName}]", bubbleHeaderStyle);

                // Spoken text styling
                bubbleTextStyle.normal.textColor = new Color(0.95f, 0.90f, 0.78f, finalAlpha);
                Rect textRect = new Rect(bubbleRect.x + 8f, bubbleRect.y + 22f, bubbleWidth - 16f, textHeight + 4f);
                GUI.Label(textRect, b.text, bubbleTextStyle);
            }

            GUI.color = prevGuiColor;
            GUI.contentColor = prevContentColor;
        }

        #endregion

        #region Town Population Synchronization

        /// <summary>
        /// Synchronizes wandering civilian town NPCs (PopulationManager) from Server to Clients.
        /// </summary>
        private void SyncTownPopulation(float dt)
        {
            populationSyncTimer += dt;
            if (populationSyncTimer < PopulationSyncInterval)
                return;

            populationSyncTimer = 0f;

            if (GameManager.Instance == null || GameManager.Instance.StreamingWorld == null)
                return;

            DaggerfallLocation dfLoc = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
            if (dfLoc == null)
                return;

            PopulationManager popMgr = dfLoc.GetComponent<PopulationManager>();
            if (popMgr == null || popMgr.PopulationPool == null)
                return;

            int poolCount = popMgr.PopulationPool.Count;
            if (poolCount == 0)
                return;

            List<CivilianStateDTO> dtoList = new List<CivilianStateDTO>();
            for (int i = 0; i < poolCount; i++)
            {
                var item = popMgr.PopulationPool[i];
                if (item.npc == null)
                    continue;

                bool isActive = item.active && item.npc.gameObject.activeSelf;
                bool isIdle = item.npc.Motor != null && item.npc.Motor.MobileAsset != null && item.npc.Motor.MobileAsset.IsIdle;
                int moves = item.npc.Motor != null ? item.npc.Motor.MoveCount : 0;

                dtoList.Add(new CivilianStateDTO
                {
                    poolIndex = i,
                    active = isActive,
                    race = (byte)item.npc.Race,
                    gender = (byte)item.npc.Gender,
                    outfitVariant = (byte)item.npc.PersonOutfitVariant,
                    faceRecordId = item.npc.PersonFaceRecordId,
                    isGuard = item.npc.IsGuard,
                    nameNPC = item.npc.NameNPC ?? string.Empty,
                    position = item.npc.transform.position,
                    yaw = item.npc.transform.eulerAngles.y,
                    isIdle = isIdle,
                    moveCount = moves
                });
            }

            if (dtoList.Count > 0)
            {
                MsgPopulationSync msg = new MsgPopulationSync
                {
                    mapID = dfLoc.Summary.MapID,
                    civilians = dtoList.ToArray()
                };
                NetworkServer.SendToAll(msg);
            }
        }

        /// <summary>
        /// Client receives authoritative civilian state from Server and updates local PopulationManager pool.
        /// </summary>
        private void OnClientReceivePopulationSync(MsgPopulationSync msg)
        {
            if (NetworkSession.IsHost)
                return;

            if (GameManager.Instance == null || GameManager.Instance.StreamingWorld == null)
                return;

            DaggerfallLocation dfLoc = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
            if (dfLoc == null || dfLoc.Summary.MapID != msg.mapID)
                return;

            PopulationManager popMgr = dfLoc.GetComponent<PopulationManager>();
            if (popMgr == null || msg.civilians == null)
                return;

            for (int i = 0; i < msg.civilians.Length; i++)
            {
                CivilianStateDTO civ = msg.civilians[i];

                // Ensure population pool has enough items up to civ.poolIndex
                while (popMgr.PopulationPool.Count <= civ.poolIndex)
                {
                    int created = popMgr.CreateNewPoolItem();
                    if (created == -1)
                        break;
                }

                if (civ.poolIndex < 0 || civ.poolIndex >= popMgr.PopulationPool.Count)
                    continue;

                var poolItem = popMgr.PopulationPool[civ.poolIndex];
                if (poolItem.npc == null)
                    continue;

                if (civ.active)
                {
                    if (!poolItem.npc.gameObject.activeSelf)
                    {
                        poolItem.npc.gameObject.SetActive(true);
                        poolItem.active = true;
                        poolItem.scheduleEnable = false;
                        poolItem.scheduleRecycle = false;
                    }

                    if (poolItem.npc.Motor != null)
                    {
                        poolItem.npc.Motor.isClientProxy = true;
                        poolItem.npc.Motor.SetNetworkTarget(civ.position, civ.yaw, civ.isIdle, civ.moveCount);
                    }

                    // Synchronize identity and appearance whenever any attribute differs
                    if (poolItem.npc.NameNPC != civ.nameNPC ||
                        poolItem.npc.PersonFaceRecordId != civ.faceRecordId ||
                        poolItem.npc.Gender != (Genders)civ.gender ||
                        poolItem.npc.Race != (Races)civ.race ||
                        poolItem.npc.PersonOutfitVariant != civ.outfitVariant ||
                        poolItem.npc.IsGuard != civ.isGuard)
                    {
                        poolItem.npc.ApplyNetworkPerson((Races)civ.race, (Genders)civ.gender, civ.outfitVariant, civ.isGuard, civ.faceRecordId, civ.nameNPC);
                    }

                    if (poolItem.npc.Asset != null)
                    {
                        MeshRenderer mr = poolItem.npc.Asset.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.enabled = (civ.moveCount > 0);
                        }
                    }

                    popMgr.PopulationPool[civ.poolIndex] = poolItem;
                }
                else
                {
                    if (poolItem.npc.gameObject.activeSelf)
                    {
                        poolItem.npc.gameObject.SetActive(false);
                        poolItem.active = false;
                        poolItem.scheduleEnable = false;
                        poolItem.scheduleRecycle = false;
                        popMgr.PopulationPool[civ.poolIndex] = poolItem;
                    }
                }
            }
        }

        #endregion

        #region Magic & PvP Synchronization

        /// <summary>
        /// Configures PvP Friendly Fire permission and broadcasts state to all peers.
        /// </summary>
        public void SetPvpAllowed(bool allowed)
        {
            isPvpAllowed = allowed;
            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(new MsgPvpStateSync { isPvpAllowed = allowed });
            }
            DaggerfallUI.AddHUDText($"[PvP] Friendly Fire is now {(allowed ? "ENABLED" : "DISABLED")}.");
        }

        /// <summary>
        /// Checks if a single effect key represents an offensive or harmful action.
        /// </summary>
        public static bool IsHarmfulEffect(string effectKey)
        {
            if (string.IsNullOrEmpty(effectKey))
                return false;

            if (effectKey.StartsWith("Damage") ||
                effectKey.StartsWith("Drain") ||
                effectKey.StartsWith("ContinuousDamage") ||
                effectKey.StartsWith("Transfer") ||
                effectKey.Equals("Paralyze", StringComparison.OrdinalIgnoreCase) ||
                effectKey.Equals("Silence", StringComparison.OrdinalIgnoreCase) ||
                effectKey.Equals("Disintegrate", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (GameManager.Instance != null && GameManager.Instance.EntityEffectBroker != null)
            {
                IEntityEffect template = GameManager.Instance.EntityEffectBroker.GetEffectTemplate(effectKey);
                if (template != null && template.Properties.MagicSkill == DFCareer.MagicSkills.Destruction)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if any effect within the bundle is harmful.
        /// </summary>
        public static bool IsHarmfulBundle(EffectBundleSettings settings)
        {
            if (settings.Effects == null || settings.Effects.Length == 0)
                return false;

            for (int i = 0; i < settings.Effects.Length; i++)
            {
                if (IsHarmfulEffect(settings.Effects[i].Key))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Broadcasts local player's spellcast to peers for projectile, audio, and animation replication.
        /// </summary>
        public void BroadcastLocalSpellCast(EntityEffectBundle readySpell)
        {
            if (readySpell == null || !NetworkSession.IsActive)
                return;

            Vector3 origin = GameManager.Instance.MainCamera != null
                ? GameManager.Instance.MainCamera.transform.position
                : (GameManager.Instance.PlayerObject != null ? GameManager.Instance.PlayerObject.transform.position + Vector3.up * 1.5f : Vector3.zero);
            Vector3 dir = GameManager.Instance.MainCamera != null ? GameManager.Instance.MainCamera.transform.forward : Vector3.forward;

            MsgCastSpell msg = new MsgCastSpell
            {
                casterConnectionId = NetworkSession.IsHost ? 0 : localConnectionId,
                casterName = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player",
                spellName = readySpell.Settings.Name,
                elementType = readySpell.Settings.ElementType,
                targetType = readySpell.Settings.TargetType,
                origin = origin,
                direction = dir
            };

            if (NetworkServer.active)
            {
                foreach (var kvp in NetworkServer.connections)
                {
                    if (kvp.Value != null && kvp.Value.isReady)
                    {
                        kvp.Value.Send(msg);
                    }
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }
        }

        private void OnServerReceiveCastSpell(NetworkConnection conn, MsgCastSpell msg)
        {
            try
            {
                msg.casterConnectionId = conn.connectionId;
                if (NetworkSession.IsHost)
                {
                    PlayRemoteSpellCastVisual(msg);
                }

                foreach (var kvp in NetworkServer.connections)
                {
                    if (kvp.Key != conn.connectionId && kvp.Value != null && kvp.Value.isReady)
                    {
                        kvp.Value.Send(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in OnServerReceiveCastSpell: {ex.Message}");
            }
        }

        private void OnClientReceiveCastSpell(MsgCastSpell msg)
        {
            try
            {
                if (NetworkSession.IsHost && msg.casterConnectionId == 0)
                    return;
                if (!NetworkSession.IsHost && msg.casterConnectionId == localConnectionId)
                    return;

                PlayRemoteSpellCastVisual(msg);
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in OnClientReceiveCastSpell: {ex.Message}");
            }
        }

        private void PlayRemoteSpellCastVisual(MsgCastSpell msg)
        {
            try
            {
                NativePlayerPuppet puppet = null;
                if (remotePlayerPuppets.TryGetValue(msg.casterConnectionId, out NativePlayerPuppet p))
                {
                    puppet = p;
                }

                if (puppet != null)
                {
                    puppet.TriggerCastAnimation();

                    if (GameManager.Instance.PlayerEffectManager != null && puppet.EntityBehaviour != null)
                    {
                        int soundId = GameManager.Instance.PlayerEffectManager.GetCastSoundID(msg.elementType);
                        GameManager.Instance.PlayerEffectManager.PlayCastSound(puppet.EntityBehaviour, soundId);
                    }
                }

                if (msg.targetType == TargetTypes.SingleTargetAtRange || msg.targetType == TargetTypes.AreaAtRange)
                {
                    if (GameManager.Instance.PlayerEffectManager != null)
                    {
                        DaggerfallMissile missile = GameManager.Instance.PlayerEffectManager.InstantiateSpellMissile(msg.elementType);
                        if (missile != null)
                        {
                            missile.TargetType = msg.targetType;
                            missile.ElementType = msg.elementType;

                            Vector3 spawnPos = msg.origin + msg.direction * 0.6f;
                            missile.CustomAimPosition = spawnPos;
                            missile.CustomAimDirection = msg.direction;
                            missile.transform.position = spawnPos;
                            missile.Payload = null; // Visual only on remote peers
                            missile.Caster = puppet != null ? puppet.EntityBehaviour : null;

                            Collider col = missile.GetComponent<Collider>();
                            if (col != null)
                            {
                                col.isTrigger = true;
                                if (puppet != null)
                                {
                                    foreach (var c in puppet.GetComponentsInChildren<Collider>())
                                        if (c != null) Physics.IgnoreCollision(col, c, true);
                                }
                                if (GameManager.Instance.PlayerObject != null)
                                {
                                    foreach (var c in GameManager.Instance.PlayerObject.GetComponentsInChildren<Collider>())
                                        if (c != null) Physics.IgnoreCollision(col, c, true);
                                }
                            }

                            Rigidbody rb = missile.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.isKinematic = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in PlayRemoteSpellCastVisual: {ex.Message}");
            }
        }

        #region Arrow Projectile Synchronization

        public void BroadcastArrowShot(Vector3 origin, Vector3 direction, int arrowType)
        {
            if (!NetworkSession.IsActive)
                return;

            MsgArrowShot msg = new MsgArrowShot
            {
                shooterConnectionId = NetworkSession.IsHost ? 0 : localConnectionId,
                origin = origin,
                direction = direction,
                arrowType = arrowType
            };

            if (NetworkServer.active)
            {
                foreach (var kvp in NetworkServer.connections)
                {
                    if (kvp.Value != null && kvp.Value.isReady)
                    {
                        kvp.Value.Send(msg);
                    }
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }
        }

        public void BroadcastEnemyArrowShot(Vector3 origin, Vector3 direction)
        {
            if (!NetworkServer.active)
                return;

            MsgArrowShot msg = new MsgArrowShot
            {
                shooterConnectionId = -1,
                origin = origin,
                direction = direction,
                arrowType = 0
            };

            foreach (var kvp in NetworkServer.connections)
            {
                if (kvp.Value != null && kvp.Value.isReady)
                {
                    kvp.Value.Send(msg);
                }
            }
        }

        private void OnServerReceiveArrowShot(NetworkConnection conn, MsgArrowShot msg)
        {
            try
            {
                msg.shooterConnectionId = conn.connectionId;
                if (NetworkSession.IsHost)
                {
                    PlayRemoteArrowShotVisual(msg);
                }

                foreach (var kvp in NetworkServer.connections)
                {
                    if (kvp.Key != conn.connectionId && kvp.Value != null && kvp.Value.isReady)
                    {
                        kvp.Value.Send(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Arrow] Error in OnServerReceiveArrowShot: {ex.Message}");
            }
        }

        private void OnClientReceiveArrowShot(MsgArrowShot msg)
        {
            try
            {
                if (msg.shooterConnectionId == localConnectionId)
                    return;

                PlayRemoteArrowShotVisual(msg);
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Arrow] Error in OnClientReceiveArrowShot: {ex.Message}");
            }
        }

        private void PlayRemoteArrowShotVisual(MsgArrowShot msg)
        {
            try
            {
                NativePlayerPuppet puppet = null;
                if (msg.shooterConnectionId >= 0 && remotePlayerPuppets.TryGetValue(msg.shooterConnectionId, out NativePlayerPuppet p))
                {
                    puppet = p;
                }

                if (puppet != null)
                {
                    puppet.TriggerAttackAnimation();
                }

                if (GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ArrowMissilePrefab != null)
                {
                    DaggerfallMissile arrow = Instantiate(GameManager.Instance.WeaponManager.ArrowMissilePrefab);
                    if (arrow != null)
                    {
                        Vector3 spawnPos = msg.origin + msg.direction * 0.6f;
                        arrow.CustomAimPosition = spawnPos;
                        arrow.CustomAimDirection = msg.direction;
                        arrow.transform.position = spawnPos;
                        arrow.TargetType = TargetTypes.SingleTargetAtRange;
                        arrow.ElementType = ElementTypes.None;
                        arrow.IsArrow = true;
                        arrow.IsArrowSummoned = (msg.arrowType == 1);
                        arrow.Payload = null;
                        arrow.Caster = puppet != null ? puppet.EntityBehaviour : null;

                        Collider col = arrow.GetComponent<Collider>();
                        if (col != null)
                        {
                            col.isTrigger = true;
                            if (puppet != null)
                            {
                                foreach (var c in puppet.GetComponentsInChildren<Collider>())
                                    if (c != null) Physics.IgnoreCollision(col, c, true);
                            }
                            if (GameManager.Instance.PlayerObject != null)
                            {
                                foreach (var c in GameManager.Instance.PlayerObject.GetComponentsInChildren<Collider>())
                                    if (c != null) Physics.IgnoreCollision(col, c, true);
                            }
                        }

                        Rigidbody rb = arrow.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Arrow] Error in PlayRemoteArrowShotVisual: {ex.Message}");
            }
        }

        #endregion

        #region DM Spell Teaching System

        public void TeachDmSpellToTarget(int targetConnId, string spellName, int spellId = -1)
        {
            if (GameManager.Instance.EntityEffectBroker == null)
            {
                DaggerfallUI.AddHUDText("[DM Spell] EntityEffectBroker no disponible.");
                return;
            }

            SpellRecord.SpellRecordData spellData = new SpellRecord.SpellRecordData();
            bool found = false;

            if (spellId >= 0 && GameManager.Instance.EntityEffectBroker.GetClassicSpellRecord(spellId, out spellData))
            {
                found = true;
            }
            else
            {
                foreach (var s in GameManager.Instance.EntityEffectBroker.StandardSpells)
                {
                    if (string.Equals(s.spellName, spellName, StringComparison.OrdinalIgnoreCase) ||
                        s.spellName.IndexOf(spellName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        spellData = s;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                DaggerfallUI.AddHUDText($"[DM Spell] Hechizo '{spellName}' no encontrado.");
                return;
            }

            EffectBundleSettings bundle;
            if (!GameManager.Instance.EntityEffectBroker.ClassicSpellRecordDataToEffectBundleSettings(spellData, BundleTypes.Spell, out bundle))
            {
                DaggerfallUI.AddHUDText($"[DM Spell] Error al crear bundle para '{spellName}'.");
                return;
            }

            if (targetConnId == 0 || targetConnId == localConnectionId)
            {
                if (GameManager.Instance.PlayerEntity != null)
                {
                    GameManager.Instance.ItemHelper.AddSpellbookItem(GameManager.Instance.PlayerEntity);
                    GameManager.Instance.PlayerEntity.AddSpell(bundle);
                    DaggerfallUI.AddHUDText($"[DM Spell] Aprendiste '{bundle.Name}'. ¡Abre tu Spellbook (B)!");
                }
            }
            else
            {
                string json = SaveLoadManager.Serialize(typeof(EffectBundleSettings), bundle, false);
                MsgTeachSpell msg = new MsgTeachSpell
                {
                    targetConnectionId = targetConnId,
                    spellBundleJson = json
                };

                if (NetworkServer.active && NetworkServer.connections.TryGetValue(targetConnId, out NetworkConnectionToClient conn) && conn != null)
                {
                    conn.Send(msg);
                    string targetName = connectedPlayerProfiles.TryGetValue(targetConnId, out MsgPlayerProfile prof) ? prof.playerName : $"ID {targetConnId}";
                    DaggerfallUI.AddHUDText($"[DM Spell] Hechizo '{bundle.Name}' otorgado a {targetName}.");
                }
            }
        }

        private void OnClientReceiveTeachSpell(MsgTeachSpell msg)
        {
            try
            {
                if (GameManager.Instance.PlayerEntity == null)
                    return;

                EffectBundleSettings bundle = (EffectBundleSettings)SaveLoadManager.Deserialize(typeof(EffectBundleSettings), msg.spellBundleJson);
                if (bundle.Effects == null || bundle.Effects.Length == 0)
                    return;

                GameManager.Instance.ItemHelper.AddSpellbookItem(GameManager.Instance.PlayerEntity);
                GameManager.Instance.PlayerEntity.AddSpell(bundle);
                DaggerfallUI.AddHUDText($"[DM Magic] ¡El Dungeon Master te ha enseñado '{bundle.Name}'! (Pulsa 'B' para abrir tu Spellbook)");
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in OnClientReceiveTeachSpell: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Sends a spell payload bundle to another player (beneficial or harmful if PvP enabled).
        /// </summary>
        public void SendSpellPayloadToPlayer(int targetConnId, EntityEffectBundle payload)
        {
            if (payload == null || payload.Settings.Effects == null || payload.Settings.Effects.Length == 0)
                return;

            bool isHarmful = IsHarmfulBundle(payload.Settings);
            if (isHarmful && !isPvpAllowed)
            {
                DaggerfallUI.AddHUDText("[PvP] Fuego amigo desactivado (net_pvp está en OFF).");
                return;
            }

            string json = SaveLoadManager.Serialize(typeof(EffectBundleSettings), payload.Settings, false);
            MsgApplySpellBundle msg = new MsgApplySpellBundle
            {
                targetConnectionId = targetConnId,
                casterConnectionId = NetworkSession.IsHost ? 0 : localConnectionId,
                casterName = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player",
                spellName = payload.Settings.Name,
                elementType = payload.Settings.ElementType,
                bundleSettingsJson = json,
                isHarmful = isHarmful
            };

            if (NetworkSession.IsHost)
            {
                if (targetConnId == 0)
                {
                    ApplyReceivedSpellBundle(msg);
                }
                else if (NetworkServer.connections.TryGetValue(targetConnId, out NetworkConnectionToClient conn) && conn != null && conn.isReady)
                {
                    conn.Send(msg);
                }
            }
            else if (NetworkClient.isConnected)
            {
                NetworkClient.Send(msg);
            }
        }

        private void OnServerReceiveApplySpellBundle(NetworkConnection conn, MsgApplySpellBundle msg)
        {
            try
            {
                msg.casterConnectionId = conn.connectionId;
                if (msg.isHarmful && !isPvpAllowed)
                    return;

                if (msg.targetConnectionId == 0)
                {
                    ApplyReceivedSpellBundle(msg);
                }
                else if (NetworkServer.connections.TryGetValue(msg.targetConnectionId, out NetworkConnectionToClient targetConn) && targetConn != null && targetConn.isReady)
                {
                    targetConn.Send(msg);
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in OnServerReceiveApplySpellBundle: {ex.Message}");
            }
        }

        private void OnClientReceiveApplySpellBundle(MsgApplySpellBundle msg)
        {
            try
            {
                int myId = NetworkSession.IsHost ? 0 : localConnectionId;
                if (msg.targetConnectionId != myId)
                    return;

                ApplyReceivedSpellBundle(msg);
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Magic] Error in OnClientReceiveApplySpellBundle: {ex.Message}");
            }
        }

        private void ApplyReceivedSpellBundle(MsgApplySpellBundle msg)
        {
            if (string.IsNullOrEmpty(msg.bundleSettingsJson))
                return;

            if (msg.isHarmful && !isPvpAllowed)
                return;

            try
            {
                EffectBundleSettings settings = (EffectBundleSettings)SaveLoadManager.Deserialize(typeof(EffectBundleSettings), msg.bundleSettingsJson);
                if (settings.Effects == null || settings.Effects.Length == 0)
                    return;

                DaggerfallEntityBehaviour casterBehaviour = null;
                if (remotePlayerPuppets.TryGetValue(msg.casterConnectionId, out NativePlayerPuppet puppet) && puppet != null)
                {
                    casterBehaviour = puppet.EntityBehaviour;
                }

                EntityEffectBundle bundle = new EntityEffectBundle(settings, casterBehaviour);
                if (GameManager.Instance.PlayerEffectManager != null)
                {
                    GameManager.Instance.PlayerEffectManager.AssignBundle(bundle, AssignBundleFlags.ShowNonPlayerFailures);

                    if (msg.isHarmful)
                    {
                        DaggerfallUI.AddHUDText($"[PvP] Recibiste impacto mágico de {msg.casterName}: {msg.spellName}");
                    }
                    else
                    {
                        DaggerfallUI.AddHUDText($"[Magia] {msg.casterName} aplicó sobre vos: {msg.spellName}");
                    }
                }
            }
            catch (Exception ex)
            {
                DaggerfallUnity.LogMessage($"[DFU Magic] Failed to apply received spell bundle: {ex.Message}", true);
            }
        }

        private void OnClientReceivePvpStateSync(MsgPvpStateSync msg)
        {
            isPvpAllowed = msg.isPvpAllowed;
            DaggerfallUI.AddHUDText($"[PvP] Friendly Fire is now {(isPvpAllowed ? "ENABLED" : "DISABLED")}.");
        }

        #endregion

        #region Pre-Game Multiplayer Lobby & Mod Compatibility Verification

        public readonly Dictionary<int, LobbyPlayerDTO> serverLobbyPlayers = new Dictionary<int, LobbyPlayerDTO>();
        public LobbyRosterBundle currentLobbyRoster = new LobbyRosterBundle();
        public event Action OnLobbyRosterUpdatedEvent;

        // Local player's selected character & lobby state
        public bool localLobbyIsDm = false;
        public int localLobbySelectionMode = 0; // 0 = None, 1 = New Character, 2 = Saved Character
        public int localLobbySaveKey = -1;
        public string localLobbyCharacterName = "";
        public string localLobbyRaceName = "";
        public string localLobbyClassName = "";
        public int localLobbyLevel = 1;
        public bool localLobbyIsReady = false;
        public bool isCreatingCharacterForLobby = false;

        /// <summary>
        /// Collects all currently enabled .dfmod filenames on this machine.
        /// </summary>
        public string[] GetLocalActiveModList()
        {
            List<string> modList = new List<string>();
            if (DaggerfallWorkshop.Game.Utility.ModSupport.ModManager.Instance != null &&
                DaggerfallWorkshop.Game.Utility.ModSupport.ModManager.Instance.Mods != null)
            {
                foreach (var mod in DaggerfallWorkshop.Game.Utility.ModSupport.ModManager.Instance.Mods)
                {
                    if (mod != null && mod.Enabled && !string.IsNullOrEmpty(mod.FileName))
                    {
                        modList.Add($"{mod.FileName} ({mod.ModInfo?.ModVersion ?? "1.0"})");
                    }
                }
            }
            return modList.ToArray();
        }

        public void InitializeServerLobbyHost()
        {
            serverLobbyPlayers.Clear();
            string[] hostMods = GetLocalActiveModList();

            string pName = PlayerPrefs.GetString("DFU_LastPlayerName", "Host / DM");
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && !string.IsNullOrEmpty(GameManager.Instance.PlayerEntity.Name))
                pName = GameManager.Instance.PlayerEntity.Name;

            LobbyPlayerDTO hostDto = new LobbyPlayerDTO
            {
                connectionId = 0,
                playerName = pName,
                isHost = true,
                isDm = localLobbyIsDm,
                selectionMode = localLobbySelectionMode,
                saveKey = localLobbySaveKey,
                characterName = localLobbyCharacterName,
                raceName = localLobbyRaceName,
                className = localLobbyClassName,
                level = localLobbyLevel,
                isReady = localLobbyIsReady,
                modsCompatible = true,
                modMismatchSummary = "Anfitrión (Base de referencia)",
                activeMods = hostMods
            };

            serverLobbyPlayers[0] = hostDto;
            BroadcastLobbyState();
        }

        public void SendLocalLobbyUpdate()
        {
            string pName = PlayerPrefs.GetString("DFU_LastPlayerName", "Adventurer");
            if (!string.IsNullOrEmpty(localLobbyCharacterName))
                pName = localLobbyCharacterName;
            else if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && !string.IsNullOrEmpty(GameManager.Instance.PlayerEntity.Name))
                pName = GameManager.Instance.PlayerEntity.Name;

            string[] localMods = GetLocalActiveModList();

            if (NetworkServer.active)
            {
                // Update Host entry directly
                if (!serverLobbyPlayers.TryGetValue(0, out LobbyPlayerDTO hostDto))
                {
                    hostDto = new LobbyPlayerDTO { connectionId = 0, isHost = true };
                    serverLobbyPlayers[0] = hostDto;
                }
                hostDto.playerName = pName;
                hostDto.isDm = localLobbyIsDm;
                hostDto.selectionMode = localLobbySelectionMode;
                hostDto.saveKey = localLobbySaveKey;
                hostDto.characterName = localLobbyCharacterName;
                hostDto.raceName = localLobbyRaceName;
                hostDto.className = localLobbyClassName;
                hostDto.level = localLobbyLevel;
                hostDto.isReady = localLobbyIsReady;
                hostDto.modsCompatible = true;
                hostDto.modMismatchSummary = "Anfitrión (Base de referencia)";
                hostDto.activeMods = localMods;

                BroadcastLobbyState();
            }
            else if (NetworkClient.isConnected)
            {
                MsgLobbyClientUpdate updateMsg = new MsgLobbyClientUpdate
                {
                    playerName = pName,
                    isDm = localLobbyIsDm,
                    selectionMode = localLobbySelectionMode,
                    saveKey = localLobbySaveKey,
                    characterName = localLobbyCharacterName,
                    raceName = localLobbyRaceName,
                    className = localLobbyClassName,
                    level = localLobbyLevel,
                    isReady = localLobbyIsReady,
                    activeModsJson = string.Join("|", localMods)
                };
                NetworkClient.Send(updateMsg);
            }
        }

        private void OnServerReceiveLobbyClientUpdate(NetworkConnection conn, MsgLobbyClientUpdate msg)
        {
            if (conn == null) return;

            string[] clientMods = string.IsNullOrEmpty(msg.activeModsJson)
                ? new string[0]
                : msg.activeModsJson.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            string[] hostMods = GetLocalActiveModList();
            EvaluateModCompatibility(hostMods, clientMods, out bool isCompat, out string summary);

            LobbyPlayerDTO dto = new LobbyPlayerDTO
            {
                connectionId = conn.connectionId,
                playerName = string.IsNullOrEmpty(msg.playerName) ? $"Jugador #{conn.connectionId}" : msg.playerName,
                isHost = false,
                isDm = msg.isDm,
                selectionMode = msg.selectionMode,
                saveKey = msg.saveKey,
                characterName = msg.characterName,
                raceName = msg.raceName,
                className = msg.className,
                level = msg.level,
                isReady = msg.isReady,
                modsCompatible = isCompat,
                modMismatchSummary = summary,
                activeMods = clientMods
            };

            serverLobbyPlayers[conn.connectionId] = dto;

            // Also keep connectedPlayerProfiles updated with chosen character details
            if (connectedPlayerProfiles.TryGetValue(conn.connectionId, out MsgPlayerProfile prof))
            {
                if (!string.IsNullOrEmpty(msg.characterName))
                    prof.playerName = msg.characterName;
                if (!string.IsNullOrEmpty(msg.raceName))
                    prof.raceName = msg.raceName;
                if (!string.IsNullOrEmpty(msg.className))
                    prof.careerName = msg.className;
                connectedPlayerProfiles[conn.connectionId] = prof;
            }

            BroadcastLobbyState();
        }

        private void EvaluateModCompatibility(string[] hostMods, string[] clientMods, out bool compatible, out string summary)
        {
            HashSet<string> hostSet = new HashSet<string>(hostMods ?? new string[0], StringComparer.OrdinalIgnoreCase);
            HashSet<string> clientSet = new HashSet<string>(clientMods ?? new string[0], StringComparer.OrdinalIgnoreCase);

            List<string> missing = new List<string>();
            List<string> extra = new List<string>();

            foreach (string hm in hostSet)
            {
                if (!clientSet.Contains(hm))
                    missing.Add(hm);
            }
            foreach (string cm in clientSet)
            {
                if (!hostSet.Contains(cm))
                    extra.Add(cm);
            }

            if (missing.Count == 0 && extra.Count == 0)
            {
                compatible = true;
                summary = $"OK ({hostSet.Count} mods sincronizados)";
            }
            else
            {
                compatible = false;
                List<string> parts = new List<string>();
                if (missing.Count > 0)
                    parts.Add($"Faltan {missing.Count}: {string.Join(", ", missing.ToArray())}");
                if (extra.Count > 0)
                    parts.Add($"Extra {extra.Count}: {string.Join(", ", extra.ToArray())}");
                summary = string.Join(" | ", parts.ToArray());
            }
        }

        public void BroadcastLobbyState()
        {
            if (!NetworkServer.active)
                return;

            LobbyRosterBundle bundle = new LobbyRosterBundle
            {
                sessionMode = localLobbySelectionMode,
                hostRequiredMods = GetLocalActiveModList(),
                players = new List<LobbyPlayerDTO>(serverLobbyPlayers.Values)
            };

            currentLobbyRoster = bundle;
            string json = SaveLoadManager.Serialize(typeof(LobbyRosterBundle), bundle, false);
            NetworkServer.SendToAll(new MsgLobbyStateSync { rosterBundleJson = json });
            OnLobbyRosterUpdatedEvent?.Invoke();
        }

        private void OnClientReceiveLobbyStateSync(MsgLobbyStateSync msg)
        {
            if (string.IsNullOrEmpty(msg.rosterBundleJson))
                return;

            try
            {
                LobbyRosterBundle bundle = SaveLoadManager.Deserialize(typeof(LobbyRosterBundle), msg.rosterBundleJson) as LobbyRosterBundle;
                if (bundle != null)
                {
                    currentLobbyRoster = bundle;
                    OnLobbyRosterUpdatedEvent?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogNetwork($"[DFU Lobby] Error deserializing LobbyRosterBundle: {ex.Message}");
            }
        }

        public bool AreAllLobbyPlayersReady(out int dominantMode)
        {
            dominantMode = localLobbySelectionMode;
            if (currentLobbyRoster == null || currentLobbyRoster.players == null || currentLobbyRoster.players.Count == 0)
                return false;

            foreach (var p in currentLobbyRoster.players)
            {
                if (!p.isReady || p.selectionMode == 0)
                    return false;
            }
            return true;
        }

        public void HostLaunchLobbySession()
        {
            if (!NetworkServer.active)
                return;

            int launchMode = localLobbySelectionMode != 0 ? localLobbySelectionMode : 2;
            NetworkServer.SendToAll(new MsgLobbyLaunchSession
            {
                sessionMode = launchMode,
                hostSaveKey = localLobbySaveKey
            });
        }

        private void OnClientReceiveLobbyLaunchSession(MsgLobbyLaunchSession msg)
        {
            LogNetwork($"[DFU Lobby] Launching multiplayer session into 3D world (Mode={msg.sessionMode})...");
            ExecuteLocalLobbyLaunch();
        }

        public void ExecuteLocalLobbyLaunch()
        {
            isCreatingCharacterForLobby = false;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.ClearAllActions();
                InputManager.Instance.ToggleAutorun = false;
            }
            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }

            // Update local profile name before entering world
            if (!string.IsNullOrEmpty(localLobbyCharacterName) && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.Name = localLobbyCharacterName;
            }

            if (DaggerfallUI.Instance != null && DaggerfallUI.UIManager != null)
            {
                DaggerfallUI.Instance.PopToHUD();
            }

            StartGameBehaviour startGameBehaviour = FindObjectOfType<StartGameBehaviour>();

            if (localLobbySelectionMode == 1 && startGameBehaviour != null && startGameBehaviour.CharacterDocument != null)
            {
                // Launch newly created character from wizard
                startGameBehaviour.StartMethod = StartGameBehaviour.StartMethods.NewCharacter;
            }
            else if (localLobbySelectionMode == 2 && localLobbySaveKey >= 0 && GameManager.Instance.SaveLoadManager != null)
            {
                // Load selected saved character
                GameManager.Instance.SaveLoadManager.EnumerateSaves();
                GameManager.Instance.SaveLoadManager.Load(localLobbySaveKey);
            }
            else if (GameManager.Instance.SaveLoadManager != null)
            {
                // Fallback: load most recent save or start new character
                GameManager.Instance.SaveLoadManager.EnumerateSaves();
                int recentKey = GameManager.Instance.SaveLoadManager.FindMostRecentSave();
                if (recentKey >= 0)
                {
                    GameManager.Instance.SaveLoadManager.Load(recentKey);
                }
                else if (startGameBehaviour != null)
                {
                    startGameBehaviour.StartMethod = StartGameBehaviour.StartMethods.NewCharacter;
                }
            }
        }

        #endregion
    }
}
