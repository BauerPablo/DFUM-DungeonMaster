// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Lateral side popup window displaying live party rest / loiter vote status,
    /// countdown timer, consensus checklist of party members, and native Yes/No buttons.
    /// Scaled cleanly for DFU's classic 320x200 canvas.
    /// </summary>
    public class DaggerfallPartyVoteWindow : DaggerfallPopupWindow
    {
        private const string buttonsFilename = "BUTTONS.RCI";

        private readonly Panel mainPanel = new Panel();
        private readonly TextLabel titleLabel = new TextLabel();
        private readonly TextLabel infoLabel = new TextLabel();
        private readonly TextLabel consensusLabel = new TextLabel();
        private readonly Panel checklistPanel = new Panel();
        private readonly Panel buttonPanel = new Panel();
        private readonly List<TextLabel> voterLabels = new List<TextLabel>();

        private Button yesButton;
        private Button noButton;
        private Vector2 cachedYesSize = new Vector2(32, 14);
        private Vector2 cachedNoSize = new Vector2(32, 14);

        public event Action<bool> OnVoteSubmitted;

        public List<TextLabel> VoterLabels => voterLabels;

        public DaggerfallPartyVoteWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null)
            : base(uiManager, previousWindow)
        {
        }

        protected override void Setup()
        {
            if (IsSetup)
                return;

            base.Setup();

            AllowCancel = false;
            ParentPanel.BackgroundColor = Color.clear;

            // Lateral top-right side docking scaled for 320x200 canvas
            mainPanel.HorizontalAlignment = HorizontalAlignment.Right;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.Position = new Vector2(-6, 12);
            mainPanel.Size = new Vector2(125, 78);
            DaggerfallUI.Instance.SetDaggerfallPopupStyle(DaggerfallUI.PopupStyle.Parchment, mainPanel);
            NativePanel.Components.Add(mainPanel);

            // Title label
            titleLabel.Position = new Vector2(0, 4);
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.Text = "[ Party Vote ]";
            titleLabel.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
            titleLabel.ShadowColor = Color.black;
            titleLabel.ShadowPosition = Vector2.one;
            mainPanel.Components.Add(titleLabel);

            // Info label (e.g. Rest for 6 hours / Fast travel to Daggerfall)
            infoLabel.Position = new Vector2(0, 13);
            infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
            infoLabel.Text = "Proposal";
            infoLabel.TextColor = Color.white;
            infoLabel.ShadowColor = Color.black;
            infoLabel.ShadowPosition = Vector2.one;
            mainPanel.Components.Add(infoLabel);

            // Consensus & countdown timer on single line
            consensusLabel.Position = new Vector2(0, 22);
            consensusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            consensusLabel.Text = "Votes: 0/0  (15s)";
            consensusLabel.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
            consensusLabel.ShadowColor = Color.black;
            consensusLabel.ShadowPosition = Vector2.one;
            mainPanel.Components.Add(consensusLabel);

            // Voter checklist panel
            checklistPanel.Position = new Vector2(6, 31);
            checklistPanel.Size = new Vector2(113, 22);
            mainPanel.Components.Add(checklistPanel);

            // Button panel for native Yes / No buttons
            buttonPanel.Position = new Vector2(0, 55);
            buttonPanel.HorizontalAlignment = HorizontalAlignment.Center;
            buttonPanel.VerticalAlignment = VerticalAlignment.None;
            mainPanel.Components.Add(buttonPanel);

            // Native buttons from BUTTONS.RCI
            Texture2D yesTexture = DaggerfallUI.GetTextureFromCifRci(buttonsFilename, (int)DaggerfallMessageBox.MessageBoxButtons.Yes);
            if (yesTexture != null)
                cachedYesSize = TextureReplacement.GetSize(yesTexture, buttonsFilename, (int)DaggerfallMessageBox.MessageBoxButtons.Yes);

            yesButton = DaggerfallUI.AddButton(Vector2.zero, cachedYesSize, buttonPanel);
            yesButton.BackgroundTexture = yesTexture;
            yesButton.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            yesButton.OnMouseClick += (sender, pos) => SubmitVote(true);

            Texture2D noTexture = DaggerfallUI.GetTextureFromCifRci(buttonsFilename, (int)DaggerfallMessageBox.MessageBoxButtons.No);
            if (noTexture != null)
                cachedNoSize = TextureReplacement.GetSize(noTexture, buttonsFilename, (int)DaggerfallMessageBox.MessageBoxButtons.No);

            noButton = DaggerfallUI.AddButton(new Vector2(cachedYesSize.x + 8, 0), cachedNoSize, buttonPanel);
            noButton.BackgroundTexture = noTexture;
            noButton.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            noButton.OnMouseClick += (sender, pos) => SubmitVote(false);

            buttonPanel.Size = new Vector2(cachedYesSize.x + 8 + cachedNoSize.x, Mathf.Max(cachedYesSize.y, cachedNoSize.y));

            IsSetup = true;
        }

        public override void Update()
        {
            base.Update();

            if (buttonPanel != null && buttonPanel.Enabled)
            {
                if (yesButton != null && yesButton.Enabled && Input.GetKeyDown(KeyCode.Y))
                    SubmitVote(true);
                else if (noButton != null && noButton.Enabled && (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape)))
                    SubmitVote(false);
            }
        }

        private void SubmitVote(bool approved)
        {
            if (approved)
            {
                if (yesButton != null)
                    yesButton.Enabled = false;

                if (noButton != null)
                    noButton.Position = new Vector2((buttonPanel.Size.x - cachedNoSize.x) / 2f, 0);
            }

            OnVoteSubmitted?.Invoke(approved);
        }

        /// <summary>
        /// Updates the real-time voting display with custom title and proposal details.
        /// </summary>
        public void UpdateVoteState(string title, string details, int approved, int total, float remainingSec, string voterStatuses, bool localHasVoted)
        {
            if (!IsSetup)
                Setup();

            if (!string.IsNullOrEmpty(title))
                titleLabel.Text = title;

            infoLabel.Text = details;
            consensusLabel.Text = $"Votes: {approved}/{total}  ({Mathf.CeilToInt(Mathf.Max(0, remainingSec))}s)";

            // Rebuild checklist items
            checklistPanel.Components.Clear();
            voterLabels.Clear();

            if (!string.IsNullOrEmpty(voterStatuses))
            {
                string[] entries = voterStatuses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                float currentY = 0;
                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(':');
                    if (parts.Length < 2)
                        continue;

                    string name = parts[0].Trim();
                    string status = parts[1].Trim();

                    TextLabel label = new TextLabel();
                    label.Position = new Vector2(0, currentY);
                    label.ShadowColor = Color.black;
                    label.ShadowPosition = Vector2.one;

                    if (status.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                    {
                        label.Text = $"[x] {name}";
                        label.TextColor = new Color32(100, 220, 100, 255);
                    }
                    else if (status.Equals("No", StringComparison.OrdinalIgnoreCase))
                    {
                        label.Text = $"[!] {name} (No)";
                        label.TextColor = new Color32(230, 80, 80, 255);
                    }
                    else
                    {
                        label.Text = $"[ ] {name} (Wait)";
                        label.TextColor = Color.white;
                    }

                    checklistPanel.Components.Add(label);
                    voterLabels.Add(label);
                    currentY += 8f;
                }
            }

            // Button layout & visibility:
            // If the local player has already approved, disable Yes button so they can't double-vote,
            // but keep the No button visible as Cancel button so they can cancel if desired.
            if (localHasVoted)
            {
                if (yesButton != null)
                    yesButton.Enabled = false;

                if (noButton != null)
                {
                    noButton.Enabled = true;
                    // Center the cancel button alone in the panel
                    noButton.Position = new Vector2((buttonPanel.Size.x - cachedNoSize.x) / 2f, 0);
                }
            }
            else
            {
                if (yesButton != null)
                {
                    yesButton.Enabled = true;
                    yesButton.Position = Vector2.zero;
                }

                if (noButton != null)
                {
                    noButton.Enabled = true;
                    noButton.Position = new Vector2(cachedYesSize.x + 8, 0);
                }
            }

            if (buttonPanel != null)
                buttonPanel.Enabled = true;
        }

        /// <summary>
        /// Backwards-compatible rest/loiter update method.
        /// </summary>
        public void UpdateVoteState(string initiator, int hours, bool isLoiter, int approved, int total, float remainingSec, string voterStatuses, bool localHasVoted)
        {
            string action = isLoiter ? "Loiter" : "Rest";
            string title = isLoiter ? "[ Party Loiter ]" : "[ Party Rest ]";
            string details = $"{action} {hours}h ({initiator})";
            UpdateVoteState(title, details, approved, total, remainingSec, voterStatuses, localHasVoted);
        }
    }
}
