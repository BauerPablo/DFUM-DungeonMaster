// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Networking;
using DaggerfallWorkshop.Game.Serialization;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Native Daggerfall Window refactored with Dark Fantasy Pixel-Art framing for launching,
    /// hosting, connecting, and staging players in a pre-game Lobby with live Mod Compatibility
    /// and Character Selection (New Character or Load Saved Character with Name, Race, and Class preview).
    /// </summary>
    public class DaggerfallMultiplayerWindow : DaggerfallPopupWindow
    {
        #region UI Constants & Cached Textures

        private const int MainWidth = 304;
        private const int MainHeight = 190;

        private Texture2D mainFrameTex;
        private Texture2D contentRecessedTex;
        private Texture2D parchmentCardTex;
        private Texture2D tabActiveTex;
        private Texture2D tabInactiveTex;
        private Texture2D crimsonBtnTex;
        private Texture2D emeraldBtnTex;
        private Texture2D ironBtnTex;
        private Texture2D goldBtnTex;

        #endregion

        #region Panels & Controls

        private Panel mainPanel = new Panel();
        private Panel contentPanel = new Panel();

        private Button joinTabButton;
        private Button hostTabButton;
        private Button lobbyTabButton;
        private Button historyTabButton;
        private Button modsTabButton;

        // 0 = Join, 1 = Host, 2 = Lobby (Pre-Game Room), 3 = History, 4 = Mods
        private int activeTab = 0;

        // Tab 0: Join
        private TextBox joinIpTextBox;
        private TextBox joinPortTextBox;
        private TextBox joinNameTextBox;
        private Button joinRoleButton;
        private bool joinAsDm = false;
        private TextLabel joinStatusLabel;

        // Tab 1: Host
        private TextBox hostPortTextBox;
        private Button hostPvpButton;
        private bool hostPvpEnabled = false;
        private Button hostRoleButton;
        private bool hostAsDm = true;
        private TextLabel hostStatusLabel;

        // Tab 2: Pre-Game Lobby & Character Selection
        private bool showingSavePickerModal = false;
        private ListBox characterNamesListBox;
        private ListBox characterSavesListBox;
        private readonly List<string> enumeratedCharNames = new List<string>();
        private readonly List<int> enumeratedCharSaveKeys = new List<int>();
        private int selectedSaveKeyPreview = -1;
        private string previewCharName = "";
        private string previewRaceName = "";
        private string previewClassName = "";
        private int previewCharLevel = 1;
        private string previewSaveCaption = "";

        private TextLabel previewNameLabel;
        private TextLabel previewRaceLabel;
        private TextLabel previewClassLabel;
        private TextLabel previewLevelLabel;
        private TextLabel previewSaveNameLabel;

        // Tab 3: History
        private ListBox historyListBox;
        private readonly List<string> recentServers = new List<string>();

        private bool needsLobbyUiRefresh = false;
        private float lobbyPollTimer = 0f;
        private int lastKnownRosterCount = -1;

        #endregion

        #region Constructor

        public DaggerfallMultiplayerWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null)
            : base(uiManager, previousWindow)
        {
            AllowCancel = true;
        }

        #endregion

        #region Lifecycle & Setup

        protected override void Setup()
        {
            base.Setup();

            ParentPanel.BackgroundColor = new Color(0.03f, 0.02f, 0.04f, 0.85f);

            GeneratePixelArtAssets();

            // Main Ornate Dark Fantasy Frame
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.Size = new Vector2(MainWidth, MainHeight);
            mainPanel.BackgroundTexture = mainFrameTex;
            NativePanel.Components.Add(mainPanel);

            // Title Banner
            TextLabel titleLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 6), mainPanel);
            titleLabel.Text = "DARK FANTASY MULTIPLAYER & TTRPG LOBBY";
            titleLabel.TextColor = DarkFantasyPixelArtTheme.TextGold;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.ShadowPosition = Vector2.one;
            titleLabel.ShadowColor = Color.black;

            // Top Navigation Tabs
            float tabY = 17f;
            float tabW = 56f;
            float tabH = 13f;

            joinTabButton = CreateTabButton(new Vector2(7, tabY), new Vector2(tabW, tabH), "JOIN", 0);
            hostTabButton = CreateTabButton(new Vector2(65, tabY), new Vector2(tabW, tabH), "HOST", 1);
            lobbyTabButton = CreateTabButton(new Vector2(123, tabY), new Vector2(tabW, tabH), "LOBBY", 2);
            historyTabButton = CreateTabButton(new Vector2(181, tabY), new Vector2(tabW, tabH), "SERVERS", 3);
            modsTabButton = CreateTabButton(new Vector2(239, tabY), new Vector2(tabW, tabH), "MODS", 4);

            // Recessed Interior Content Area
            contentPanel.Position = new Vector2(7, 32);
            contentPanel.Size = new Vector2(MainWidth - 14, MainHeight - 50);
            contentPanel.BackgroundTexture = contentRecessedTex;
            mainPanel.Components.Add(contentPanel);

            // Bottom Bar: Return / Disconnect
            Button closeButton = DaggerfallUI.AddButton(new Vector2(7, MainHeight - 16), new Vector2(MainWidth - 14, 11), mainPanel);
            closeButton.BackgroundTexture = crimsonBtnTex;
            TextLabel closeLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 2), closeButton);
            closeLabel.Text = "RETURN TO MAIN MENU (ESC)";
            closeLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            closeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            closeButton.OnMouseClick += (s, e) => CloseWindow();

            LoadRecentServers();

            // Subscribe to network events so Lobby refreshes automatically
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.OnLobbyRosterUpdatedEvent -= HandleLobbyRosterUpdated;
                DFUNetworkManager.Instance.OnLobbyRosterUpdatedEvent += HandleLobbyRosterUpdated;
                DFUNetworkManager.Instance.OnClientConnectedToHostEvent -= HandleLobbyRosterUpdated;
                DFUNetworkManager.Instance.OnClientConnectedToHostEvent += HandleLobbyRosterUpdated;
            }

            if (NetworkSession.IsActive)
                SwitchTab(2);
            else
                SwitchTab(0);
        }

        public override void OnPush()
        {
            base.OnPush();
            if (NetworkSession.IsActive && IsSetup)
            {
                showingSavePickerModal = false;
                SwitchTab(2);
            }
        }

        public override void OnReturn()
        {
            base.OnReturn();
            // When returning from StartNewGameWizard after creating a character for the Lobby
            if (NetworkSession.IsActive)
            {
                showingSavePickerModal = false;
                SwitchTab(2);
            }
        }

        public override void OnPop()
        {
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.OnLobbyRosterUpdatedEvent -= HandleLobbyRosterUpdated;
                DFUNetworkManager.Instance.OnClientConnectedToHostEvent -= HandleLobbyRosterUpdated;
            }
            base.OnPop();
        }

        public override void Update()
        {
            base.Update();

            lobbyPollTimer += Time.realtimeSinceStartup > 0 ? 0.016f : 0.016f;
            if (activeTab == 2 && !showingSavePickerModal)
            {
                int currentCount = (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.currentLobbyRoster != null && DFUNetworkManager.Instance.currentLobbyRoster.players != null)
                    ? DFUNetworkManager.Instance.currentLobbyRoster.players.Count
                    : 0;
                if (currentCount != lastKnownRosterCount)
                {
                    lastKnownRosterCount = currentCount;
                    needsLobbyUiRefresh = true;
                }
            }

            if (needsLobbyUiRefresh)
            {
                needsLobbyUiRefresh = false;
                if (activeTab == 2 && !showingSavePickerModal)
                {
                    SwitchTab(2);
                }
            }
        }

        private void HandleLobbyRosterUpdated()
        {
            needsLobbyUiRefresh = true;
        }

        private void GeneratePixelArtAssets()
        {
            if (mainFrameTex == null)
                mainFrameTex = DarkFantasyPixelArtTheme.CreateOrnateWindowTexture(MainWidth, MainHeight, DarkFantasyPixelArtTheme.ObsidianBg, false);
            if (contentRecessedTex == null)
                contentRecessedTex = DarkFantasyPixelArtTheme.CreateRecessedPanelTexture(MainWidth - 14, MainHeight - 50, false);
            if (parchmentCardTex == null)
                parchmentCardTex = DarkFantasyPixelArtTheme.CreateOrnateWindowTexture(140, 88, DarkFantasyPixelArtTheme.ParchmentBase, true);

            if (tabActiveTex == null)
                tabActiveTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(56, 13, new Color32(110, 78, 34, 255), DarkFantasyPixelArtTheme.GoldHighlight, new Color32(48, 32, 12, 255), false);
            if (tabInactiveTex == null)
                tabInactiveTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(56, 13, DarkFantasyPixelArtTheme.IronButtonBase, DarkFantasyPixelArtTheme.IronButtonHighlight, DarkFantasyPixelArtTheme.IronButtonShadow, false);

            if (crimsonBtnTex == null)
                crimsonBtnTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(120, 15, DarkFantasyPixelArtTheme.CrimsonBase, DarkFantasyPixelArtTheme.CrimsonHighlight, DarkFantasyPixelArtTheme.CrimsonShadow);
            if (emeraldBtnTex == null)
                emeraldBtnTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(120, 15, DarkFantasyPixelArtTheme.EmeraldBase, DarkFantasyPixelArtTheme.EmeraldHighlight, DarkFantasyPixelArtTheme.EmeraldShadow);
            if (ironBtnTex == null)
                ironBtnTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(120, 15, DarkFantasyPixelArtTheme.IronButtonBase, DarkFantasyPixelArtTheme.IronButtonHighlight, DarkFantasyPixelArtTheme.IronButtonShadow);
            if (goldBtnTex == null)
                goldBtnTex = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(120, 15, new Color32(128, 92, 34, 255), DarkFantasyPixelArtTheme.GoldBright, new Color32(62, 42, 14, 255));
        }

        private Button CreateTabButton(Vector2 pos, Vector2 size, string text, int tabIndex)
        {
            Button btn = DaggerfallUI.AddButton(pos, size, mainPanel);
            btn.BackgroundTexture = (activeTab == tabIndex) ? tabActiveTex : tabInactiveTex;

            TextLabel label = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 3), btn);
            label.Text = text;
            label.TextColor = (activeTab == tabIndex) ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextMuted;
            label.HorizontalAlignment = HorizontalAlignment.Center;

            btn.OnMouseClick += (s, e) =>
            {
                showingSavePickerModal = false;
                SwitchTab(tabIndex);
            };
            return btn;
        }

        private void SwitchTab(int tabIndex)
        {
            activeTab = tabIndex;
            contentPanel.Components.Clear();

            UpdateTabButtonStyles();

            switch (activeTab)
            {
                case 0:
                    SetupJoinTab();
                    break;
                case 1:
                    SetupHostTab();
                    break;
                case 2:
                    SetupLobbyTab();
                    break;
                case 3:
                    SetupHistoryTab();
                    break;
                case 4:
                    SetupModsTab();
                    break;
            }
        }

        private void UpdateTabButtonStyles()
        {
            SetTabStyle(joinTabButton, 0);
            SetTabStyle(hostTabButton, 1);
            SetTabStyle(lobbyTabButton, 2);
            SetTabStyle(historyTabButton, 3);
            SetTabStyle(modsTabButton, 4);
        }

        private void SetTabStyle(Button btn, int tabIndex)
        {
            if (btn == null) return;
            btn.BackgroundTexture = (activeTab == tabIndex) ? tabActiveTex : tabInactiveTex;
            if (btn.Components.Count > 0 && btn.Components[0] is TextLabel label)
            {
                label.TextColor = (activeTab == tabIndex) ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextMuted;
            }
        }

        #endregion

        #region Tab 0: Join / Direct Connect

        private void SetupJoinTab()
        {
            float y = 6f;

            AddFormLabel("SERVER ADDRESS (HOST IP):", new Vector2(10, y));
            AddFormLabel("UDP PORT:", new Vector2(188, y));
            y += 10f;
            joinIpTextBox = AddFormInput(new Vector2(10, y), 170f, "127.0.0.1", 32);
            joinPortTextBox = AddFormInput(new Vector2(188, y), 90f, "7777", 6);
            y += 18f;

            AddFormLabel("PLAYER ALIAS IN LOBBY:", new Vector2(10, y));
            y += 10f;
            string defaultName = GetDefaultPlayerName();
            joinNameTextBox = AddFormInput(new Vector2(10, y), 268f, defaultName, 24);
            y += 18f;

            AddFormLabel("ROLE UPON ENTERING LOBBY:", new Vector2(10, y));
            y += 10f;
            joinRoleButton = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(268f, 14f), contentPanel);
            joinRoleButton.BackgroundTexture = joinAsDm ? goldBtnTex : ironBtnTex;
            TextLabel roleLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 3), joinRoleButton);
            roleLabel.Text = joinAsDm ? "ROLE: [ DUNGEON MASTER (DM) ]" : "ROLE: [ ADVENTURER (PLAYER) ]";
            roleLabel.TextColor = joinAsDm ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            roleLabel.HorizontalAlignment = HorizontalAlignment.Center;

            joinRoleButton.OnMouseClick += (s, e) =>
            {
                joinAsDm = !joinAsDm;
                joinRoleButton.BackgroundTexture = joinAsDm ? goldBtnTex : ironBtnTex;
                roleLabel.Text = joinAsDm ? "ROLE: [ DUNGEON MASTER (DM) ]" : "ROLE: [ ADVENTURER (PLAYER) ]";
                roleLabel.TextColor = joinAsDm ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            };
            y += 20f;

            Button connectButton = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(268f, 17f), contentPanel);
            connectButton.BackgroundTexture = emeraldBtnTex;
            TextLabel connectLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), connectButton);
            connectLabel.Text = "CONNECT & ENTER LOBBY";
            connectLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            connectLabel.HorizontalAlignment = HorizontalAlignment.Center;
            connectButton.OnMouseClick += (s, e) => ExecuteJoinSession();
            y += 22f;

            joinStatusLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(10, y), "Status: Ready to connect to Host Lobby.", contentPanel);
            joinStatusLabel.TextColor = DarkFantasyPixelArtTheme.TextMuted;
        }

        private void ExecuteJoinSession()
        {
            string ip = joinIpTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

            ushort.TryParse(joinPortTextBox.Text.Trim(), out ushort port);
            if (port == 0) port = 7777;

            string playerName = joinNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(playerName))
            {
                PlayerPrefs.SetString("DFU_LastPlayerName", playerName);
                if (GameManager.Instance.PlayerEntity != null)
                    GameManager.Instance.PlayerEntity.Name = playerName;
            }

            SaveRecentServer($"{ip}:{port}");

            if (DFUNetworkManager.Instance != null)
            {
                var netMgr = DFUNetworkManager.Instance;
                netMgr.localLobbyIsDm = joinAsDm;
                netMgr.localLobbyIsReady = false;
                if (string.IsNullOrEmpty(netMgr.localLobbyCharacterName))
                    netMgr.localLobbyCharacterName = playerName;

                netMgr.ConnectToServer(ip, port);

                // Transition immediately to the Pre-Game Lobby room instead of closing to the main menu
                showingSavePickerModal = false;
                SwitchTab(2);
            }
            else
            {
                joinStatusLabel.Text = "Error: DFUNetworkManager not initialized.";
                joinStatusLabel.TextColor = DarkFantasyPixelArtTheme.TextErrorRed;
            }
        }

        #endregion

        #region Tab 1: Host Server

        private void SetupHostTab()
        {
            float y = 6f;

            AddFormLabel("LOCAL KCP UDP PORT:", new Vector2(10, y));
            y += 10f;
            hostPortTextBox = AddFormInput(new Vector2(10, y), 268f, "7777", 6);
            y += 18f;

            AddFormLabel("HOST / AUTHORITY ROLE:", new Vector2(10, y));
            y += 10f;
            hostRoleButton = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(268f, 14f), contentPanel);
            hostRoleButton.BackgroundTexture = hostAsDm ? goldBtnTex : ironBtnTex;
            TextLabel hostRoleLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 3), hostRoleButton);
            hostRoleLabel.Text = hostAsDm ? "HOST ROLE: [ DUNGEON MASTER (DM) ]" : "HOST ROLE: [ ADVENTURER (PLAYER) ]";
            hostRoleLabel.TextColor = hostAsDm ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            hostRoleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            hostRoleButton.OnMouseClick += (s, e) =>
            {
                hostAsDm = !hostAsDm;
                hostRoleButton.BackgroundTexture = hostAsDm ? goldBtnTex : ironBtnTex;
                hostRoleLabel.Text = hostAsDm ? "HOST ROLE: [ DUNGEON MASTER (DM) ]" : "HOST ROLE: [ ADVENTURER (PLAYER) ]";
                hostRoleLabel.TextColor = hostAsDm ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            };
            y += 18f;

            AddFormLabel("COMBAT RULES (FRIENDLY FIRE / PVP):", new Vector2(10, y));
            y += 10f;
            hostPvpButton = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(268f, 14f), contentPanel);
            hostPvpButton.BackgroundTexture = hostPvpEnabled ? crimsonBtnTex : emeraldBtnTex;
            TextLabel pvpLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 3), hostPvpButton);
            pvpLabel.Text = hostPvpEnabled ? "FRIENDLY FIRE: ENABLED (PVP ON)" : "FRIENDLY FIRE: DISABLED (CO-OP SAFE)";
            pvpLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            pvpLabel.HorizontalAlignment = HorizontalAlignment.Center;
            hostPvpButton.OnMouseClick += (s, e) =>
            {
                hostPvpEnabled = !hostPvpEnabled;
                hostPvpButton.BackgroundTexture = hostPvpEnabled ? crimsonBtnTex : emeraldBtnTex;
                pvpLabel.Text = hostPvpEnabled ? "FRIENDLY FIRE: ENABLED (PVP ON)" : "FRIENDLY FIRE: DISABLED (CO-OP SAFE)";
            };
            y += 20f;

            Button startHostButton = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(268f, 17f), contentPanel);
            startHostButton.BackgroundTexture = emeraldBtnTex;
            TextLabel startHostLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), startHostButton);
            startHostLabel.Text = "CREATE SERVER & OPEN LOBBY";
            startHostLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            startHostLabel.HorizontalAlignment = HorizontalAlignment.Center;
            startHostButton.OnMouseClick += (s, e) => ExecuteHostSession();
            y += 22f;

            hostStatusLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(10, y), "Creates an authoritative Host Lobby and waits for the party.", contentPanel);
            hostStatusLabel.TextColor = DarkFantasyPixelArtTheme.TextMuted;
        }

        private void ExecuteHostSession()
        {
            ushort.TryParse(hostPortTextBox.Text.Trim(), out ushort port);
            if (port == 0) port = 7777;

            if (DFUNetworkManager.Instance != null)
            {
                var netMgr = DFUNetworkManager.Instance;
                netMgr.localLobbyIsDm = hostAsDm;
                netMgr.localLobbyIsReady = false;
                netMgr.SetPvpAllowed(hostPvpEnabled);
                netMgr.StartHosting(port);
                netMgr.SendLocalLobbyUpdate();

                // Transition directly to the Pre-Game Lobby room instead of closing to the main menu
                showingSavePickerModal = false;
                SwitchTab(2);
            }
            else
            {
                hostStatusLabel.Text = "Error: DFUNetworkManager unavailable.";
                hostStatusLabel.TextColor = DarkFantasyPixelArtTheme.TextErrorRed;
            }
        }

        #endregion

        #region Tab 2: Pre-Game Lobby, Character Selection & Mod Compatibility

        private void SetupLobbyTab()
        {
            if (!NetworkSession.IsActive || DFUNetworkManager.Instance == null)
            {
                AddFormLabel("NO ACTIVE SESSION YET", new Vector2(10, 14));
                TextLabel hint = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(10, 28),
                    "Use the JOIN or HOST tab first to open a Lobby session.", contentPanel);
                hint.TextColor = DarkFantasyPixelArtTheme.TextMuted;

                Button goHostBtn = DaggerfallUI.AddButton(new Vector2(10, 48), new Vector2(128, 16), contentPanel);
                goHostBtn.BackgroundTexture = goldBtnTex;
                TextLabel ghLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), goHostBtn);
                ghLabel.Text = "GO TO HOST TAB";
                ghLabel.TextColor = DarkFantasyPixelArtTheme.GoldBright;
                ghLabel.HorizontalAlignment = HorizontalAlignment.Center;
                goHostBtn.OnMouseClick += (s, e) => SwitchTab(1);

                Button goJoinBtn = DaggerfallUI.AddButton(new Vector2(148, 48), new Vector2(128, 16), contentPanel);
                goJoinBtn.BackgroundTexture = emeraldBtnTex;
                TextLabel gjLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), goJoinBtn);
                gjLabel.Text = "GO TO JOIN TAB";
                gjLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
                gjLabel.HorizontalAlignment = HorizontalAlignment.Center;
                goJoinBtn.OnMouseClick += (s, e) => SwitchTab(0);
                return;
            }

            if (showingSavePickerModal)
            {
                SetupSavedCharacterPickerView();
                return;
            }

            var netMgr = DFUNetworkManager.Instance;

            // Top Session Status Banner
            string connBanner = NetworkSession.IsHost
                ? $"SESSION: HOSTING ON UDP PORT {netMgr.defaultPort}  |  ROLE: {(netMgr.localLobbyIsDm ? "DM" : "PLAYER")}"
                : (Mirror.NetworkClient.isConnected
                    ? $"SESSION: CONNECTED TO {netMgr.networkAddress} (ID #{netMgr.localConnectionId})"
                    : $"SESSION: CONNECTING TO {netMgr.networkAddress}...");

            TextLabel bannerLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(8, 4), contentPanel);
            bannerLabel.Text = connBanner;
            bannerLabel.TextColor = Mirror.NetworkClient.isConnected || NetworkSession.IsHost
                ? DarkFantasyPixelArtTheme.TextReadyGreen
                : DarkFantasyPixelArtTheme.TextWarnAmber;

            Button disconnectBtn = DaggerfallUI.AddButton(new Vector2(218, 2), new Vector2(64, 11), contentPanel);
            disconnectBtn.BackgroundTexture = crimsonBtnTex;
            TextLabel discLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 2), disconnectBtn);
            discLabel.Text = "DISCONNECT";
            discLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            discLabel.HorizontalAlignment = HorizontalAlignment.Center;
            disconnectBtn.OnMouseClick += (s, e) =>
            {
                netMgr.DisconnectSession();
                SwitchTab(0);
            };

            // Left Column: Party Roster & Mod Compatibility Status
            AddFormLabel("PARTY ROSTER & MOD SYNC:", new Vector2(8, 16));

            Panel rosterBox = new Panel();
            rosterBox.Position = new Vector2(8, 25);
            rosterBox.Size = new Vector2(166, 86);
            rosterBox.Outline.Enabled = true;
            rosterBox.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            rosterBox.BackgroundColor = new Color32(10, 9, 12, 235);
            contentPanel.Components.Add(rosterBox);

            List<LobbyPlayerDTO> players = (netMgr.currentLobbyRoster != null && netMgr.currentLobbyRoster.players != null)
                ? netMgr.currentLobbyRoster.players
                : new List<LobbyPlayerDTO>();

            float rowY = 4f;
            if (players.Count == 0)
            {
                TextLabel waitLbl = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, rowY), "Waiting for handshake...", rosterBox);
                waitLbl.TextColor = DarkFantasyPixelArtTheme.TextMuted;
            }
            else
            {
                foreach (var p in players)
                {
                    string roleTag = p.isHost ? (p.isDm ? "[HOST/DM]" : "[HOST]") : (p.isDm ? "[DM]" : "[PLY]");
                    string charSummary = p.selectionMode == 1
                        ? $"{p.characterName} (New: {p.raceName} {p.className})"
                        : (p.selectionMode == 2
                            ? $"{p.characterName} ({p.raceName} {p.className} L{p.level})"
                            : "No character selected");

                    string readyTag = p.isReady ? "[READY]" : "[WAIT]";
                    TextLabel line1 = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(4, rowY),
                        $"{roleTag} {charSummary}", rosterBox);
                    line1.TextColor = p.isReady ? DarkFantasyPixelArtTheme.TextReadyGreen : DarkFantasyPixelArtTheme.TextIvory;
                    line1.TextScale = 0.85f;
                    rowY += 7f;

                    string modTag = p.modsCompatible ? $"Mods: OK  |  Estado: {readyTag}" : $"Mods: MISMATCH ({p.modMismatchSummary})";
                    TextLabel line2 = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(8, rowY), modTag, rosterBox);
                    line2.TextColor = p.modsCompatible
                        ? (p.isReady ? DarkFantasyPixelArtTheme.TextGold : DarkFantasyPixelArtTheme.TextMuted)
                        : DarkFantasyPixelArtTheme.TextErrorRed;
                    line2.TextScale = 0.80f;
                    rowY += 10f;
                }
            }

            // Right Column: Parchment Character Card (Ventanas.png inspired)
            AddFormLabel("YOUR CHARACTER:", new Vector2(180, 16));

            Panel charCard = new Panel();
            charCard.Position = new Vector2(180, 25);
            charCard.Size = new Vector2(102, 54);
            charCard.BackgroundTexture = parchmentCardTex;
            contentPanel.Components.Add(charCard);

            string modeTitle = netMgr.localLobbySelectionMode == 1
                ? "NEW CHARACTER"
                : (netMgr.localLobbySelectionMode == 2 ? "SAVED CHARACTER" : "UNSELECTED");

            TextLabel cardHeader = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 5), modeTitle, charCard);
            cardHeader.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            cardHeader.ShadowPosition = Vector2.zero;
            cardHeader.TextScale = 0.82f;

            string cName = !string.IsNullOrEmpty(netMgr.localLobbyCharacterName) ? netMgr.localLobbyCharacterName : "None";
            string cRace = !string.IsNullOrEmpty(netMgr.localLobbyRaceName) ? netMgr.localLobbyRaceName : "-";
            string cClass = !string.IsNullOrEmpty(netMgr.localLobbyClassName) ? netMgr.localLobbyClassName : "-";

            TextLabel cNameLbl = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 15), $"Name: {cName}", charCard);
            cNameLbl.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            cNameLbl.ShadowPosition = Vector2.zero;
            cNameLbl.TextScale = 0.85f;

            TextLabel cRaceLbl = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 25), $"Race: {cRace}", charCard);
            cRaceLbl.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            cRaceLbl.ShadowPosition = Vector2.zero;
            cRaceLbl.TextScale = 0.85f;

            TextLabel cClassLbl = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 35), $"Class: {cClass}", charCard);
            cClassLbl.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            cClassLbl.ShadowPosition = Vector2.zero;
            cClassLbl.TextScale = 0.85f;

            TextLabel cLvlLbl = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 44), $"Level: {netMgr.localLobbyLevel}", charCard);
            cLvlLbl.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            cLvlLbl.ShadowPosition = Vector2.zero;
            cLvlLbl.TextScale = 0.82f;

            // Character Mode Radio Selection: [ (•) NEW CHARACTER ] vs [ ( ) LOAD CHARACTER ]
            bool isNewChar = netMgr.localLobbySelectionMode == 1;
            bool isLoadChar = netMgr.localLobbySelectionMode == 2;

            Button newCharBtn = DaggerfallUI.AddButton(new Vector2(180, 81), new Vector2(102, 14), contentPanel);
            newCharBtn.BackgroundTexture = isNewChar ? goldBtnTex : ironBtnTex;

            Panel radio1Dot = new Panel();
            radio1Dot.Position = new Vector2(4, 2);
            radio1Dot.Size = new Vector2(10, 10);
            radio1Dot.BackgroundTexture = isNewChar ? DarkFantasyPixelArtTheme.RadioButtonCheckedTex : DarkFantasyPixelArtTheme.RadioButtonUncheckedTex;
            newCharBtn.Components.Add(radio1Dot);

            TextLabel ncLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(18, 3), newCharBtn);
            ncLabel.Text = "NEW CHARACTER";
            ncLabel.TextColor = isNewChar ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            newCharBtn.OnMouseClick += (s, e) => LaunchNewCharacterWizardForLobby();

            Button loadCharBtn = DaggerfallUI.AddButton(new Vector2(180, 97), new Vector2(102, 14), contentPanel);
            loadCharBtn.BackgroundTexture = isLoadChar ? goldBtnTex : ironBtnTex;

            Panel radio2Dot = new Panel();
            radio2Dot.Position = new Vector2(4, 2);
            radio2Dot.Size = new Vector2(10, 10);
            radio2Dot.BackgroundTexture = isLoadChar ? DarkFantasyPixelArtTheme.RadioButtonCheckedTex : DarkFantasyPixelArtTheme.RadioButtonUncheckedTex;
            loadCharBtn.Components.Add(radio2Dot);

            TextLabel lcLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(18, 3), loadCharBtn);
            lcLabel.Text = "LOAD CHARACTER";
            lcLabel.TextColor = isLoadChar ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextIvory;
            loadCharBtn.OnMouseClick += (s, e) =>
            {
                showingSavePickerModal = true;
                SwitchTab(2);
            };

            // Bottom Action Bar: Ready Toggle & DM Launch Button
            bool hasSelectedChar = netMgr.localLobbySelectionMode != 0;
            Button readyBtn = DaggerfallUI.AddButton(new Vector2(8, 116), new Vector2(132, 18), contentPanel);
            readyBtn.BackgroundTexture = !hasSelectedChar
                ? ironBtnTex
                : (netMgr.localLobbyIsReady ? emeraldBtnTex : goldBtnTex);

            TextLabel readyLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 5), readyBtn);
            readyLbl.Text = !hasSelectedChar
                ? "SELECT CHAR FIRST"
                : (netMgr.localLobbyIsReady ? "STATUS: READY [OK]" : "MARK AS READY");
            readyLbl.TextColor = !hasSelectedChar ? DarkFantasyPixelArtTheme.TextMuted : DarkFantasyPixelArtTheme.TextIvory;
            readyLbl.HorizontalAlignment = HorizontalAlignment.Center;

            readyBtn.OnMouseClick += (s, e) =>
            {
                if (netMgr.localLobbySelectionMode == 0)
                    return;
                netMgr.localLobbyIsReady = !netMgr.localLobbyIsReady;
                netMgr.SendLocalLobbyUpdate();
                SwitchTab(2);
            };

            // Host / DM Launch Button (Enabled only when ALL party members in Lobby are Ready)
            bool allReady = netMgr.AreAllLobbyPlayersReady(out int domMode);
            Button launchBtn = DaggerfallUI.AddButton(new Vector2(146, 116), new Vector2(136, 18), contentPanel);

            if (NetworkSession.IsHost)
            {
                launchBtn.BackgroundTexture = allReady ? emeraldBtnTex : ironBtnTex;
                string launchCaption = !allReady
                    ? "WAITING FOR PARTY..."
                    : (netMgr.localLobbySelectionMode == 1 ? "START NEW GAME (DM)" : "LOAD PARTY GAME (DM)");

                TextLabel launchLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 5), launchBtn);
                launchLbl.Text = launchCaption;
                launchLbl.TextColor = allReady ? DarkFantasyPixelArtTheme.GoldBright : DarkFantasyPixelArtTheme.TextMuted;
                launchLbl.HorizontalAlignment = HorizontalAlignment.Center;

                launchBtn.OnMouseClick += (s, e) =>
                {
                    if (netMgr.AreAllLobbyPlayersReady(out _))
                    {
                        netMgr.HostLaunchLobbySession();
                    }
                };
            }
            else
            {
                launchBtn.BackgroundTexture = ironBtnTex;
                TextLabel waitHostLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 5), launchBtn);
                waitHostLbl.Text = allReady ? "WAITING FOR DM LAUNCH" : "WAITING FOR PARTY...";
                waitHostLbl.TextColor = allReady ? DarkFantasyPixelArtTheme.TextReadyGreen : DarkFantasyPixelArtTheme.TextMuted;
                waitHostLbl.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        private void LaunchNewCharacterWizardForLobby()
        {
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.isCreatingCharacterForLobby = true;
                DFUNetworkManager.Instance.localLobbyIsReady = false;
                DFUNetworkManager.Instance.SendLocalLobbyUpdate();
            }

            uiManager.PushWindow(UIWindowFactory.GetInstance(UIWindowType.StartNewGameWizard, uiManager));
        }

        /// <summary>
        /// Sub-view inside the Lobby to pick a saved character and inspect Name, Race, Class, and Level before confirming.
        /// </summary>
        private void SetupSavedCharacterPickerView()
        {
            AddFormLabel("SELECT SAVED CHARACTER FOR LOBBY:", new Vector2(8, 4));

            GameManager.Instance.SaveLoadManager.EnumerateSaves();
            enumeratedCharNames.Clear();
            enumeratedCharNames.AddRange(GameManager.Instance.SaveLoadManager.GetCharacterNames());

            // Left Panel: Character Names
            Panel charListPanel = new Panel();
            charListPanel.Position = new Vector2(8, 14);
            charListPanel.Size = new Vector2(86, 96);
            charListPanel.Outline.Enabled = true;
            charListPanel.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            charListPanel.BackgroundColor = new Color32(10, 9, 12, 240);
            contentPanel.Components.Add(charListPanel);

            characterNamesListBox = new ListBox();
            characterNamesListBox.Position = new Vector2(2, 2);
            characterNamesListBox.Size = new Vector2(82, 92);
            characterNamesListBox.RowsDisplayed = 10;
            characterNamesListBox.TextColor = DarkFantasyPixelArtTheme.TextGold;
            charListPanel.Components.Add(characterNamesListBox);

            foreach (string cName in enumeratedCharNames)
            {
                characterNamesListBox.AddItem(cName);
            }

            // Middle Panel: Saves for Selected Character
            Panel saveListPanel = new Panel();
            saveListPanel.Position = new Vector2(98, 14);
            saveListPanel.Size = new Vector2(84, 96);
            saveListPanel.Outline.Enabled = true;
            saveListPanel.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            saveListPanel.BackgroundColor = new Color32(10, 9, 12, 240);
            contentPanel.Components.Add(saveListPanel);

            characterSavesListBox = new ListBox();
            characterSavesListBox.Position = new Vector2(2, 2);
            characterSavesListBox.Size = new Vector2(80, 92);
            characterSavesListBox.RowsDisplayed = 10;
            characterSavesListBox.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            saveListPanel.Components.Add(characterSavesListBox);

            // Right Panel: Parchment Sheet Inspecting Name, Race, Class & Level
            Panel inspectCard = new Panel();
            inspectCard.Position = new Vector2(186, 14);
            inspectCard.Size = new Vector2(96, 96);
            inspectCard.BackgroundTexture = parchmentCardTex;
            contentPanel.Components.Add(inspectCard);

            TextLabel sheetTitle = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 6), "CHARACTER SHEET", inspectCard);
            sheetTitle.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            sheetTitle.ShadowPosition = Vector2.zero;
            sheetTitle.TextScale = 0.85f;

            previewNameLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 20), "Name: -", inspectCard);
            previewNameLabel.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            previewNameLabel.ShadowPosition = Vector2.zero;
            previewNameLabel.TextScale = 0.85f;

            previewRaceLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 33), "Race: -", inspectCard);
            previewRaceLabel.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            previewRaceLabel.ShadowPosition = Vector2.zero;
            previewRaceLabel.TextScale = 0.85f;

            previewClassLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 46), "Class: -", inspectCard);
            previewClassLabel.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            previewClassLabel.ShadowPosition = Vector2.zero;
            previewClassLabel.TextScale = 0.85f;

            previewLevelLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 59), "Level: -", inspectCard);
            previewLevelLabel.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            previewLevelLabel.ShadowPosition = Vector2.zero;
            previewLevelLabel.TextScale = 0.85f;

            previewSaveNameLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(6, 74), "Save: -", inspectCard);
            previewSaveNameLabel.TextColor = DarkFantasyPixelArtTheme.ParchmentInk;
            previewSaveNameLabel.ShadowPosition = Vector2.zero;
            previewSaveNameLabel.TextScale = 0.78f;

            characterNamesListBox.OnSelectItem += () =>
            {
                PopulateCharacterSavesList();
            };

            characterSavesListBox.OnSelectItem += () =>
            {
                InspectSelectedSaveSlot();
            };

            if (enumeratedCharNames.Count > 0)
            {
                characterNamesListBox.SelectIndex(0);
                PopulateCharacterSavesList();
            }

            // Bottom Confirm / Cancel Buttons
            Button confirmCharBtn = DaggerfallUI.AddButton(new Vector2(8, 116), new Vector2(168, 18), contentPanel);
            confirmCharBtn.BackgroundTexture = emeraldBtnTex;
            TextLabel confLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 5), confirmCharBtn);
            confLbl.Text = "SELECT THIS CHARACTER FOR LOBBY";
            confLbl.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            confLbl.HorizontalAlignment = HorizontalAlignment.Center;
            confirmCharBtn.OnMouseClick += (s, e) =>
            {
                if (selectedSaveKeyPreview >= 0 && DFUNetworkManager.Instance != null)
                {
                    var netMgr = DFUNetworkManager.Instance;
                    netMgr.localLobbySelectionMode = 2; // Saved Character
                    netMgr.localLobbySaveKey = selectedSaveKeyPreview;
                    netMgr.localLobbyCharacterName = previewCharName;
                    netMgr.localLobbyRaceName = previewRaceName;
                    netMgr.localLobbyClassName = previewClassName;
                    netMgr.localLobbyLevel = previewCharLevel;
                    netMgr.localLobbyIsReady = true; // Automatically mark ready upon picking saved character
                    netMgr.SendLocalLobbyUpdate();
                }
                showingSavePickerModal = false;
                SwitchTab(2);
            };

            Button cancelPickerBtn = DaggerfallUI.AddButton(new Vector2(182, 116), new Vector2(100, 18), contentPanel);
            cancelPickerBtn.BackgroundTexture = crimsonBtnTex;
            TextLabel cancLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 5), cancelPickerBtn);
            cancLbl.Text = "BACK TO LOBBY";
            cancLbl.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            cancLbl.HorizontalAlignment = HorizontalAlignment.Center;
            cancelPickerBtn.OnMouseClick += (s, e) =>
            {
                showingSavePickerModal = false;
                SwitchTab(2);
            };
        }

        private void PopulateCharacterSavesList()
        {
            characterSavesListBox.ClearItems();
            enumeratedCharSaveKeys.Clear();
            selectedSaveKeyPreview = -1;

            if (characterNamesListBox.SelectedIndex < 0 || characterNamesListBox.SelectedIndex >= enumeratedCharNames.Count)
                return;

            string charName = enumeratedCharNames[characterNamesListBox.SelectedIndex];
            int[] keys = GameManager.Instance.SaveLoadManager.GetCharacterSaveKeys(charName);

            List<KeyValuePair<int, SaveInfo_v1>> ordered = new List<KeyValuePair<int, SaveInfo_v1>>();
            foreach (int k in keys)
            {
                SaveInfo_v1 info = GameManager.Instance.SaveLoadManager.GetSaveInfo(k);
                ordered.Add(new KeyValuePair<int, SaveInfo_v1>(k, info));
            }

            ordered = ordered.OrderByDescending(kv => kv.Value.dateAndTime != null ? kv.Value.dateAndTime.realTime : 0).ToList();

            foreach (var kv in ordered)
            {
                enumeratedCharSaveKeys.Add(kv.Key);
                characterSavesListBox.AddItem(string.IsNullOrEmpty(kv.Value.saveName) ? $"Save #{kv.Key}" : kv.Value.saveName);
            }

            if (enumeratedCharSaveKeys.Count > 0)
            {
                characterSavesListBox.SelectIndex(0);
                InspectSelectedSaveSlot();
            }
        }

        private void InspectSelectedSaveSlot()
        {
            if (characterSavesListBox.SelectedIndex < 0 || characterSavesListBox.SelectedIndex >= enumeratedCharSaveKeys.Count)
                return;

            int key = enumeratedCharSaveKeys[characterSavesListBox.SelectedIndex];
            selectedSaveKeyPreview = key;

            SaveInfo_v1 info = GameManager.Instance.SaveLoadManager.GetSaveInfo(key);
            previewCharName = !string.IsNullOrEmpty(info.characterName) ? info.characterName : "Adventurer";
            previewSaveCaption = !string.IsNullOrEmpty(info.saveName) ? info.saveName : $"SAVE{key}";
            previewRaceName = "Breton";
            previewClassName = "Adventurer";
            previewCharLevel = 1;

            // Read SaveData.txt from the save folder to extract exact Race, Class, and Level
            try
            {
                string folder = GameManager.Instance.SaveLoadManager.GetSaveFolder(key);
                string saveDataPath = Path.Combine(folder, "SaveData.txt");
                if (File.Exists(saveDataPath))
                {
                    string json = File.ReadAllText(saveDataPath);
                    SaveData_v1 saveData = SaveLoadManager.Deserialize(typeof(SaveData_v1), json) as SaveData_v1;
                    if (saveData != null && saveData.playerData != null && saveData.playerData.playerEntity != null)
                    {
                        var pe = saveData.playerData.playerEntity;
                        if (!string.IsNullOrEmpty(pe.name))
                            previewCharName = pe.name;
                        if (pe.raceTemplate != null && !string.IsNullOrEmpty(pe.raceTemplate.Name))
                            previewRaceName = pe.raceTemplate.Name;
                        if (pe.careerTemplate != null && !string.IsNullOrEmpty(pe.careerTemplate.Name))
                            previewClassName = pe.careerTemplate.Name;
                        if (pe.level > 0)
                            previewCharLevel = pe.level;
                    }
                }
            }
            catch (Exception ex)
            {
                DaggerfallUnity.LogMessage($"[DFU Lobby] Failed to inspect SaveData.txt for key {key}: {ex.Message}");
            }

            if (previewNameLabel != null) previewNameLabel.Text = $"Name: {previewCharName}";
            if (previewRaceLabel != null) previewRaceLabel.Text = $"Race: {previewRaceName}";
            if (previewClassLabel != null) previewClassLabel.Text = $"Class: {previewClassName}";
            if (previewLevelLabel != null) previewLevelLabel.Text = $"Level: {previewCharLevel}";
            if (previewSaveNameLabel != null) previewSaveNameLabel.Text = $"Save: {previewSaveCaption}";
        }

        #endregion

        #region Tab 3: History (Recent Servers)

        private void SetupHistoryTab()
        {
            float y = 6f;
            AddFormLabel("RECENT & FAVORITE SERVERS:", new Vector2(10, y));
            y += 10f;

            Panel listPanel = new Panel();
            listPanel.Position = new Vector2(10, y);
            listPanel.Size = new Vector2(268, 86);
            listPanel.Outline.Enabled = true;
            listPanel.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            listPanel.BackgroundColor = new Color32(10, 9, 12, 240);
            contentPanel.Components.Add(listPanel);

            historyListBox = new ListBox();
            historyListBox.Position = new Vector2(2, 2);
            historyListBox.Size = new Vector2(264, 82);
            historyListBox.RowsDisplayed = 8;
            historyListBox.TextColor = DarkFantasyPixelArtTheme.TextGold;
            listPanel.Components.Add(historyListBox);

            foreach (string s in recentServers)
            {
                historyListBox.AddItem(s);
            }
            if (recentServers.Count > 0)
                historyListBox.SelectedIndex = 0;

            y += 92f;

            Button quickConnectBtn = DaggerfallUI.AddButton(new Vector2(10, y), new Vector2(130, 16), contentPanel);
            quickConnectBtn.BackgroundTexture = emeraldBtnTex;
            TextLabel qcLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), quickConnectBtn);
            qcLabel.Text = "SELECT SERVER";
            qcLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            qcLabel.HorizontalAlignment = HorizontalAlignment.Center;
            quickConnectBtn.OnMouseClick += (s, e) =>
            {
                if (historyListBox.SelectedIndex >= 0 && historyListBox.SelectedIndex < recentServers.Count)
                {
                    string entry = recentServers[historyListBox.SelectedIndex];
                    string[] parts = entry.Split(':');
                    SwitchTab(0);
                    if (parts.Length > 0 && joinIpTextBox != null)
                        joinIpTextBox.Text = parts[0];
                    if (parts.Length > 1 && joinPortTextBox != null)
                        joinPortTextBox.Text = parts[1];
                }
            };

            Button deleteBtn = DaggerfallUI.AddButton(new Vector2(148, y), new Vector2(130, 16), contentPanel);
            deleteBtn.BackgroundTexture = crimsonBtnTex;
            TextLabel delLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 4), deleteBtn);
            delLabel.Text = "REMOVE ENTRY";
            delLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            delLabel.HorizontalAlignment = HorizontalAlignment.Center;
            deleteBtn.OnMouseClick += (s, e) =>
            {
                if (historyListBox.SelectedIndex >= 0 && historyListBox.SelectedIndex < recentServers.Count)
                {
                    recentServers.RemoveAt(historyListBox.SelectedIndex);
                    SaveRecentServersList();
                    SwitchTab(3);
                }
            };
        }

        private void LoadRecentServers()
        {
            recentServers.Clear();
            string saved = PlayerPrefs.GetString("DFU_RecentServers", "127.0.0.1:7777");
            string[] items = saved.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string itm in items)
            {
                if (!string.IsNullOrEmpty(itm) && !recentServers.Contains(itm))
                    recentServers.Add(itm);
            }
        }

        private void SaveRecentServer(string serverEndpoint)
        {
            if (string.IsNullOrEmpty(serverEndpoint)) return;
            recentServers.Remove(serverEndpoint);
            recentServers.Insert(0, serverEndpoint);
            if (recentServers.Count > 10)
                recentServers.RemoveAt(recentServers.Count - 1);

            SaveRecentServersList();
        }

        private void SaveRecentServersList()
        {
            PlayerPrefs.SetString("DFU_RecentServers", string.Join(";", recentServers.ToArray()));
            PlayerPrefs.Save();
        }

        #endregion

        #region Tab 4: Mods & Compatibility Inspector

        private void SetupModsTab()
        {
            float y = 6f;
            AddFormLabel("ACTIVE LOCAL & HOST MODS (.DFMOD):", new Vector2(10, y));
            y += 10f;

            Panel infoBox = new Panel();
            infoBox.Position = new Vector2(10, y);
            infoBox.Size = new Vector2(268, 114);
            infoBox.Outline.Enabled = true;
            infoBox.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            infoBox.BackgroundColor = new Color32(10, 9, 12, 240);
            contentPanel.Components.Add(infoBox);

            ListBox modsListBox = new ListBox();
            modsListBox.Position = new Vector2(4, 4);
            modsListBox.Size = new Vector2(260, 106);
            modsListBox.RowsDisplayed = 11;
            modsListBox.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            infoBox.Components.Add(modsListBox);

            string[] localMods = DFUNetworkManager.Instance != null
                ? DFUNetworkManager.Instance.GetLocalActiveModList()
                : new string[0];

            modsListBox.AddItem($"--- Local Active Mods ({localMods.Length}) ---");
            if (localMods.Length == 0)
            {
                modsListBox.AddItem("  (Vanilla DFU - No active .dfmod bundles)");
            }
            else
            {
                foreach (string m in localMods)
                    modsListBox.AddItem($"  * {m}");
            }

            if (NetworkSession.IsActive && DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.currentLobbyRoster != null)
            {
                string[] hostMods = DFUNetworkManager.Instance.currentLobbyRoster.hostRequiredMods ?? new string[0];
                modsListBox.AddItem($"--- Host Required Mods ({hostMods.Length}) ---");
                if (hostMods.Length == 0)
                {
                    modsListBox.AddItem("  (Host is running Vanilla DFU)");
                }
                else
                {
                    foreach (string hm in hostMods)
                        modsListBox.AddItem($"  [HOST] {hm}");
                }
            }
        }

        #endregion

        #region Helpers

        private void AddFormLabel(string text, Vector2 pos)
        {
            TextLabel label = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, pos, text, contentPanel);
            label.TextColor = DarkFantasyPixelArtTheme.TextGold;
            label.ShadowPosition = Vector2.one;
            label.ShadowColor = Color.black;
        }

        private TextBox AddFormInput(Vector2 pos, float width, string defaultText, int maxChars)
        {
            Panel inputPanel = new Panel();
            inputPanel.Position = pos;
            inputPanel.Size = new Vector2(width, 12f);
            inputPanel.Outline.Enabled = true;
            inputPanel.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            inputPanel.BackgroundColor = new Color32(10, 9, 12, 245);
            contentPanel.Components.Add(inputPanel);

            TextBox textBox = new TextBox();
            textBox.Position = new Vector2(2, 2);
            textBox.Size = new Vector2(width - 4, 8f);
            textBox.FixedSize = true;
            textBox.UseFocus = true;
            textBox.MaxCharacters = maxChars;
            textBox.Text = defaultText;
            textBox.DefaultText = defaultText;
            textBox.DefaultTextColor = DarkFantasyPixelArtTheme.TextMuted;
            inputPanel.Components.Add(textBox);

            return textBox;
        }

        private string GetDefaultPlayerName()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && !string.IsNullOrEmpty(GameManager.Instance.PlayerEntity.Name))
            {
                return GameManager.Instance.PlayerEntity.Name;
            }
            return PlayerPrefs.GetString("DFU_LastPlayerName", "Adventurer");
        }

        #endregion
    }
}
