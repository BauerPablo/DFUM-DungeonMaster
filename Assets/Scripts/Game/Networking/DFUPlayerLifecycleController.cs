// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Networking
{
    public enum PlayerLifecycleState
    {
        Alive,
        Downed,
        Spectator
    }

    /// <summary>
    /// Phase 19 Anti-Desync Player Lifecycle Controller.
    /// Intercepts single-player 0 HP death sequences in multiplayer sessions,
    /// transitioning the player into a 3-minute Downed (bleeding out) state,
    /// enabling cooperative resuscitation via spells, potions, first-aid, or DM resurrect,
    /// and seamlessly transitioning into FreeCam Spectator mode upon bleedout timeout
    /// without triggering TitleMenuFromDeath, scene unloads, or network desynchronization.
    /// </summary>
    public class DFUPlayerLifecycleController : MonoBehaviour
    {
        public static DFUPlayerLifecycleController Instance { get; private set; }

        public const float MaxBleedoutSeconds = 180f;

        [Header("Lifecycle State")]
        [SerializeField] private PlayerLifecycleState currentState = PlayerLifecycleState.Alive;
        public PlayerLifecycleState CurrentState => currentState;
        public bool IsAlive => currentState == PlayerLifecycleState.Alive;
        public bool IsDowned => currentState == PlayerLifecycleState.Downed;
        public bool IsSpectator => currentState == PlayerLifecycleState.Spectator;
        public bool IsDownedOrSpectator => currentState != PlayerLifecycleState.Alive;

        [Header("Bleedout Countdown")]
        [SerializeField] private float bleedoutTimer = MaxBleedoutSeconds;
        public float BleedoutTimer => bleedoutTimer;

        [Header("Audio & Visuals")]
        private float heartbeatInterval = 1.3f;
        private float heartbeatTimer = 0f;
        private AudioSource localAudioSource;

        private float targetCameraHeight = 0.25f;
        private float savedCameraHeight = 1.6f;
        private bool cameraHeightSaved = false;

        private Texture2D redVignetteTex;
        private Texture2D blackOverlayTex;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.spatialBlend = 0f;
            localAudioSource.playOnAwake = false;

            CreateOverlayTextures();
        }

        private void OnDestroy()
        {
            if (redVignetteTex != null)
                Destroy(redVignetteTex);
            if (blackOverlayTex != null)
                Destroy(blackOverlayTex);
        }

        private void CreateOverlayTextures()
        {
            redVignetteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            redVignetteTex.SetPixel(0, 0, new Color(0.85f, 0.08f, 0.08f, 0.35f));
            redVignetteTex.Apply();

            blackOverlayTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blackOverlayTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            blackOverlayTex.Apply();
        }

        private void Update()
        {
            if (!NetworkSession.IsActive)
            {
                if (currentState != PlayerLifecycleState.Alive)
                    ResetLifecycleState();
                return;
            }

            if (currentState == PlayerLifecycleState.Downed)
            {
                UpdateDownedState();
            }
        }

        private void UpdateDownedState()
        {
            // Cancel motor motion and weapon swings
            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = true;
                GameManager.Instance.PlayerMotor.FreezeMotor = 0.5f;
            }

            if (GameManager.Instance != null && GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null)
            {
                GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = false;
            }

            // Lower camera smoothly to floor level
            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam != null)
            {
                Vector3 camPos = mainCam.transform.localPosition;
                if (camPos.y > targetCameraHeight)
                {
                    camPos.y = Mathf.MoveTowards(camPos.y, targetCameraHeight, 2.5f * Time.deltaTime);
                    mainCam.transform.localPosition = camPos;
                }
            }

            // Heartbeat audio pulse
            bleedoutTimer -= Time.deltaTime;
            heartbeatInterval = Mathf.Lerp(0.45f, 1.4f, Mathf.Clamp01(bleedoutTimer / MaxBleedoutSeconds));
            heartbeatTimer += Time.deltaTime;

            if (heartbeatTimer >= heartbeatInterval)
            {
                heartbeatTimer = 0f;
                PlayHeartbeatSound();
            }

            // Check bleedout expiration
            if (bleedoutTimer <= 0f)
            {
                bleedoutTimer = 0f;
                TriggerLocalPlayerPermaDeath();
            }
        }

        private void PlayHeartbeatSound()
        {
            try
            {
                if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.DaggerfallAudioSource != null)
                {
                    // Use subtle audio cue for heartbeat
                    DaggerfallUI.Instance.DaggerfallAudioSource.PlayOneShot(SoundClips.PainSound, 0.45f);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Triggered when the local player's health drops <= 0 in an active multiplayer session.
        /// </summary>
        public void TriggerLocalPlayerDowned()
        {
            if (currentState != PlayerLifecycleState.Alive)
                return;

            currentState = PlayerLifecycleState.Downed;
            bleedoutTimer = MaxBleedoutSeconds;
            heartbeatTimer = 0f;

            // Cache original camera height
            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam != null && !cameraHeightSaved)
            {
                savedCameraHeight = mainCam.transform.localPosition.y;
                cameraHeightSaved = true;
            }

            // Pin health at 0
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
            {
                GameManager.Instance.PlayerEntity.CurrentHealth = 0;
            }

            // Disallow movement
            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }

            int myConnId = NetworkSession.IsHost ? 0 : (DFUNetworkManager.Instance != null ? DFUNetworkManager.Instance.localConnectionId : 0);
            string myName = GameManager.Instance != null && GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player";
            Vector3 myPos = GameManager.Instance != null && GameManager.Instance.PlayerObject != null ? GameManager.Instance.PlayerObject.transform.position : Vector3.zero;

            MsgPlayerDowned msg = new MsgPlayerDowned
            {
                connectionId = myConnId,
                playerName = myName,
                position = myPos,
                bleedoutSeconds = MaxBleedoutSeconds
            };

            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(msg);
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(msg);
            }

            if (DMChatController.Instance != null)
            {
                DMChatController.Instance.AddLocalSystemMessage($"[Party] ¡{myName} ha caído inconsciente y necesita reanimación!");
            }

            DaggerfallUI.AddHUDText($"[INCONSCIENTE] Has caído en combate. Desangrándote: 03:00");
            Debug.Log($"[DFU Lifecycle] Local player {myName} (conn {myConnId}) is DOWNED. Bleedout: {MaxBleedoutSeconds}s.");
        }

        /// <summary>
        /// Restores player from Downed or Spectator state back to Alive.
        /// </summary>
        public void ReviveLocalPlayer(string healerName, int restoredHealth)
        {
            if (currentState == PlayerLifecycleState.Alive)
                return;

            currentState = PlayerLifecycleState.Alive;
            bleedoutTimer = MaxBleedoutSeconds;

            // Restore camera height
            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam != null && cameraHeightSaved)
            {
                Vector3 camPos = mainCam.transform.localPosition;
                camPos.y = savedCameraHeight;
                mainCam.transform.localPosition = camPos;
                cameraHeightSaved = false;
            }

            // Restore player health authoritatively
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
            {
                int hpToSet = Mathf.Clamp(restoredHealth, 1, GameManager.Instance.PlayerEntity.MaxHealth);
                GameManager.Instance.PlayerEntity.SetHealth(hpToSet, restoreMode: true);
                GameManager.Instance.PlayerEntity.NoTargetMode = false;
                GameManager.Instance.PlayerEntity.MagicalConcealmentFlags &= ~MagicalConcealmentFlags.InvisibleTrue;
            }

            // Restore player controller and motor
            if (GameManager.Instance != null && GameManager.Instance.PlayerController != null)
            {
                GameManager.Instance.PlayerController.detectCollisions = true;
                GameManager.Instance.PlayerController.enabled = true;
            }

            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = false;
                GameManager.Instance.PlayerMotor.FreezeMotor = 0f;
            }

            // Restore screen weapon
            if (GameManager.Instance != null && GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null)
            {
                GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = true;
            }

            // Deactivate freecam if in spectator
            if (FreeCamController.Instance != null && FreeCamController.Instance.IsActive)
            {
                FreeCamController.Instance.Deactivate();
            }

            string notification = $"¡Has sido reanimado por {healerName}! (+{restoredHealth} HP)";
            DaggerfallUI.AddHUDText(notification);
            if (DMChatController.Instance != null)
            {
                DMChatController.Instance.AddLocalSystemMessage($"[Party] {notification}");
            }
            Debug.Log($"[DFU Lifecycle] Local player revived by {healerName} with {restoredHealth} HP.");
        }

        /// <summary>
        /// Called when bleedout reaches 00:00. Spawns permanent corpse and transitions to spectator.
        /// </summary>
        public void TriggerLocalPlayerPermaDeath()
        {
            if (currentState == PlayerLifecycleState.Spectator)
                return;

            currentState = PlayerLifecycleState.Spectator;

            int myConnId = NetworkSession.IsHost ? 0 : (DFUNetworkManager.Instance != null ? DFUNetworkManager.Instance.localConnectionId : 0);
            string myName = GameManager.Instance != null && GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player";
            Vector3 myPos = GameManager.Instance != null && GameManager.Instance.PlayerObject != null ? GameManager.Instance.PlayerObject.transform.position : Vector3.zero;
            float myYaw = GameManager.Instance != null && GameManager.Instance.PlayerObject != null ? GameManager.Instance.PlayerObject.transform.eulerAngles.y : 0f;

            int corpseTex = 401;
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.Career != null)
            {
                if (EnemyBasics.GetEnemy(GameObjectHelper.ResolvePlayerClassMobileType(GameManager.Instance.PlayerEntity.Career.Name), out MobileEnemy enemy))
                {
                    bool isFemale = GameManager.Instance.PlayerEntity.Gender == Genders.Female;
                    corpseTex = (isFemale && enemy.FemaleCorpseTexture != 0) ? enemy.FemaleCorpseTexture : enemy.CorpseTexture;
                }
            }

            MsgPlayerPermaDeath msg = new MsgPlayerPermaDeath
            {
                connectionId = myConnId,
                playerName = myName,
                position = myPos,
                yaw = myYaw,
                corpseTexture = corpseTex
            };

            if (NetworkServer.active)
            {
                NetworkServer.SendToAll(msg);
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(msg);
            }

            // Spawn permanent corpse marker locally
            SpawnPermanentCorpseMarker(myPos, myYaw, corpseTex);

            // Activate FreeCam Spectator mode
            if (FreeCamController.Instance != null)
            {
                FreeCamController.Instance.ActivateSpectatorMode();
            }

            string notification = $"[Party] ¡{myName} ha sucumbido a sus heridas y ahora es un espectador!";
            if (DMChatController.Instance != null)
            {
                DMChatController.Instance.AddLocalSystemMessage(notification);
            }
            DaggerfallUI.AddHUDText("[ESPECTADOR] Bleedout terminado. Eres un espectador libre. El chat sigue activo.");
            Debug.Log($"[DFU Lifecycle] Local player {myName} died permanently. Transitioned to FreeCam spectator.");
        }

        public void SpawnPermanentCorpseMarker(Vector3 pos, float yaw, int corpseTex)
        {
            try
            {
                EnemyBasics.ReverseCorpseTexture(corpseTex, out int archive, out int record);
                GameObject corpseObj = new GameObject("PlayerCorpseMarker");
                corpseObj.transform.position = pos;
                corpseObj.transform.rotation = Quaternion.Euler(0, yaw, 0);

                DaggerfallBillboard billboard = corpseObj.AddComponent<DaggerfallBillboard>();
                billboard.SetMaterial(archive, record);
                billboard.AlignToBase();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU Lifecycle] Could not spawn corpse marker: {ex.Message}");
            }
        }

        /// <summary>
        /// Called by PlayerEntity.SetHealth when healing is received while in the Downed state.
        /// </summary>
        public void OnLocalPlayerHealthRestored(int restoredHp)
        {
            if (currentState == PlayerLifecycleState.Downed)
            {
                int myConnId = NetworkSession.IsHost ? 0 : (DFUNetworkManager.Instance != null ? DFUNetworkManager.Instance.localConnectionId : 0);
                string myName = GameManager.Instance != null && GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player";

                ReviveLocalPlayer("Magia / Curación", restoredHp);

                // Notify peers of authoritative revival
                MsgPlayerRevive msg = new MsgPlayerRevive
                {
                    targetConnectionId = myConnId,
                    revivedPlayerName = myName,
                    healerPlayerName = "Magia / Curación",
                    restoredHealth = restoredHp
                };

                if (NetworkServer.active)
                {
                    NetworkServer.SendToAll(msg);
                }
                else if (NetworkClient.active)
                {
                    NetworkClient.Send(msg);
                }
            }
        }

        /// <summary>
        /// Resets lifecycle state back to standard offline single-player.
        /// </summary>
        public void ResetLifecycleState()
        {
            currentState = PlayerLifecycleState.Alive;
            bleedoutTimer = MaxBleedoutSeconds;

            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam != null && cameraHeightSaved)
            {
                Vector3 camPos = mainCam.transform.localPosition;
                camPos.y = savedCameraHeight;
                mainCam.transform.localPosition = camPos;
                cameraHeightSaved = false;
            }

            if (FreeCamController.Instance != null && FreeCamController.Instance.IsActive)
            {
                FreeCamController.Instance.Deactivate();
            }
        }

        private void OnGUI()
        {
            if (!NetworkSession.IsActive)
                return;

            if (currentState == PlayerLifecycleState.Downed)
            {
                DrawDownedVignetteAndHUD();
            }
            else if (currentState == PlayerLifecycleState.Spectator)
            {
                DrawSpectatorHUD();
            }
        }

        private void DrawDownedVignetteAndHUD()
        {
            float w = Screen.width;
            float h = Screen.height;

            // Pulsing screen border vignette
            float pulse = Mathf.PingPong(Time.time * 2.5f, 0.45f) + 0.15f;
            Color prevColor = GUI.color;
            GUI.color = new Color(0.85f, 0.05f, 0.05f, pulse);

            float borderThickness = 18f;
            if (redVignetteTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, w, borderThickness), redVignetteTex);
                GUI.DrawTexture(new Rect(0, h - borderThickness, w, borderThickness), redVignetteTex);
                GUI.DrawTexture(new Rect(0, 0, borderThickness, h), redVignetteTex);
                GUI.DrawTexture(new Rect(w - borderThickness, 0, borderThickness, h), redVignetteTex);
            }

            GUI.color = prevColor;

            // Central Downed Countdown Banner
            float bannerWidth = 460f;
            float bannerHeight = 82f;
            Rect bannerRect = new Rect((w - bannerWidth) * 0.5f, 24f, bannerWidth, bannerHeight);

            if (blackOverlayTex != null)
            {
                GUI.DrawTexture(bannerRect, blackOverlayTex);
            }

            int minutes = Mathf.FloorToInt(bleedoutTimer / 60f);
            int seconds = Mathf.FloorToInt(bleedoutTimer % 60f);
            string timeString = $"{minutes:00}:{seconds:00}";

            GUILayout.BeginArea(bannerRect, GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Label($"<color=#FF4444><b>[INCONSCIENTE]</b></color> <color=#FFDF78>Desangrándote: <b>{timeString}</b></color>", DarkFantasyPixelArtTheme.SectionBannerStyle ?? GUI.skin.label);
            GUILayout.Space(2);
            GUILayout.Label("<size=12><color=#E0D0B0>Pide auxilio por el chat (<b>/party</b> o <b>Enter</b>). Un aliado puede reanimarte con Primeros Auxilios, Poción o Hechizo.</color></size>");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawSpectatorHUD()
        {
            float w = Screen.width;
            float bannerWidth = 480f;
            float bannerHeight = 54f;
            Rect bannerRect = new Rect((w - bannerWidth) * 0.5f, 18f, bannerWidth, bannerHeight);

            if (blackOverlayTex != null)
            {
                GUI.DrawTexture(bannerRect, blackOverlayTex);
            }

            GUILayout.BeginArea(bannerRect, GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Label("<color=#70D0FF><b>✦ MODO ESPECTADOR (VUELO LIBRE) ✦</b></color>", DarkFantasyPixelArtTheme.SectionBannerStyle ?? GUI.skin.label);
            GUILayout.Label("<size=11><color=#C0C0C0>Vuela libre con <b>WASD / Espacio / C</b>. Presiona <b>Enter</b> para chatear con la party.</color></size>");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
