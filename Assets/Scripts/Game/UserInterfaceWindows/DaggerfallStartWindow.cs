// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using System;
using DaggerfallWorkshop.Game.UserInterface;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements the Daggerfall Start user interface refactored with Dark Fantasy Pixel-Art menu framing.
    /// </summary>
    public class DaggerfallStartWindow : DaggerfallBaseWindow
    {
        const string nativeImgName = "PICK03I0.IMG";

        Texture2D nativeTexture;
        Texture2D titleLogoTexture;
        Texture2D menuFrameTexture;
        Texture2D crimsonBtnTexture;
        Texture2D goldBtnTexture;
        Texture2D darkBtnTexture;

        private Panel titleLogoPanel;
        private Panel menuContainerPanel;
        private Button loadGameButton;
        private Button newGameButton;
        private Button multiplayerButton;
        private Button exitButton;

        bool isLoadGameDeferred = false;
        bool isNewGameDeferred = false;
        bool isExitGameDeferred = false;

        public DaggerfallStartWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        protected override void Setup()
        {
            // Load custom pixel-art campfire menu background from Resources/Screens/background_menu
            nativeTexture = Resources.Load<Texture2D>("Screens/background_menu");
            if (nativeTexture != null)
            {
                nativeTexture.filterMode = FilterMode.Point;
            }
            else
            {
                // Fallback to classic IMG only if resource is missing
                nativeTexture = DaggerfallUI.GetTextureFromImg(nativeImgName);
            }

            if (!nativeTexture)
                throw new Exception("DaggerfallStartWindow: Could not load menu background texture.");

            // Fill full screen cleanly and keep 320x200 virtual coordinate space on NativePanel
            ParentPanel.BackgroundTexture = nativeTexture;
            ParentPanel.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;
            NativePanel.BackgroundColor = Color.clear;
            NativePanel.BackgroundTexture = nativeTexture;
            NativePanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

            // Load title logo (The Elder Scrolls II: Daggerfall Multiplayer TTRPG)
            titleLogoTexture = Resources.Load<Texture2D>("Screens/title_logo");
            if (titleLogoTexture != null)
            {
                titleLogoTexture.filterMode = FilterMode.Point;
                titleLogoPanel = new Panel();
                titleLogoPanel.Position = new Vector2(16, 12);
                titleLogoPanel.Size = new Vector2(120, 70);
                titleLogoPanel.BackgroundTexture = titleLogoTexture;
                titleLogoPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                NativePanel.Components.Add(titleLogoPanel);
            }

            // Compact Dark Fantasy Pixel-Art menu container positioned neatly below the title banner
            int frameW = 120;
            int frameH = 99;
            menuFrameTexture = DarkFantasyPixelArtTheme.CreateOrnateWindowTexture(
                frameW,
                frameH,
                new Color32(16, 14, 18, 238),
                parchmentInterior: false);

            int btnW = 106;
            crimsonBtnTexture = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(
                btnW, 18,
                DarkFantasyPixelArtTheme.CrimsonBase,
                DarkFantasyPixelArtTheme.CrimsonHighlight,
                DarkFantasyPixelArtTheme.CrimsonShadow);

            goldBtnTexture = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(
                btnW, 20,
                new Color32(118, 84, 32, 250),
                DarkFantasyPixelArtTheme.GoldHighlight,
                new Color32(58, 40, 14, 255));

            darkBtnTexture = DarkFantasyPixelArtTheme.CreatePixelButtonTexture(
                btnW, 17,
                DarkFantasyPixelArtTheme.IronButtonBase,
                DarkFantasyPixelArtTheme.IronButtonHighlight,
                DarkFantasyPixelArtTheme.IronButtonShadow);

            menuContainerPanel = new Panel();
            menuContainerPanel.Position = new Vector2(16, 88);
            menuContainerPanel.Size = new Vector2(frameW, frameH);
            menuContainerPanel.BackgroundTexture = menuFrameTexture;
            NativePanel.Components.Add(menuContainerPanel);

            // If title graphic is missing, fallback to text header
            if (titleLogoTexture == null)
            {
                TextLabel headerLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 7), menuContainerPanel);
                headerLabel.Text = "DAGGERFALL";
                headerLabel.TextColor = DarkFantasyPixelArtTheme.TextGold;
                headerLabel.ShadowColor = Color.black;
                headerLabel.ShadowPosition = Vector2.one;
                headerLabel.HorizontalAlignment = HorizontalAlignment.Center;

                TextLabel subHeaderLabel = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 16), menuContainerPanel);
                subHeaderLabel.Text = "CHRONICLES & TTRPG";
                subHeaderLabel.TextColor = DarkFantasyPixelArtTheme.TextMuted;
                subHeaderLabel.ShadowColor = Color.black;
                subHeaderLabel.ShadowPosition = Vector2.one;
                subHeaderLabel.HorizontalAlignment = HorizontalAlignment.Center;
                subHeaderLabel.TextScale = 0.85f;
            }

            int btnX = (frameW - btnW) / 2;

            // Button 1: Start New Game (Crimson Pixel-Art Button)
            newGameButton = CreateStyledMenuButton(
                new Vector2(btnX, 7),
                new Vector2(btnW, 18),
                "NEW ADVENTURE",
                crimsonBtnTexture,
                DarkFantasyPixelArtTheme.TextIvory);
            newGameButton.OnMouseClick += NewGameButton_OnMouseClick;
            newGameButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.MainMenuStart);
            newGameButton.OnKeyboardEvent += NewGameButton_OnKeyboardEvent;

            // Button 2: Load Saved Game (Crimson Pixel-Art Button)
            loadGameButton = CreateStyledMenuButton(
                new Vector2(btnX, 29),
                new Vector2(btnW, 18),
                "LOAD SAVED GAME",
                crimsonBtnTexture,
                DarkFantasyPixelArtTheme.TextIvory);
            loadGameButton.OnMouseClick += LoadGameButton_OnMouseClick;
            loadGameButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.MainMenuLoad);
            loadGameButton.OnKeyboardEvent += LoadGameButton_OnKeyboardEvent;

            // Button 3: Multiplayer / TTRPG Lobby (Ornate Gold/Bronze Highlight Button)
            multiplayerButton = CreateStyledMenuButton(
                new Vector2(btnX, 51),
                new Vector2(btnW, 20),
                "MULTIPLAYER LOBBY",
                goldBtnTexture,
                DarkFantasyPixelArtTheme.GoldBright);
            multiplayerButton.OnMouseClick += (s, e) => OpenMultiplayerWindow();

            // Button 4: Exit Game (Dark Iron Button)
            exitButton = CreateStyledMenuButton(
                new Vector2(btnX, 75),
                new Vector2(btnW, 17),
                "EXIT TO DESKTOP",
                darkBtnTexture,
                DarkFantasyPixelArtTheme.TextMuted);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.MainMenuExit);
            exitButton.OnKeyboardEvent += ExitButton_OnKeyboardEvent;
        }

        private Button CreateStyledMenuButton(Vector2 pos, Vector2 size, string labelText, Texture2D bgTex, Color textColor)
        {
            Button btn = DaggerfallUI.AddButton(pos, size, menuContainerPanel);
            btn.BackgroundTexture = bgTex;

            TextLabel label = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, (size.y - 7f) * 0.5f), btn);
            label.Text = labelText;
            label.TextColor = textColor;
            label.ShadowColor = Color.black;
            label.ShadowPosition = Vector2.one;
            label.HorizontalAlignment = HorizontalAlignment.Center;

            return btn;
        }

        public override void Update()
        {
            base.Update();
            InputManager.Instance.CursorVisible = true;
        }

        float timer = 0;
        bool oneTime = false;
        public override void Draw()
        {
            base.Draw();

            // 100ms is enough time to ensure window drawn and event raised before user can click
            if (!oneTime && (timer += Time.realtimeSinceStartup) > 100)
            {
                RaiseOnStartFirstVisibleEvent();
                oneTime = true;
            }
        }

        void LoadGame()
        {
            uiManager.PushWindow(UIWindowFactory.GetInstanceWithArgs(UIWindowType.UnitySaveGame, new object[] { uiManager, DaggerfallUnitySaveGameWindow.Modes.LoadGame, null, true }));
        }

        void StartNewGame()
        {
            if (Networking.DFUNetworkManager.Instance != null)
                Networking.DFUNetworkManager.Instance.isCreatingCharacterForLobby = false;

            uiManager.PushWindow(UIWindowFactory.GetInstance(UIWindowType.StartNewGameWizard, uiManager));
        }

        void ExitGame()
        {
            Application.Quit();
        }

        void OpenMultiplayerWindow()
        {
            uiManager.PushWindow(new DaggerfallMultiplayerWindow(uiManager, this));
        }

        public override void ProcessMessages()
        {
            base.ProcessMessages();

            switch (uiManager.GetMessage())
            {
                case DaggerfallUIMessages.dfuiOpenLoadSavedGameWindow:
                    LoadGame();
                    break;
                case DaggerfallUIMessages.dfuiStartNewGame:
                    StartNewGame();
                    break;
                case DaggerfallUIMessages.dfuiExitGame:
                    ExitGame();
                    break;
            }
        }

        void LoadGameButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            LoadGame();
        }

        void LoadGameButton_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyDown)
                isLoadGameDeferred = true;
            else if (keyboardEvent.type == EventType.KeyUp && isLoadGameDeferred)
            {
                isLoadGameDeferred = false;
                LoadGame();
            }
        }

        void NewGameButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            StartNewGame();
        }

        void NewGameButton_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyDown)
                isNewGameDeferred = true;
            else if (keyboardEvent.type == EventType.KeyUp && isNewGameDeferred)
            {
                isNewGameDeferred = false;
                StartNewGame();
            }
        }

        void ExitButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            ExitGame();
        }

        void ExitButton_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyDown)
                isExitGameDeferred = true;
            else if (keyboardEvent.type == EventType.KeyUp && isExitGameDeferred)
            {
                isExitGameDeferred = false;
                ExitGame();
            }
        }

        public delegate void OnStartFirstVisibleEventHandler();
        public static event OnStartFirstVisibleEventHandler OnStartFirstVisible;
        void RaiseOnStartFirstVisibleEvent()
        {
            if (OnStartFirstVisible != null)
                OnStartFirstVisible();
        }
    }
}