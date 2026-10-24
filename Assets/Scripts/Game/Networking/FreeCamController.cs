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
        public Transform OriginalCameraParent => originalCameraParent;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;

        public void SetCameraPositionAndFacing(Vector3 pos, float yaw, float pitch)
        {
            if (cameraTransform != null)
            {
                cameraTransform.position = pos;
                cameraYaw = yaw;
                cameraPitch = pitch;
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            }
        }

        private GameObject localPuppet = null;
        private Transform cameraTransform = null;
        private HeadBobber playerHeadBobber = null;
        private PlayerHeightChanger playerHeightChanger = null;
        private PlayerMouseLook playerMouseLook = null;
        private PlayerMotor playerMotor = null;
        private static bool isQuitting = false;
        private bool wasWeaponShown = true;

        [Header("FreeCam Movement")]
        public float flySpeed = 15f;
        public float sprintMultiplier = 3.5f;
        public float lookSensitivity = 2.0f;
        public float mouseWheelSpeedSensitivity = 15f;

        private float cameraPitch = 0f;
        private float cameraYaw = 0f;
        public float CameraPitch => cameraPitch;
        public float CameraYaw => cameraYaw;
        public Vector3 CameraPosition => cameraTransform != null ? cameraTransform.position : Vector3.zero;

        [Header("Tactical Top-Down View")]
        private bool isTopDownView = false;
        public bool IsTopDownView => isTopDownView;
        private float savedFreeCamPitch = 0f;
        public float tacticalZoomSpeed = 10f;

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
            // Ctrl + F8 hotkey toggle (handled by DFUNetworkManager if present to coordinate DM dual-entity mode)
            if (DFUNetworkManager.Instance == null)
            {
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F8))
                {
                    ToggleFreeCam();
                    return;
                }
            }

            if (!isActive || cameraTransform == null)
                return;

            // Suspend FreeCam camera updates while DM is possessing an entity
            if (DMNpcController.Instance != null && DMNpcController.Instance.IsPossessing)
                return;

            // Hotkey 'B' toggles Tactical Cenital / Bird's Eye View while in flight/FreeCam (conflict-free key)
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleTopDownView();
                return;
            }

            // Click-to-Teleport: Shift + Left Click on any ground/floor surface
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButtonDown(0))
            {
                PerformClickToTeleport();
                return;
            }

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

            // Click into game window to recapture cursor if lost (only in 6-DOF FreeCam, not in Top-Down view)
            if (!isTopDownView && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    if (Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            // Speed or Altitude zoom via mouse wheel
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.001f)
                scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                if (isTopDownView)
                {
                    // Zoom altitude in and out smoothly
                    float zoomDelta = -Mathf.Sign(scroll) * tacticalZoomSpeed;
                    Vector3 newPos = cameraTransform.position + Vector3.up * zoomDelta;
                    if (newPos.y < 3f) newPos.y = 3f;
                    cameraTransform.position = newPos;
                    DaggerfallUI.AddHUDText($"[Tactical View] Altitude: {cameraTransform.position.y:F0}m");
                }
                else
                {
                    flySpeed = Mathf.Clamp(flySpeed + Mathf.Sign(scroll) * 5f, 2f, 150f);
                    DaggerfallUI.AddHUDText($"[FreeCam] Speed: {flySpeed:F0} m/s");
                }
            }

            // Camera orientation
            if (isTopDownView)
            {
                // Lock pitch directly downward (89.9f prevents gimbal lock)
                cameraPitch = 89.9f;
                cameraTransform.rotation = Quaternion.Euler(89.9f, cameraYaw, 0f);

                // Right click drag allows rotating yaw
                if (Input.GetMouseButton(1))
                {
                    float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
                    cameraYaw += mouseX;
                    cameraTransform.rotation = Quaternion.Euler(89.9f, cameraYaw, 0f);
                }
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

                cameraYaw += mouseX;
                cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -89f, 89f);
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            }

            // Movement calculation (WASD, Space, C)
            float currentSpeed = flySpeed;
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !Input.GetMouseButton(0))
                currentSpeed *= sprintMultiplier;

            Vector3 moveDir = Vector3.zero;

            if (isTopDownView)
            {
                // Planar panning along ground plane relative to compass yaw
                Vector3 forwardPlanar = Quaternion.Euler(0, cameraYaw, 0) * Vector3.forward;
                Vector3 rightPlanar = Quaternion.Euler(0, cameraYaw, 0) * Vector3.right;

                if (Input.GetKey(KeyCode.W)) moveDir += forwardPlanar;
                if (Input.GetKey(KeyCode.S)) moveDir -= forwardPlanar;
                if (Input.GetKey(KeyCode.D)) moveDir += rightPlanar;
                if (Input.GetKey(KeyCode.A)) moveDir -= rightPlanar;
            }
            else
            {
                if (Input.GetKey(KeyCode.W)) moveDir += cameraTransform.forward;
                if (Input.GetKey(KeyCode.S)) moveDir -= cameraTransform.forward;
                if (Input.GetKey(KeyCode.D)) moveDir += cameraTransform.right;
                if (Input.GetKey(KeyCode.A)) moveDir -= cameraTransform.right;
            }

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

            // Cache and freeze player motor & mouse look directly without GameManager lookups
            if (mainCam != null)
                playerMouseLook = mainCam.GetComponent<PlayerMouseLook>();
            if (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                playerMotor = GameManager.Instance.PlayerObject.GetComponent<PlayerMotor>();

            if (playerMotor != null)
            {
                playerMotor.CancelMovement = true;
                playerMotor.enabled = false;
            }

            if (playerMouseLook != null)
            {
                playerMouseLook.enableMouseLook = false;
                playerMouseLook.enabled = false;
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

            // Restore screen weapon, player motor & mouse look safely without GameManager lookups
            try
            {
                if (GameManager.HasInstance && GameManager.Instance.WeaponManager != null && GameManager.Instance.WeaponManager.ScreenWeapon != null)
                {
                    GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = wasWeaponShown;
                }

                if (playerMotor != null)
                {
                    playerMotor.enabled = true;
                    playerMotor.CancelMovement = true;
                    playerMotor = null;
                }

                if (playerMouseLook != null)
                {
                    playerMouseLook.enabled = true;
                    playerMouseLook.enableMouseLook = true;
                    playerMouseLook.SetFacing(cameraYaw, cameraPitch);
                    playerMouseLook = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FreeCam] Safe teardown warning: {ex.Message}");
            }

            // Destroy local puppet
            DestroyLocalPuppet();

            isActive = false;
            isTopDownView = false;
            DaggerfallUI.AddHUDText("[Eagle Eye] FreeCam OFF. Camera restored to player.");
        }

        public void ToggleTopDownView()
        {
            if (!isActive || cameraTransform == null)
                return;

            // Suspend FreeCam camera updates while DM is possessing an entity
            if (DMNpcController.Instance != null && DMNpcController.Instance.IsPossessing)
                return;

            isTopDownView = !isTopDownView;

            if (isTopDownView)
            {
                savedFreeCamPitch = cameraPitch;
                cameraPitch = 89.9f;
                cameraTransform.rotation = Quaternion.Euler(89.9f, cameraYaw, 0f);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                DaggerfallUI.AddHUDText("[Tactical View] Top-Down Cenital ACTIVE (WASD: Pan, Scroll: Zoom, Shift+LClick: Teleport, B: Exit)");
            }
            else
            {
                cameraPitch = savedFreeCamPitch;
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

                if (Application.isFocused)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                DaggerfallUI.AddHUDText("[Tactical View] FreeCam 6-DOF ACTIVE (B: Bird's Eye Cenital, Shift+LClick: Teleport)");
            }
        }

        private void PerformClickToTeleport()
        {
            if (cameraTransform == null)
                return;

            Ray ray;
            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;

            if (isTopDownView || Cursor.lockState != CursorLockMode.Locked)
            {
                if (mainCam != null)
                    ray = mainCam.ScreenPointToRay(Input.mousePosition);
                else
                    ray = new Ray(cameraTransform.position, cameraTransform.forward);
            }
            else
            {
                ray = new Ray(cameraTransform.position, cameraTransform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f))
            {
                Vector3 groundTarget = hit.point + Vector3.up * 0.15f;

                // Move local player entity safely using elevation safeguards
                if (DFUNetworkManager.Instance != null)
                {
                    DFUNetworkManager.Instance.RelocateLocalPlayerToCamera(groundTarget, cameraYaw);
                }
                else if (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                {
                    GameManager.Instance.PlayerObject.transform.position = groundTarget;
                }

                // Update local puppet position if active
                if (localPuppet != null && GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                {
                    localPuppet.transform.position = GameManager.Instance.PlayerObject.transform.position;
                }

                // In top-down view, center the camera over the target while maintaining altitude
                if (isTopDownView)
                {
                    cameraTransform.position = new Vector3(hit.point.x, cameraTransform.position.y, hit.point.z);
                }
                else
                {
                    // In freecam, relocate camera to just above the teleported position
                    cameraTransform.position = hit.point + Vector3.up * 2.0f;
                }

                DaggerfallUI.AddHUDText($"[DM Teleport] Teleported to ({hit.point.x:F1}, {hit.point.y:F1}, {hit.point.z:F1})");
            }
            else
            {
                DaggerfallUI.AddHUDText("[DM Teleport] No surface targeted (aim at terrain, floor or structure).");
            }
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
                    yaw = player != null ? player.transform.eulerAngles.y : 0f,
                    isDm = (NetworkSession.IsServer || (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm))
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

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDisable()
        {
            if (isQuitting)
            {
                isActive = false;
                return;
            }

            if (isActive)
                Deactivate();
        }

        private void OnDestroy()
        {
            if (isQuitting)
            {
                isActive = false;
                return;
            }

            if (isActive)
                Deactivate();
        }
    }
}
