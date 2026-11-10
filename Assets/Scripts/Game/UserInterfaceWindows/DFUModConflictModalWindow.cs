// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Author:          Antigravity / DFU-Multiplayer Team
// Description:     Phase 18: Mod Conflict Diagnostic Modal Window for Pre-Game Lobby.

using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Networking;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Modal popup window presenting detailed 1:1 mod parity conflict diagnostics:
    /// missing mods, extra unapproved mods, and version / SHA-256 hash mismatches.
    /// Scaled cleanly for Daggerfall Unity's classic 320x200 canvas.
    /// </summary>
    public class DFUModConflictModalWindow : DaggerfallPopupWindow
    {
        private readonly Panel mainPanel = new Panel();
        private readonly TextLabel titleLabel = new TextLabel();
        private readonly TextLabel subtitleLabel = new TextLabel();
        private ListBox conflictsListBox;
        private Button closeButton;

        public DFUModConflictModalWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null)
            : base(uiManager, previousWindow)
        {
        }

        protected override void Setup()
        {
            if (IsSetup)
                return;

            base.Setup();

            AllowCancel = true;
            ParentPanel.BackgroundColor = new Color(0, 0, 0, 0.65f);

            // Centered Modal Window on 320x200 Canvas
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.Size = new Vector2(264, 154);
            mainPanel.BackgroundTexture = DarkFantasyPixelArtTheme.CreateOrnateWindowTexture(264, 154);
            mainPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            NativePanel.Components.Add(mainPanel);

            // Title
            titleLabel.Position = new Vector2(0, 6);
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.Text = "[ MOD PARITY CONFLICT DIAGNOSTICS ]";
            titleLabel.TextColor = DarkFantasyPixelArtTheme.GoldBright;
            titleLabel.ShadowColor = DarkFantasyPixelArtTheme.ShadowBlack;
            titleLabel.ShadowPosition = Vector2.one;
            mainPanel.Components.Add(titleLabel);

            // Subtitle
            subtitleLabel.Position = new Vector2(0, 16);
            subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            subtitleLabel.Text = "Host requires 100% mod parity. Resolve conflicts below:";
            subtitleLabel.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            subtitleLabel.TextScale = 0.82f;
            mainPanel.Components.Add(subtitleLabel);

            // ListBox inside ornate inner frame
            Panel innerBox = new Panel();
            innerBox.Position = new Vector2(10, 28);
            innerBox.Size = new Vector2(244, 100);
            innerBox.Outline.Enabled = true;
            innerBox.Outline.Color = DarkFantasyPixelArtTheme.BronzeOuter;
            innerBox.BackgroundColor = new Color32(10, 9, 12, 245);
            mainPanel.Components.Add(innerBox);

            conflictsListBox = new ListBox();
            conflictsListBox.Position = new Vector2(4, 4);
            conflictsListBox.Size = new Vector2(236, 92);
            conflictsListBox.RowsDisplayed = 9;
            conflictsListBox.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            innerBox.Components.Add(conflictsListBox);

            PopulateDiagnosticsList();

            // Bottom Close Button
            closeButton = DaggerfallUI.AddButton(new Vector2(82, 132), new Vector2(100, 15), mainPanel);
            closeButton.BackgroundTexture = DarkFantasyPixelArtTheme.EmeraldButtonTexture;
            TextLabel closeLbl = DaggerfallUI.AddDefaultShadowedTextLabel(new Vector2(0, 3), closeButton);
            closeLbl.Text = "ENTENDIDO / CLOSE";
            closeLbl.TextColor = DarkFantasyPixelArtTheme.TextIvory;
            closeLbl.HorizontalAlignment = HorizontalAlignment.Center;
            closeButton.OnMouseClick += (s, e) => CloseWindow();
        }

        private void PopulateDiagnosticsList()
        {
            conflictsListBox.ClearItems();

            if (DFUModSyncController.Instance == null)
            {
                conflictsListBox.AddItem("No diagnostic information available.");
                return;
            }

            var sync = DFUModSyncController.Instance;

            if (!sync.HasModConflicts)
            {
                conflictsListBox.AddItem("✦ ¡100% PARIDAD DE MODS VERIFICADA! ✦");
                conflictsListBox.AddItem("Tu cliente posee exactamente los mismos mods");
                conflictsListBox.AddItem("y versiones que el servidor.");
                conflictsListBox.AddItem("Estado: Listo para jugar (Ready OK).");
                return;
            }

            // 1. Missing Mods
            if (sync.MissingMods.Count > 0)
            {
                conflictsListBox.AddItem($"--- MODS FALTANTES ({sync.MissingMods.Count}) - Debes instalar: ---");
                foreach (string m in sync.MissingMods)
                {
                    conflictsListBox.AddItem($"  [FALTA] {m}");
                }
            }

            // 2. Extra Mods
            if (sync.ExtraMods.Count > 0)
            {
                conflictsListBox.AddItem($"--- MODS SOBRANTES ({sync.ExtraMods.Count}) - Debes desactivar: ---");
                foreach (string m in sync.ExtraMods)
                {
                    conflictsListBox.AddItem($"  [EXTRA] {m}");
                }
            }

            // 3. Version / Checksum Mismatches
            if (sync.VersionMismatches.Count > 0)
            {
                conflictsListBox.AddItem($"--- VERSIONES O ARCHIVOS EN CONFLICTO ({sync.VersionMismatches.Count}): ---");
                foreach (string m in sync.VersionMismatches)
                {
                    conflictsListBox.AddItem($"  [MISMATCH] {m}");
                }
            }

            conflictsListBox.AddItem("----------------------------------------");
            conflictsListBox.AddItem("Guía: Consulta Docs/modding_guide_climates_and_calories.md");
        }
    }
}
