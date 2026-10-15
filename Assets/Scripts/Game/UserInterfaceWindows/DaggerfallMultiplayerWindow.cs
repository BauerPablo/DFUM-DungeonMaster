// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using UnityEngine;
using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Networking;
using DaggerfallWorkshop.Game.Serialization;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Native Daggerfall Retro Window for launching, hosting, and connecting to Multiplayer / TTRPG sessions.
    /// Accessible directly from the main start menu and pause menu.
    /// </summary>
    public class DaggerfallMultiplayerWindow : DaggerfallPopupWindow
    {
        #region UI Rects & Constants

        private const float MainWidth = 280f;
        private const float MainHeight = 175f;

        private readonly Color mainBackgroundColor = new Color(0.12f, 0.10f, 0.08f, 0.95f);
        private readonly Color panelOutlineColor = new Color(0.70f, 0.55f, 0.22f, 0.90f);
        private readonly Color tabActiveColor = new Color(0.28f, 0.22f, 0.12f, 1f);
        private readonly Color tabInactiveColor = new Color(0.16f, 0.14f, 0.12f, 0.85f);
        private readonly Color goldTextColor = new Color(0.95f, 0.85f, 0.40f);
        private readonly Color buttonGreenColor = new Color(0.15f, 0.45f, 0.15f, 0.90f);
        private readonly Color buttonRedColor = new Color(0.50f, 0.15f, 0.15f, 0.90f);

        #endregion

        #region Panels & Controls

        private Panel mainPanel = new Panel();
        private Panel contentPanel = new Panel();

        private Button joinTabButton;
        private Button hostTabButton;
        private Button historyTabButton;
        private Button modsTabButton;

        private int activeTab = 0; // 0=Join, 1=Host, 2=History, 3=Mods

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

        // Tab 2: History
        private ListBox historyListBox;
        private readonly List<string> recentServers = new List<string>();

        // Tab 3: Mods
        private TextLabel modsSummaryLabel;

        #endregion

        #region Constructor

        public DaggerfallMultiplayerWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null)
            : base(uiManager, previousWindow)
        {
            AllowCancel = true;
        }

        #endregion

        #region Setup

        protected override void Setup()
        {
            base.Setup();

            ParentPanel.BackgroundColor = ScreenDimColor;

            // Main Background Panel
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.Size = new Vector2(MainWidth, MainHeight);
            mainPanel.Outline.Enabled = true;
            mainPanel.Outline.Color = panelOutlineColor;
            mainPanel.BackgroundColor = mainBackgroundColor;
            NativePanel.Components.Add(mainPanel);

            // Title Label
            TextLabel titleLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 4), mainPanel);
            titleLabel.Text = "DAGGERFALL MULTIPLAYER & TTRPG";
            titleLabel.TextColor = goldTextColor;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.ShadowPosition = Vector2.one;
            titleLabel.ShadowColor = Color.black;

            // Tab Buttons
            float tabY = 18f;
            float tabWidth = 66f;
            float tabHeight = 13f;

            joinTabButton = CreateTabButton(new Vector2(6, tabY), new Vector2(tabWidth, tabHeight), "UNIRSE", 0);
            hostTabButton = CreateTabButton(new Vector2(74, tabY), new Vector2(tabWidth, tabHeight), "CREAR (HOST)", 1);
            historyTabButton = CreateTabButton(new Vector2(142, tabY), new Vector2(tabWidth, tabHeight), "HISTORIAL", 2);
            modsTabButton = CreateTabButton(new Vector2(210, tabY), new Vector2(tabWidth, tabHeight), "MODS", 3);

            // Content Panel Area
            contentPanel.Position = new Vector2(6, 34);
            contentPanel.Size = new Vector2(MainWidth - 12, MainHeight - 52);
            contentPanel.Outline.Enabled = true;
            contentPanel.Outline.Color = new Color(0.4f, 0.35f, 0.25f, 0.7f);
            contentPanel.BackgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.9f);
            mainPanel.Components.Add(contentPanel);

            // Bottom Bar: Close Button
            Button closeButton = DaggerfallUI.AddButton(new Vector2(6, MainHeight - 15), new Vector2(MainWidth - 12, 12), mainPanel);
            closeButton.Outline.Enabled = true;
            closeButton.Outline.Color = panelOutlineColor;
            closeButton.BackgroundColor = buttonRedColor;
            TextLabel closeLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), closeButton);
            closeLabel.Text = "VOLVER / BACK (ESC)";
            closeLabel.TextColor = Color.white;
            closeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            closeButton.OnMouseClick += (s, e) => CloseWindow();

            LoadRecentServers();
            SwitchTab(0);
        }

        private Button CreateTabButton(Vector2 pos, Vector2 size, string text, int tabIndex)
        {
            Button btn = DaggerfallUI.AddButton(pos, size, mainPanel);
            btn.Outline.Enabled = true;
            btn.Outline.Color = panelOutlineColor;
            btn.BackgroundColor = (activeTab == tabIndex) ? tabActiveColor : tabInactiveColor;

            TextLabel label = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), btn);
            label.Text = text;
            label.TextColor = (activeTab == tabIndex) ? goldTextColor : new Color(0.7f, 0.7f, 0.7f);
            label.HorizontalAlignment = HorizontalAlignment.Center;

            btn.OnMouseClick += (s, e) => SwitchTab(tabIndex);
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
                    SetupHistoryTab();
                    break;
                case 3:
                    SetupModsTab();
                    break;
            }
        }

        private void UpdateTabButtonStyles()
        {
            SetTabStyle(joinTabButton, 0);
            SetTabStyle(hostTabButton, 1);
            SetTabStyle(historyTabButton, 2);
            SetTabStyle(modsTabButton, 3);
        }

        private void SetTabStyle(Button btn, int tabIndex)
        {
            if (btn == null) return;
            btn.BackgroundColor = (activeTab == tabIndex) ? tabActiveColor : tabInactiveColor;
            if (btn.Components.Count > 0 && btn.Components[0] is TextLabel label)
            {
                label.TextColor = (activeTab == tabIndex) ? goldTextColor : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        #endregion

        #region Tab 0: Join / Direct Connect

        private void SetupJoinTab()
        {
            float y = 4f;

            AddFormLabel("DIRECCIÓN IP DEL SERVIDOR (HOST IP):", new Vector2(8, y));
            y += 10f;
            joinIpTextBox = AddFormInput(new Vector2(8, y), 160f, "127.0.0.1", 32);

            AddFormLabel("PUERTO (PORT):", new Vector2(176, y - 10f));
            joinPortTextBox = AddFormInput(new Vector2(176, y), 80f, "7777", 6);
            y += 18f;

            AddFormLabel("NOMBRE EN PARTIDA:", new Vector2(8, y));
            y += 10f;
            string defaultName = GetDefaultPlayerName();
            joinNameTextBox = AddFormInput(new Vector2(8, y), 248f, defaultName, 24);
            y += 18f;

            AddFormLabel("ROL AL INGRESAR:", new Vector2(8, y));
            y += 10f;
            joinRoleButton = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(248f, 13f), contentPanel);
            joinRoleButton.Outline.Enabled = true;
            joinRoleButton.Outline.Color = panelOutlineColor;
            joinRoleButton.BackgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            TextLabel roleLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), joinRoleButton);
            roleLabel.Text = joinAsDm ? "ROL: [ DUNGEON MASTER (DM) ]" : "ROL: [ JUGADOR (PLAYER) ]";
            roleLabel.TextColor = joinAsDm ? goldTextColor : Color.white;
            roleLabel.HorizontalAlignment = HorizontalAlignment.Center;

            joinRoleButton.OnMouseClick += (s, e) =>
            {
                joinAsDm = !joinAsDm;
                roleLabel.Text = joinAsDm ? "ROL: [ DUNGEON MASTER (DM) ]" : "ROL: [ JUGADOR (PLAYER) ]";
                roleLabel.TextColor = joinAsDm ? goldTextColor : Color.white;
            };
            y += 18f;

            Button connectButton = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(248f, 16f), contentPanel);
            connectButton.Outline.Enabled = true;
            connectButton.Outline.Color = panelOutlineColor;
            connectButton.BackgroundColor = buttonGreenColor;
            TextLabel connectLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 2), connectButton);
            connectLabel.Text = "CONECTAR A LA PARTIDA";
            connectLabel.TextColor = Color.white;
            connectLabel.HorizontalAlignment = HorizontalAlignment.Center;
            connectButton.OnMouseClick += (s, e) => ExecuteJoinSession();
            y += 20f;

            joinStatusLabel = DaggerfallUI.AddTextLabel(new Vector2(8, y), "Estado: Listo para conectar", contentPanel);
            joinStatusLabel.TextColor = new Color(0.7f, 0.7f, 0.7f);
        }

        private void ExecuteJoinSession()
        {
            string ip = joinIpTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

            ushort.TryParse(joinPortTextBox.Text.Trim(), out ushort port);
            if (port == 0) port = 7777;

            string playerName = joinNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(playerName) && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.Name = playerName;
            }

            SaveRecentServer($"{ip}:{port}");

            if (DFUNetworkManager.Instance != null)
            {
                joinStatusLabel.Text = $"Conectando a {ip}:{port}...";
                joinStatusLabel.TextColor = goldTextColor;

                DFUNetworkManager.Instance.ConnectToServer(ip, port);
                CloseWindow();
            }
            else
            {
                joinStatusLabel.Text = "Error: DFUNetworkManager no inicializado.";
                joinStatusLabel.TextColor = Color.red;
            }
        }

        #endregion

        #region Tab 1: Host Server

        private void SetupHostTab()
        {
            float y = 4f;

            AddFormLabel("PUERTO LOCAL (KCP UDP PORT):", new Vector2(8, y));
            y += 10f;
            hostPortTextBox = AddFormInput(new Vector2(8, y), 248f, "7777", 6);
            y += 18f;

            AddFormLabel("ROL DEL ANFITRIÓN:", new Vector2(8, y));
            y += 10f;
            hostRoleButton = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(248f, 13f), contentPanel);
            hostRoleButton.Outline.Enabled = true;
            hostRoleButton.Outline.Color = panelOutlineColor;
            hostRoleButton.BackgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            TextLabel hostRoleLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), hostRoleButton);
            hostRoleLabel.Text = hostAsDm ? "ROL ANFITRIÓN: [ DUNGEON MASTER (DM) ]" : "ROL ANFITRIÓN: [ JUGADOR (PLAYER) ]";
            hostRoleLabel.TextColor = hostAsDm ? goldTextColor : Color.white;
            hostRoleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            hostRoleButton.OnMouseClick += (s, e) =>
            {
                hostAsDm = !hostAsDm;
                hostRoleLabel.Text = hostAsDm ? "ROL ANFITRIÓN: [ DUNGEON MASTER (DM) ]" : "ROL ANFITRIÓN: [ JUGADOR (PLAYER) ]";
                hostRoleLabel.TextColor = hostAsDm ? goldTextColor : Color.white;
            };
            y += 18f;

            AddFormLabel("REGLAS DE COMBATE (PVP / FUEGO AMIGO):", new Vector2(8, y));
            y += 10f;
            hostPvpButton = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(248f, 13f), contentPanel);
            hostPvpButton.Outline.Enabled = true;
            hostPvpButton.Outline.Color = panelOutlineColor;
            hostPvpButton.BackgroundColor = hostPvpEnabled ? new Color(0.5f, 0.2f, 0.2f, 0.9f) : new Color(0.2f, 0.35f, 0.2f, 0.9f);
            TextLabel pvpLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), hostPvpButton);
            pvpLabel.Text = hostPvpEnabled ? "FUEGO AMIGO: ACTIVADO (PVP ON)" : "FUEGO AMIGO: DESACTIVADO (COOP SEGURO)";
            pvpLabel.TextColor = hostPvpEnabled ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 1f, 0.6f);
            pvpLabel.HorizontalAlignment = HorizontalAlignment.Center;
            hostPvpButton.OnMouseClick += (s, e) =>
            {
                hostPvpEnabled = !hostPvpEnabled;
                pvpLabel.Text = hostPvpEnabled ? "FUEGO AMIGO: ACTIVADO (PVP ON)" : "FUEGO AMIGO: DESACTIVADO (COOP SEGURO)";
                pvpLabel.TextColor = hostPvpEnabled ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 1f, 0.6f);
                hostPvpButton.BackgroundColor = hostPvpEnabled ? new Color(0.5f, 0.2f, 0.2f, 0.9f) : new Color(0.2f, 0.35f, 0.2f, 0.9f);
            };
            y += 18f;

            Button startHostButton = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(248f, 16f), contentPanel);
            startHostButton.Outline.Enabled = true;
            startHostButton.Outline.Color = panelOutlineColor;
            startHostButton.BackgroundColor = buttonGreenColor;
            TextLabel startHostLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 2), startHostButton);
            startHostLabel.Text = "INICIAR SERVIDOR (START HOST)";
            startHostLabel.TextColor = Color.white;
            startHostLabel.HorizontalAlignment = HorizontalAlignment.Center;
            startHostButton.OnMouseClick += (s, e) => ExecuteHostSession();
            y += 20f;

            hostStatusLabel = DaggerfallUI.AddTextLabel(new Vector2(8, y), "Listo para crear sesión en este equipo.", contentPanel);
            hostStatusLabel.TextColor = new Color(0.7f, 0.7f, 0.7f);
        }

        private void ExecuteHostSession()
        {
            ushort.TryParse(hostPortTextBox.Text.Trim(), out ushort port);
            if (port == 0) port = 7777;

            if (DFUNetworkManager.Instance != null)
            {
                hostStatusLabel.Text = $"Iniciando Servidor en puerto {port}...";
                hostStatusLabel.TextColor = goldTextColor;

                DFUNetworkManager.Instance.SetPvpAllowed(hostPvpEnabled);
                DFUNetworkManager.Instance.StartHosting(port);
                CloseWindow();
            }
            else
            {
                hostStatusLabel.Text = "Error: DFUNetworkManager no disponible.";
                hostStatusLabel.TextColor = Color.red;
            }
        }

        #endregion

        #region Tab 2: History (Recent Servers)

        private void SetupHistoryTab()
        {
            float y = 4f;
            AddFormLabel("SERVIDORES RECIENTES Y FAVORITOS:", new Vector2(8, y));
            y += 10f;

            Panel listPanel = new Panel();
            listPanel.Position = new Vector2(8, y);
            listPanel.Size = new Vector2(248, 70);
            listPanel.Outline.Enabled = true;
            listPanel.Outline.Color = panelOutlineColor;
            listPanel.BackgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.95f);
            contentPanel.Components.Add(listPanel);

            historyListBox = new ListBox();
            historyListBox.Position = new Vector2(2, 2);
            historyListBox.Size = new Vector2(244, 66);
            historyListBox.RowsDisplayed = 6;
            historyListBox.TextColor = goldTextColor;
            listPanel.Components.Add(historyListBox);

            foreach (string s in recentServers)
            {
                historyListBox.AddItem(s);
            }
            if (recentServers.Count > 0)
                historyListBox.SelectedIndex = 0;

            y += 74f;

            Button quickConnectBtn = DaggerfallUI.AddButton(new Vector2(8, y), new Vector2(120, 14), contentPanel);
            quickConnectBtn.Outline.Enabled = true;
            quickConnectBtn.Outline.Color = panelOutlineColor;
            quickConnectBtn.BackgroundColor = buttonGreenColor;
            TextLabel qcLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), quickConnectBtn);
            qcLabel.Text = "CONECTAR SELECCIONADO";
            qcLabel.TextColor = Color.white;
            qcLabel.HorizontalAlignment = HorizontalAlignment.Center;
            quickConnectBtn.OnMouseClick += (s, e) =>
            {
                if (historyListBox.SelectedIndex >= 0 && historyListBox.SelectedIndex < recentServers.Count)
                {
                    string entry = recentServers[historyListBox.SelectedIndex];
                    string[] parts = entry.Split(':');
                    if (parts.Length > 0 && joinIpTextBox != null)
                        joinIpTextBox.Text = parts[0];
                    if (parts.Length > 1 && joinPortTextBox != null)
                        joinPortTextBox.Text = parts[1];
                    SwitchTab(0);
                }
            };

            Button deleteBtn = DaggerfallUI.AddButton(new Vector2(136, y), new Vector2(120, 14), contentPanel);
            deleteBtn.Outline.Enabled = true;
            deleteBtn.Outline.Color = panelOutlineColor;
            deleteBtn.BackgroundColor = buttonRedColor;
            TextLabel delLabel = DaggerfallUI.AddDefaultTitleTextLabel(new Vector2(0, 1), deleteBtn);
            delLabel.Text = "BORRAR ENTRADA";
            delLabel.TextColor = Color.white;
            delLabel.HorizontalAlignment = HorizontalAlignment.Center;
            deleteBtn.OnMouseClick += (s, e) =>
            {
                if (historyListBox.SelectedIndex >= 0 && historyListBox.SelectedIndex < recentServers.Count)
                {
                    recentServers.RemoveAt(historyListBox.SelectedIndex);
                    SaveRecentServersList();
                    SwitchTab(2);
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

        #region Tab 3: Mods & Compatibility

        private void SetupModsTab()
        {
            float y = 6f;
            AddFormLabel("ESTADO DEL SISTEMA MODS CATCHER:", new Vector2(8, y));
            y += 12f;

            Panel infoBox = new Panel();
            infoBox.Position = new Vector2(8, y);
            infoBox.Size = new Vector2(248, 85);
            infoBox.Outline.Enabled = true;
            infoBox.Outline.Color = panelOutlineColor;
            infoBox.BackgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.95f);
            contentPanel.Components.Add(infoBox);

            string modInfo = "<b>• Mods Catcher:</b> ACTIVO (Protocolo 1:1)\n" +
                             "<b>• Validación de Partida:</b> Automática en Handshake\n" +
                             "<b>• Replicación Config:</b> modsettings.json (Host)\n" +
                             "<b>• Modding TTRPG:</b> Climates & Calories compatible\n" +
                             "<b>• Verificación de Archivos:</b> .dfmod y scripts nativos\n\n" +
                             "<i>Los clientes deben contar con los mismos mods del host\n" +
                             "para evitar inconsistencias en el mundo de juego.</i>";

            modsSummaryLabel = DaggerfallUI.AddTextLabel(new Vector2(4, 4), modInfo, infoBox);
            modsSummaryLabel.TextColor = new Color(0.85f, 0.85f, 0.85f);
        }

        #endregion

        #region Helpers

        private void AddFormLabel(string text, Vector2 pos)
        {
            TextLabel label = DaggerfallUI.AddTextLabel(pos, text, contentPanel);
            label.TextColor = goldTextColor;
            label.ShadowPosition = Vector2.one;
            label.ShadowColor = Color.black;
        }

        private TextBox AddFormInput(Vector2 pos, float width, string defaultText, int maxChars)
        {
            Panel inputPanel = new Panel();
            inputPanel.Position = pos;
            inputPanel.Size = new Vector2(width, 12f);
            inputPanel.Outline.Enabled = true;
            inputPanel.Outline.Color = new Color(0.5f, 0.45f, 0.35f, 0.8f);
            inputPanel.BackgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.95f);
            contentPanel.Components.Add(inputPanel);

            TextBox textBox = new TextBox();
            textBox.Position = new Vector2(2, 1);
            textBox.Size = new Vector2(width - 4, 10f);
            textBox.MaxCharacters = maxChars;
            textBox.Text = defaultText;
            textBox.DefaultText = defaultText;
            textBox.TextColor = Color.white;
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
