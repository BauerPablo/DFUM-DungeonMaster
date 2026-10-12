// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Eagle Eye / FreeCam controller for Dungeon Masters and observers.
    /// Detaches the main camera, freezes player motor and mouse look,
    /// spawns a local character puppet at the player position,
    /// and allows smooth 6-DOF flying in 3D space with WASD, Space/Ctrl, Sprint, and mouse look.
    /// </summary>
    public class FreeCamController : MonoBehaviour
    {
        public static FreeCamController Instance { get; private set; }

        private bool isActive = false;
        public bool IsActive => isActive;

        private Transform originalCameraParent = null;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;

        private GameObject localPuppet = null;
        private Transform cameraTransform = null;
        private HeadBobber playerHeadBobber = null;
        private PlayerHeightChanger playerHeightChanger = null;
        private bool wasWeaponShown = true;

        [Header("FreeCam Movement")]
        public float flySpeed = 15f;
        public float sprintMultiplier = 3.5f;
        public float lookSensitivity = 2.0f;
        public float mouseWheelSpeedSensitivity = 15f;

        private float cameraPitch = 0f;
        private float cameraYaw = 0f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCamera != null)
                cameraTransform = GameManager.Instance.MainCamera.transform;
        }

        private void Update()
        {
            // Ctrl + F8 hotkey toggle (F8 alone is reserved by DFU for HUD / debug)
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F8))
            {
                ToggleFreeCam();
                return;
            }

            if (!isActive || cameraTransform == null)
                return;

            // Do not capture controls if a modal UI window is open (DaggerfallHUD is normal gameplay window)
            bool isModalWindowOpen = DaggerfallUI.UIManager != null &&
                                     DaggerfallUI.UIManager.TopWindow != null &&
                                     !(DaggerfallUI.UIManager.TopWindow is DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallHUD);

            bool isAnyMenuOpen = isModalWindowOpen ||
                                 (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.IsAnyGuiOpen);

            if (!Application.isFocused || isAnyMenuOpen)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            // Click into game window to recapture cursor if lost
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // Speed adjustment via mouse wheel (using direct mouseScrollDelta with Input.GetAxis fallback)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.001f)
                scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                flySpeed = Mathf.Clamp(flySpeed + Mathf.Sign(scroll) * 5f, 2f, 150f);
                DaggerfallUI.AddHUDText($"[FreeCam] Speed: {flySpeed:F0} m/s");
            }

            // Mouse look when cursor is locked
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

                cameraYaw += mouseX;
                cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -89f, 89f);
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            }

            // Movement calculation (WASD, Space, C)
            float currentSpeed = flySpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                currentSpeed *= sprintMultiplier;

            Vector3 moveDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) moveDir += cameraTransform.forward;
            if (Input.GetKey(KeyCode.S)) moveDir -= cameraTransform.forward;
            if (Input.GetKey(KeyCode.D)) moveDir += cameraTransform.right;
            if (Input.GetKey(KeyCode.A)) moveDir -= cameraTransform.right;

            // Up / Down (Space / C - Ctrl removed to avoid shortcut conflicts)
            if (Input.GetKey(KeyCode.Space)) moveDir += Vector3.up;
            if (Input.GetKey(KeyCode.C)) moveDir += Vector3.down;

            cameraTransform.position += moveDir * (currentSpeed * Time.unscaledDeltaTime);
        }

        public void ToggleFreeCam()
        {
            if (isActive)
                Deactivate();
            else
                Activate();
        }

        public void Activate()
        {
            if (isActive)
                return;

            if (GameManager.Instance == null || GameManager.Instance.MainCameraObject == null)
            {
                DaggerfallUI.AddHUDText("[Eagle Eye] Unable to activate: Camera not ready.");
                return;
            }

            Camera mainCam = GameManager.Instance.MainCamera;
            if (mainCam == null)
                return;

            cameraTransform = mainCam.transform;
            originalCameraParent = cameraTransform.parent;
            originalCameraLocalPos = cameraTransform.localPosition;
            originalCameraLocalRot = cameraTransform.localRotation;

            // Calculate starting orientation
            cameraPitch = cameraTransform.eulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
            cameraYaw = cameraTransform.eulerAngles.y;

            // Detach camera from player hierarchy at its current world position
            Vector3 startWorldPos = cameraTransform.position;
            cameraTransform.SetParent(null);
            cameraTransform.position = startWorldPos;

            // Disable HeadBobber & PlayerHeightChanger so they do not continuously force localPosition to (0, 0.7, 0)
            if (GameManager.Instance.PlayerObject != null)
            {
                playerHeadBobber = GameManager.Instance.PlayerObject.GetComponent<HeadBobber>();
                if (playerHeadBobber != null)
                    playerHeadBobber.enabled = false;

                playerHeightChanger = GameManager.Instance.PlayerObject.GetComponent<PlayerHeightChanger>();
                if (playerHeightChanger != null)
                    playerHeightChanger.enabled = false;
            }

            // Hide first-person screen weapon during eagle eye
            if (GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null)
            {
                wasWeaponShown = GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon;
                GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = false;
            }

            // Freeze player motor & mouse look
            if (GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.CancelMovement = true;
                GameManager.Instance.PlayerMotor.enabled = false;
            }

            if (GameManager.Instance.PlayerMouseLook != null)
            {
                GameManager.Instance.PlayerMouseLook.enableMouseLook = false;
                GameManager.Instance.PlayerMouseLook.enabled = false;
            }

            if (Application.isFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Spawn local character puppet
            SpawnLocalCharacterPuppet();

            isActive = true;
            DaggerfallUI.AddHUDText($"[Eagle Eye] FreeCam ON (Speed: {flySpeed:F0}m/s. WASD/Space/Ctrl fly, Mouse look, Wheel speed, Ctrl+F8 exit)");
        }

        public void Deactivate()
        {
            if (!isActive)
                return;

            // Re-parent camera back to original player anchor
            if (cameraTransform != null)
            {
                cameraTransform.SetParent(originalCameraParent);
                cameraTransform.localPosition = originalCameraLocalPos;
                cameraTransform.localRotation = originalCameraLocalRot;
            }

            // Restore HeadBobber and PlayerHeightChanger
            if (playerHeadBobber != null)
            {
                playerHeadBobber.enabled = true;
                playerHeadBobber = null;
            }

            if (playerHeightChanger != null)
            {
                playerHeightChanger.enabled = true;
                playerHeightChanger = null;
            }

            // Restore screen weapon
            if (GameManager.Instance != null && GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null)
            {
                GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = wasWeaponShown;
            }

            // Destroy local puppet
            DestroyLocalPuppet();

            // Re-enable player motor & mouse look
            if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
            {
                GameManager.Instance.PlayerMotor.enabled = true;
                GameManager.Instance.PlayerMotor.CancelMovement = true;
            }

            if (GameManager.Instance != null && GameManager.Instance.PlayerMouseLook != null)
            {
                GameManager.Instance.PlayerMouseLook.enabled = true;
                GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
                GameManager.Instance.PlayerMouseLook.SetFacing(cameraYaw, cameraPitch);
            }

            isActive = false;
            DaggerfallUI.AddHUDText("[Eagle Eye] FreeCam OFF. Camera restored to player.");
        }

        private void SpawnLocalCharacterPuppet()
        {
            DestroyLocalPuppet();

            MsgPlayerProfile profile;
            if (DFUNetworkManager.Instance != null)
            {
                profile = DFUNetworkManager.Instance.BuildLocalPlayerProfile(99999);
            }
            else
            {
                GameObject player = GameManager.Instance.PlayerObject;
                profile = new MsgPlayerProfile
                {
                    connectionId = 99999,
                    playerName = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Name : "Player",
                    gender = GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.Gender == Entity.Genders.Male ? 0 : 1,
                    careerName = GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.Career != null ? GameManager.Instance.PlayerEntity.Career.Name : "Warrior",
                    raceName = GameManager.Instance.PlayerEntity != null ? GameManager.Instance.PlayerEntity.Race.ToString() : "Breton",
                    position = player != null ? player.transform.position : Vector3.zero,
                    yaw = player != null ? player.transform.eulerAngles.y : 0f
                };
            }

            profile.playerName = $"{profile.playerName} (You)";

            localPuppet = GameObjectHelper.CreateRemotePlayerProxy(profile);
            UpdatePuppetVisibility();
        }

        public void UpdatePuppetVisibility()
        {
            if (localPuppet == null)
                return;

            bool isInvisible = false;
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.isDmInvisible)
                isInvisible = true;
            else if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.IsInvisible)
                isInvisible = true;

            localPuppet.SetActive(!isInvisible);
        }

        private void DestroyLocalPuppet()
        {
            if (localPuppet != null)
            {
                Destroy(localPuppet);
                localPuppet = null;
            }
        }

        private void OnDisable()
        {
            if (isActive)
                Deactivate();
        }

        private void OnDestroy()
        {
            if (isActive)
                Deactivate();
        }
    }
}
