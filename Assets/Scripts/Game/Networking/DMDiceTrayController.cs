// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Author:          Antigravity / DFU-Multiplayer Team
// Description:     Interactive Top-Center D20 Tray & BG3-Style DC Skill Challenge Controller (Phase 17).

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Interactive Baldur's Gate 3-style D20 Tray & DM Challenge system.
    /// Provides a pull-down triangular tab at the top-center of the screen, an animated 20-sided die
    /// with tumbling suspense (~1.8s), and DM-directed DC skill checks with hidden/visible DC options.
    /// </summary>
    public class DMDiceTrayController : MonoBehaviour
    {
        public static DMDiceTrayController Instance { get; private set; }
        public static event Action<MsgDiceResultBroadcast> OnDiceRollCompleted;

        // Tray UI State
        private bool isTrayOpen = false;
        public bool IsTrayOpen => isTrayOpen;

        // Rolling & Visual Animation State
        private bool isRolling = false;
        private float rollTimer = 0f;
        private const float RollDuration = 1.8f;
        private int currentDisplayNumber = 20;
        private int finalRollResult = 20;
        private float rattleTimer = 0f;
        private float dieRotation = 0f;
        private float dieBounceY = 0f;
        private float dieScaleX = 1f;
        private float dieScaleY = 1f;

        // Procedural Textures & Styles
        private Texture2D d20NormalTex;
        private Texture2D d20CritSuccessTex;
        private Texture2D d20CritFailTex;
        private GUIStyle d20CenteredNumberStyle;
        private GUIStyle d20MotionGlintStyle;

        // Active DC Challenge
        private int activeChallengeId = 0;
        private string activeChallengeTitle = "";
        private int activeTargetDC = 15;
        private bool activeShowDC = true;
        private bool hasActiveChallenge = false;
        private bool activeChallengeResolved = false;

        // Latest Result Banner
        private bool showResultBanner = false;
        private float resultBannerTimer = 0f;
        private const float ResultBannerDuration = 5.0f;
        private MsgDiceResultBroadcast lastResult;

        // DM Challenge Composer inputs
        private string dmInputTitle = "Percepción";
        private string dmInputDC = "15";
        private bool dmInputShowDC = true;

        // Styles
        private GUIStyle d20NumberStyle;
        private GUIStyle challengeHeaderStyle;
        private GUIStyle bannerStyle;
        private GUIStyle tabButtonStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.ReplaceHandler<MsgDiceChallenge>(OnClientReceiveDiceChallenge);
                NetworkClient.ReplaceHandler<MsgDiceResultBroadcast>(OnClientReceiveDiceResultBroadcast);
            }
            if (NetworkServer.active)
            {
                NetworkServer.ReplaceHandler<MsgSubmitDiceRoll>(OnServerReceiveSubmitDiceRoll);
            }
        }

        public void UnregisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.UnregisterHandler<MsgDiceChallenge>();
                NetworkClient.UnregisterHandler<MsgDiceResultBroadcast>();
            }
            if (NetworkServer.active)
            {
                NetworkServer.UnregisterHandler<MsgSubmitDiceRoll>();
            }
        }

        private void Update()
        {
            // Unlock and keep cursor visible while tray is open
            if (isTrayOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (InputManager.Instance != null)
                    InputManager.Instance.CursorVisible = true;
            }

            // Update rolling animation & physics
            if (isRolling)
            {
                float progress = 1f - Mathf.Clamp01(rollTimer / RollDuration);
                rollTimer -= Time.unscaledDeltaTime;

                // Angular deceleration (cubic ease-out)
                float spinVelocity = (1f - progress * progress) * 1150f;
                dieRotation += spinVelocity * Time.unscaledDeltaTime;

                // Damped hopping bounce across the felt tray
                dieBounceY = -Mathf.Abs(Mathf.Sin(progress * 4f * Mathf.PI)) * Mathf.Pow(1f - progress, 1.4f) * 26f;

                // Dynamic squash & stretch on bounce impacts
                float impactSquash = Mathf.Max(0f, 1f - Mathf.Abs(Mathf.Sin(progress * 4f * Mathf.PI)) * 4f) * (1f - progress) * 0.16f;
                dieScaleX = 1f + impactSquash;
                dieScaleY = 1f - impactSquash;

                // Realistic decelerating rattle clacks
                rattleTimer -= Time.unscaledDeltaTime;
                if (rattleTimer <= 0f)
                {
                    float rattleInterval = Mathf.Lerp(0.06f, 0.30f, progress * progress);
                    rattleTimer = rattleInterval;
                    DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
                }

                if (rollTimer <= 0f)
                {
                    isRolling = false;
                    dieRotation = 0f;
                    dieBounceY = 0f;
                    dieScaleX = 1.15f;
                    dieScaleY = 1.15f;
                    currentDisplayNumber = finalRollResult;
                    OnRollCompleted();
                }
            }
            else
            {
                // Smoothly return scale to 1.0f after settle punch
                if (Mathf.Abs(dieScaleX - 1f) > 0.005f)
                {
                    dieScaleX = Mathf.Lerp(dieScaleX, 1f, Time.unscaledDeltaTime * 8f);
                    dieScaleY = Mathf.Lerp(dieScaleY, 1f, Time.unscaledDeltaTime * 8f);
                }
            }

            // Update result banner fade timer
            if (showResultBanner)
            {
                resultBannerTimer -= Time.unscaledDeltaTime;
                if (resultBannerTimer <= 0f)
                {
                    showResultBanner = false;
                }
            }
        }

        #region Roll Execution & Submission

        /// <summary>
        /// Starts rolling the d20 with tumbling suspense.
        /// </summary>
        public void RollD20()
        {
            if (isRolling)
                return;

            finalRollResult = UnityEngine.Random.Range(1, 21);
            isRolling = true;
            rollTimer = RollDuration;
            rattleTimer = 0.05f;
            dieRotation = 0f;
            dieBounceY = 0f;
            dieScaleX = 1f;
            dieScaleY = 1f;
            DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
        }

        private void OnRollCompleted()
        {
            string pName = GetLocalPlayerName();
            int connId = NetworkSession.IsHost ? 0 : DFUNetworkManager.Instance.localConnectionId;

            // Play resolution sound
            if (finalRollResult == 20)
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.GoldPieces);
            else if (finalRollResult == 1)
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
            else
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);

            if (NetworkSession.IsActive)
            {
                int chalId = (hasActiveChallenge && !activeChallengeResolved) ? activeChallengeId : 0;
                // Send to server
                MsgSubmitDiceRoll submitMsg = new MsgSubmitDiceRoll
                {
                    challengeId = chalId,
                    rollResult = finalRollResult,
                    rollerName = pName,
                    rollerConnectionId = connId
                };

                if (NetworkServer.active)
                {
                    ProcessSubmittedRoll(submitMsg);
                }
                else if (NetworkClient.active)
                {
                    NetworkClient.Send(submitMsg);
                }
            }
            else
            {
                int dc = activeTargetDC;
                if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.activeEscapeChallengeId != 0)
                    dc = DFUNetworkManager.Instance.activeEscapeDC;

                int chalId = (hasActiveChallenge && !activeChallengeResolved) ? activeChallengeId : 0;
                // Single player fallback
                MsgDiceResultBroadcast localResult = new MsgDiceResultBroadcast
                {
                    challengeId = chalId,
                    challengeTitle = hasActiveChallenge ? activeChallengeTitle : "Tirada Libre",
                    rollerName = pName,
                    rollerConnectionId = 0,
                    rollResult = finalRollResult,
                    targetDC = dc,
                    showDC = activeShowDC,
                    isCriticalSuccess = (finalRollResult == 20),
                    isCriticalFail = (finalRollResult == 1),
                    passed = (finalRollResult == 20) || (finalRollResult != 1 && finalRollResult >= dc)
                };
                ShowResult(localResult);
            }
        }

        #endregion

        #region Server-Side DC Resolution

        private void OnServerReceiveSubmitDiceRoll(NetworkConnection conn, MsgSubmitDiceRoll msg)
        {
            ProcessSubmittedRoll(msg);
        }

        private void ProcessSubmittedRoll(MsgSubmitDiceRoll msg)
        {
            int dc = activeTargetDC;
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.activeEscapeChallengeId != 0 &&
                (DFUNetworkManager.Instance.activeEscapeChallengeId == msg.challengeId || DFUNetworkManager.Instance.activeEscapeTargetConnId == msg.rollerConnectionId))
            {
                dc = DFUNetworkManager.Instance.activeEscapeDC;
            }

            bool isCritSuccess = (msg.rollResult == 20);
            bool isCritFail = (msg.rollResult == 1);
            bool passed = isCritSuccess || (!isCritFail && msg.rollResult >= dc);

            MsgDiceResultBroadcast resultMsg = new MsgDiceResultBroadcast
            {
                challengeId = msg.challengeId,
                challengeTitle = hasActiveChallenge ? activeChallengeTitle : "Tirada de Dado",
                rollerName = msg.rollerName,
                rollerConnectionId = msg.rollerConnectionId,
                rollResult = msg.rollResult,
                targetDC = dc,
                showDC = activeShowDC,
                isCriticalSuccess = isCritSuccess,
                isCriticalFail = isCritFail,
                passed = passed
            };

            if (hasActiveChallenge && (msg.challengeId == activeChallengeId || msg.challengeId == 0))
            {
                activeChallengeResolved = true;
            }

            // Broadcast authoritative outcome to all players
            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(resultMsg);
            }
            else
            {
                ShowResult(resultMsg);
            }
        }

        #endregion

        #region Client-Side Handlers

        private void OnClientReceiveDiceChallenge(MsgDiceChallenge msg)
        {
            activeChallengeId = msg.challengeId;
            activeChallengeTitle = msg.challengeTitle;
            activeTargetDC = msg.targetDC;
            activeShowDC = msg.showDC;
            hasActiveChallenge = true;
            activeChallengeResolved = false;

            // Automatically open tray so the player notices the challenge immediately
            isTrayOpen = true;

            string dcText = msg.showDC ? $"DC {msg.targetDC}" : "DC Oculta";
            DaggerfallUI.AddHUDText($"[Desafío DM] ¡Nueva prueba: {msg.challengeTitle} ({dcText})!");
            DaggerfallUI.Instance?.PlayOneShot(SoundClips.ParchmentScratching);
        }

        private void OnClientReceiveDiceResultBroadcast(MsgDiceResultBroadcast msg)
        {
            ShowResult(msg);
        }

        private void ShowResult(MsgDiceResultBroadcast res)
        {
            lastResult = res;
            showResultBanner = true;
            resultBannerTimer = ResultBannerDuration;

            if (hasActiveChallenge && (res.challengeId == activeChallengeId || res.challengeId == 0))
            {
                activeChallengeResolved = true;
            }

            // HUD notification
            string outcomeStr;
            if (res.isCriticalSuccess)
                outcomeStr = "<color=#FFDF78><b>¡ÉXITO CRÍTICO! (NAT 20)</b></color>";
            else if (res.isCriticalFail)
                outcomeStr = "<color=#FF5555><b>¡FALLO CRÍTICO! (NAT 1)</b></color>";
            else if (res.passed)
                outcomeStr = "<color=#55FF55><b>¡ÉXITO!</b></color>";
            else
                outcomeStr = "<color=#FF7777><b>FALLÓ</b></color>";

            string dcStr = res.showDC ? $" (vs DC {res.targetDC})" : "";
            DaggerfallUI.AddHUDText($"[D20] {res.rollerName} sacó un <b>{res.rollResult}</b>{dcStr}: {outcomeStr}");

            OnDiceRollCompleted?.Invoke(res);
        }

        public void OpenTray()
        {
            isTrayOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (InputManager.Instance != null)
                InputManager.Instance.CursorVisible = true;
        }

        public void CloseTray()
        {
            isTrayOpen = false;
            bool isChatOpen = (DMChatController.Instance != null && DMChatController.Instance.IsChatOpen);
            bool isOtherUiOpen = (DaggerfallUI.UIManager != null && DaggerfallUI.UIManager.TopWindow != null && !(DaggerfallUI.UIManager.TopWindow is DaggerfallHUD));
            bool isManagerGuiOpen = (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.IsAnyGuiOpen);
            if (!isChatOpen && !isOtherUiOpen && !isManagerGuiOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (InputManager.Instance != null)
                    InputManager.Instance.CursorVisible = false;
            }
        }

        public void SetActiveChallengeDirect(int challengeId, string title, int targetDC, bool showDC)
        {
            activeChallengeId = challengeId;
            activeChallengeTitle = string.IsNullOrEmpty(title) ? "Prueba de Habilidad" : title;
            activeTargetDC = Mathf.Clamp(targetDC, 1, 30);
            activeShowDC = showDC;
            hasActiveChallenge = true;
            activeChallengeResolved = false;
            OpenTray();
        }

        #endregion

        #region DM Challenge Creator

        /// <summary>
        /// Called by DM to issue a party-wide DC challenge.
        /// </summary>
        public void IssueDMChallenge(string title, int targetDC, bool showDC, int explicitChallengeId = 0)
        {
            if (!NetworkSession.IsHost && !NetworkSession.IsSinglePlayer)
            {
                DaggerfallUI.AddHUDText("[D20] Solo el Dungeon Master puede lanzar desafíos de dificultad.");
                return;
            }

            activeChallengeId = (explicitChallengeId != 0) ? explicitChallengeId : UnityEngine.Random.Range(1000, 999999);
            activeChallengeTitle = string.IsNullOrEmpty(title) ? "Prueba de Habilidad" : title;
            activeTargetDC = Mathf.Clamp(targetDC, 1, 30);
            activeShowDC = showDC;
            hasActiveChallenge = true;
            activeChallengeResolved = false;

            MsgDiceChallenge msg = new MsgDiceChallenge
            {
                challengeId = activeChallengeId,
                challengeTitle = activeChallengeTitle,
                targetDC = activeTargetDC,
                showDC = activeShowDC
            };

            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(msg);
            }
            else
            {
                OnClientReceiveDiceChallenge(msg);
            }
        }

        #endregion

        #region GUI Rendering (Top-Center Tray & BG3 Banner)

        private void OnGUI()
        {
            GUI.depth = -1020;
            InitStyles();

            // 1. Triangular Puller Tab at Top-Center
            float tabWidth = 84f;
            float tabHeight = 22f;
            float tabX = (Screen.width - tabWidth) * 0.5f;
            Rect tabRect = new Rect(tabX, 0f, tabWidth, tabHeight);

            string tabLabel = isTrayOpen ? "▲ D20 ▲" : "▼ D20 ▼";
            if (GUI.Button(tabRect, tabLabel, tabButtonStyle))
            {
                isTrayOpen = !isTrayOpen;
                if (!isTrayOpen)
                {
                    bool isChatOpen = (DMChatController.Instance != null && DMChatController.Instance.IsChatOpen);
                    bool isOtherUiOpen = (DaggerfallUI.UIManager != null && DaggerfallUI.UIManager.TopWindow != null && !(DaggerfallUI.UIManager.TopWindow is DaggerfallHUD));
                    bool isManagerGuiOpen = (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.IsAnyGuiOpen);
                    if (!isChatOpen && !isOtherUiOpen && !isManagerGuiOpen)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                        if (InputManager.Instance != null)
                            InputManager.Instance.CursorVisible = false;
                    }
                }
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
            }

            // 2. Unfolded Dice Tray
            if (isTrayOpen)
            {
                float trayWidth = 350f;
                float trayHeight = (NetworkSession.IsHost || NetworkSession.IsSinglePlayer) ? 355f : 230f;
                float trayX = (Screen.width - trayWidth) * 0.5f;
                Rect trayRect = new Rect(trayX, tabHeight, trayWidth, trayHeight);

                GUI.Box(trayRect, GUIContent.none, DarkFantasyPixelArtTheme.SectionBannerStyle ?? GUI.skin.box);
                GUILayout.BeginArea(new Rect(trayRect.x + 10f, trayRect.y + 6f, trayWidth - 20f, trayHeight - 12f));

                // Active Challenge Header
                if (hasActiveChallenge)
                {
                    GUILayout.Label($"<color=#FFDF78><b>DESAFÍO: {activeChallengeTitle}</b></color>", challengeHeaderStyle);
                    string dcDisplay = activeShowDC ? $"Dificultad Objetivo: <color=#66DDFF><b>DC {activeTargetDC}</b></color>" : "Dificultad: <color=#AAAAAA><i>Oculta por el DM</i></color>";
                    GUILayout.Label(dcDisplay, challengeHeaderStyle);
                }
                else
                {
                    GUILayout.Label("<color=#FFDF78><b>✦ BANDEJA DE DADOS D20 ✦</b></color>", challengeHeaderStyle);
                    GUILayout.Label("<color=#888888><i>Tirada libre de dados para rol</i></color>", challengeHeaderStyle);
                }

                GUILayout.Space(4);

                // D20 Animated Tumbling Die (Rotating, Bouncing, Procedural 3D Icosahedron)
                float dieVisualSize = 80f;
                Rect dieBoxRect = GUILayoutUtility.GetRect(dieVisualSize, dieVisualSize + 4f);
                Vector2 dieCenter = new Vector2(dieBoxRect.x + dieBoxRect.width * 0.5f, dieBoxRect.y + dieBoxRect.height * 0.5f + dieBounceY);

                Texture2D activeDieTex = d20NormalTex;
                if (!isRolling)
                {
                    if (finalRollResult == 20) activeDieTex = d20CritSuccessTex;
                    else if (finalRollResult == 1) activeDieTex = d20CritFailTex;
                }

                Matrix4x4 oldMat = GUI.matrix;
                GUIUtility.RotateAroundPivot(dieRotation, dieCenter);

                Rect dieDrawRect = new Rect(
                    dieCenter.x - dieVisualSize * 0.5f * dieScaleX,
                    dieCenter.y - dieVisualSize * 0.5f * dieScaleY,
                    dieVisualSize * dieScaleX,
                    dieVisualSize * dieScaleY
                );

                if (activeDieTex != null)
                {
                    GUI.DrawTexture(dieDrawRect, activeDieTex, ScaleMode.StretchToFill);
                }

                if (isRolling)
                {
                    // Tumbling motion glint in the center of the spinning facet (no numbers cycling)
                    GUI.Label(new Rect(dieCenter.x - 24f, dieCenter.y - 18f, 48f, 36f), "✦", d20MotionGlintStyle);
                }
                else
                {
                    // Settled number prominently displayed inside the front facet
                    string numColorHex = (finalRollResult == 20) ? "#FFDF78" : (finalRollResult == 1 ? "#FF5555" : "#EDE2C8");
                    GUI.Label(new Rect(dieCenter.x - 30f, dieCenter.y - 18f, 60f, 36f), $"<color={numColorHex}><b>{finalRollResult}</b></color>", d20CenteredNumberStyle);
                }

                GUI.matrix = oldMat;
                GUILayout.Space(4);

                // Roll Button
                bool isChallengeRollPending = hasActiveChallenge && !activeChallengeResolved;
                GUI.enabled = !isRolling;
                string rollBtnLabel = isRolling
                    ? "Rodando dado..."
                    : (isChallengeRollPending ? "✦ ¡LANZAR DADO DE PRUEBA! ✦" : "✦ ¡LANZAR 1D20! ✦");

                if (GUILayout.Button(rollBtnLabel, DarkFantasyPixelArtTheme.EmeraldButtonStyle, GUILayout.Height(28)))
                {
                    RollD20();
                }
                GUI.enabled = true;

                // DM Section: Challenge Composer (Host / Singleplayer only)
                if (NetworkSession.IsHost || NetworkSession.IsSinglePlayer)
                {
                    GUILayout.Space(6);
                    DarkFantasyPixelArtTheme.DrawImGuiDivider();
                    GUILayout.Label("<color=#FFDF78><b>Panel del Dungeon Master:</b></color>");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Prueba:", GUILayout.Width(52));
                    dmInputTitle = GUILayout.TextField(dmInputTitle, GUILayout.Width(130));
                    GUILayout.Label("DC:", GUILayout.Width(26));
                    dmInputDC = GUILayout.TextField(dmInputDC, GUILayout.Width(36));
                    GUILayout.EndHorizontal();

                    dmInputShowDC = GUILayout.Toggle(dmInputShowDC, "Mostrar DC a Jugadores");

                    GUILayout.Space(2);
                    int.TryParse(dmInputDC, out int parsedDC);
                    if (GUILayout.Button("✦ Solicitar Prueba a la Party ✦", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Height(26)))
                    {
                        IssueDMChallenge(dmInputTitle, parsedDC, dmInputShowDC);
                    }
                }

                GUILayout.EndArea();
            }

            // 3. BG3-Style Dramatic Outcome Banner
            if (showResultBanner)
            {
                float bWidth = 460f;
                float bHeight = 44f;
                float bX = (Screen.width - bWidth) * 0.5f;
                float currentTrayH = (NetworkSession.IsHost || NetworkSession.IsSinglePlayer) ? 355f : 230f;
                float bY = isTrayOpen ? (tabHeight + currentTrayH + 10f) : (tabHeight + 12f);
                Rect bRect = new Rect(bX, bY, bWidth, bHeight);

                GUI.Box(bRect, GUIContent.none, DarkFantasyPixelArtTheme.SectionBannerStyle ?? GUI.skin.box);
                GUILayout.BeginArea(bRect);
                GUILayout.Space(4);

                string outcomeText;
                if (lastResult.isCriticalSuccess)
                    outcomeText = $"<color=#FFDF78><b>✦ ¡ÉXITO CRÍTICO! NAT 20 ✦</b></color>\n<size=11>{lastResult.rollerName} supera de forma épica la prueba: {lastResult.challengeTitle}</size>";
                else if (lastResult.isCriticalFail)
                    outcomeText = $"<color=#FF4444><b>☠ ¡FALLO CRÍTICO! NAT 1 ☠</b></color>\n<size=11>{lastResult.rollerName} comete una pifia fatal en: {lastResult.challengeTitle}</size>";
                else if (lastResult.passed)
                    outcomeText = $"<color=#55FF55><b>✦ ¡PRUEBA SUPERADA! (Sacó {lastResult.rollResult}) ✦</b></color>\n<size=11>{lastResult.rollerName} ha tenido éxito en: {lastResult.challengeTitle}</size>";
                else
                    outcomeText = $"<color=#FF7777><b>✦ PRUEBA FALLIDA (Sacó {lastResult.rollResult}) ✦</b></color>\n<size=11>{lastResult.rollerName} no alcanzó el objetivo en: {lastResult.challengeTitle}</size>";

                GUILayout.Label(outcomeText, bannerStyle);
                GUILayout.EndArea();
            }
        }

        private void InitStyles()
        {
            if (d20CenteredNumberStyle == null)
            {
                d20CenteredNumberStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
            }

            if (d20MotionGlintStyle == null)
            {
                d20MotionGlintStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    normal = { textColor = new Color(1f, 0.88f, 0.45f, 0.85f) }
                };
            }

            if (challengeHeaderStyle == null)
            {
                challengeHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    richText = true
                };
            }

            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    richText = true
                };
            }

            if (tabButtonStyle == null)
            {
                tabButtonStyle = new GUIStyle(DarkFantasyPixelArtTheme.GoldButtonStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
            }

            // Generate procedural pixel-art D20 icosahedron textures
            if (d20NormalTex == null)
            {
                d20NormalTex = GenerateD20Texture(
                    new Color(0.18f, 0.16f, 0.24f, 1f),
                    new Color(0.85f, 0.69f, 0.22f, 1f),
                    new Color(0.11f, 0.09f, 0.16f, 1f)
                );
            }

            if (d20CritSuccessTex == null)
            {
                d20CritSuccessTex = GenerateD20Texture(
                    new Color(0.65f, 0.50f, 0.12f, 1f),
                    new Color(1.0f, 0.92f, 0.55f, 1f),
                    new Color(0.45f, 0.35f, 0.08f, 1f)
                );
            }

            if (d20CritFailTex == null)
            {
                d20CritFailTex = GenerateD20Texture(
                    new Color(0.45f, 0.10f, 0.12f, 1f),
                    new Color(0.92f, 0.30f, 0.30f, 1f),
                    new Color(0.30f, 0.06f, 0.08f, 1f)
                );
            }
        }

        private Texture2D GenerateD20Texture(Color baseColor, Color borderColor, Color centerColor)
        {
            int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rOuter = size * 0.44f;
            float rInner = rOuter * 0.54f;

            Vector2[] outerPts = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad;
                outerPts[i] = new Vector2(center.x + rOuter * Mathf.Cos(angle), center.y + rOuter * Mathf.Sin(angle));
            }

            Vector2[] innerPts = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f + 90f) * Mathf.Deg2Rad;
                innerPts[i] = new Vector2(center.x + rInner * Mathf.Cos(angle), center.y + rInner * Mathf.Sin(angle));
            }

            List<Vector2[]> segments = new List<Vector2[]>();
            for (int i = 0; i < 6; i++)
                segments.Add(new Vector2[] { outerPts[i], outerPts[(i + 1) % 6] });
            for (int i = 0; i < 3; i++)
                segments.Add(new Vector2[] { innerPts[i], innerPts[(i + 1) % 3] });
            for (int i = 0; i < 6; i++)
                segments.Add(new Vector2[] { outerPts[i], innerPts[i / 2] });

            float[] facetMultipliers = new float[] { 1.18f, 0.92f, 0.72f, 0.62f, 0.82f, 1.28f };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    bool inCenter = IsPointInTriangle(pt, innerPts[0], innerPts[1], innerPts[2]);
                    float facetMul = -1f;

                    if (!inCenter)
                    {
                        for (int f = 0; f < 6; f++)
                        {
                            if (IsPointInTriangle(pt, outerPts[f], outerPts[(f + 1) % 6], innerPts[f / 2]))
                            {
                                facetMul = facetMultipliers[f];
                                break;
                            }
                        }
                    }

                    if (!inCenter && facetMul < 0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    Color fill = inCenter ? centerColor : (baseColor * facetMul);
                    fill.a = 1f;

                    float minD = 999f;
                    for (int s = 0; s < segments.Count; s++)
                    {
                        float d = DistancePointToSegment(pt, segments[s][0], segments[s][1]);
                        if (d < minD) minD = d;
                    }

                    if (minD <= 1.2f)
                    {
                        pixels[y * size + x] = borderColor;
                    }
                    else if (minD <= 1.8f)
                    {
                        float t = (minD - 1.2f) / 0.6f;
                        pixels[y * size + x] = Color.Lerp(borderColor, fill, t);
                    }
                    else
                    {
                        pixels[y * size + x] = fill;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 == 0f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 proj = a + t * ab;
            return Vector2.Distance(p, proj);
        }

        private static float TriangleSign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1 = TriangleSign(pt, v1, v2);
            float d2 = TriangleSign(pt, v2, v3);
            float d3 = TriangleSign(pt, v3, v1);
            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private string GetLocalPlayerName()
        {
            string pName = "";
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                pName = GameManager.Instance.PlayerEntity.Name;

            if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
            {
                if (DFUNetworkManager.Instance != null)
                {
                    if (!string.IsNullOrEmpty(DFUNetworkManager.Instance.localLobbyCharacterName) && DFUNetworkManager.Instance.localLobbyCharacterName != "Nameless")
                        pName = DFUNetworkManager.Instance.localLobbyCharacterName;
                    else if (!string.IsNullOrEmpty(DFUNetworkManager.Instance.localLobbyPlayerName))
                        pName = DFUNetworkManager.Instance.localLobbyPlayerName;
                }

                if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
                {
                    string pref = PlayerPrefs.GetString("DFU_LastPlayerName", "");
                    if (!string.IsNullOrEmpty(pref) && pref != "Nameless")
                        pName = pref;
                }

                if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
                {
                    pName = (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm) ? "Dungeon Master" : "Aventurero";
                }

                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                    GameManager.Instance.PlayerEntity.Name = pName;
            }

            return pName;
        }

        #endregion
    }
}
