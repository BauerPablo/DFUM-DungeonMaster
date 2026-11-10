// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Author:          Antigravity / DFU-Multiplayer Team
// Description:     Tactical RTS Spawner & Ghost Placement Preview Controller for DM (Phase 16).

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Provides the Dungeon Master with real-time RTS-style placement of monsters, NPCs, and creatures.
    /// Manages a purely local ghost preview with collision & slope validation, mouse-wheel rotation,
    /// and authoritative host spawn confirmation. Zero network traffic while previewing.
    /// </summary>
    public class DMRtsPlacementController : MonoBehaviour
    {
        public static DMRtsPlacementController Instance { get; private set; }

        private bool isPlacing = false;
        public bool IsPlacing => isPlacing;

        private MobileTypes activeEnemyType = MobileTypes.SkeletalWarrior;
        private float activeHpMultiplier = 1.0f;
        private float currentFacingAngle = 0f;

        private GameObject ghostRoot = null;
        private GameObject placementRing = null;
        private GameObject directionPointer = null;
        private Material ringMaterial = null;
        private Material pointerMaterial = null;
        private MeshRenderer ghostBillboardRenderer = null;

        private bool isPositionValid = false;
        private Vector3 currentHitPoint = Vector3.zero;
        private Vector3 currentHitNormal = Vector3.up;

        private readonly Color validColor = new Color(0.15f, 0.95f, 0.25f, 0.6f);
        private readonly Color invalidColor = new Color(0.95f, 0.15f, 0.15f, 0.65f);
        private readonly Color validGhostColor = new Color(0.7f, 1f, 0.7f, 0.75f);
        private readonly Color invalidGhostColor = new Color(1f, 0.35f, 0.35f, 0.75f);

        private const float MaxRaycastDistance = 150f;
        private const float MaxWalkableSlopeAngle = 50f;
        private const float RotationStepDegrees = 15f;

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
            CancelPlacement();
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationQuit()
        {
            CancelPlacement();
        }

        /// <summary>
        /// Initiates RTS tactical placement mode for the specified enemy and HP scale.
        /// </summary>
        public void StartPlacement(MobileTypes enemyType, float hpMultiplier = 1.0f)
        {
            if (!NetworkSession.ShouldHandleSpawns)
            {
                DaggerfallUI.AddHUDText("[RTS Spawner] Solo el Host/Servidor (DM) puede colocar entidades tácticas.");
                return;
            }

            if (isPlacing)
            {
                CancelPlacement();
            }

            activeEnemyType = enemyType;
            activeHpMultiplier = Mathf.Clamp(hpMultiplier, 0.5f, 10.0f);

            Camera cam = GetActiveCamera();
            currentFacingAngle = cam != null ? cam.transform.eulerAngles.y : 0f;

            if (!CreateLocalGhostPreview(enemyType))
            {
                DaggerfallUI.AddHUDText("[RTS Spawner] Error al instanciar vista previa del monstruo.");
                return;
            }

            isPlacing = true;
            DaggerfallUI.AddHUDText($"[RTS Spawner] Colocación Táctica: {enemyType}. Rueda/Q-E=Rotar | Click Izq=Colocar (Shift=Múltiple) | Click Der/Esc=Cancelar.");
            DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
        }

        /// <summary>
        /// Cancels active placement and destroys all local preview objects without network footprint.
        /// </summary>
        public void CancelPlacement()
        {
            isPlacing = false;
            isPositionValid = false;

            if (ghostRoot != null)
            {
                Destroy(ghostRoot);
                ghostRoot = null;
            }
            if (placementRing != null)
            {
                Destroy(placementRing);
                placementRing = null;
            }
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
                ringMaterial = null;
            }
            if (pointerMaterial != null)
            {
                Destroy(pointerMaterial);
                pointerMaterial = null;
            }
            ghostBillboardRenderer = null;
        }

        private void Update()
        {
            if (!isPlacing)
                return;

            // Cancellation inputs
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
                DaggerfallUI.AddHUDText("[RTS Spawner] Colocación cancelada.");
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
                return;
            }

            Camera cam = GetActiveCamera();
            if (cam == null)
                return;

            // Raycast surface hit from mouse cursor or center of screen
            Ray ray;
            if (Cursor.lockState != CursorLockMode.Locked)
                ray = cam.ScreenPointToRay(Input.mousePosition);
            else
                ray = new Ray(cam.transform.position, cam.transform.forward);

            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance, ~0, QueryTriggerInteraction.Ignore);

            if (hitSomething)
            {
                if (ghostRoot != null) ghostRoot.SetActive(true);
                if (placementRing != null) placementRing.SetActive(true);

                currentHitPoint = hit.point;
                currentHitNormal = hit.normal;

                // Rotation adjustment via mouse wheel or Q / E
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.05f)
                {
                    currentFacingAngle = (currentFacingAngle + scroll * RotationStepDegrees) % 360f;
                    if (currentFacingAngle < 0f) currentFacingAngle += 360f;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    currentFacingAngle = (currentFacingAngle - 90f * Time.unscaledDeltaTime) % 360f;
                    if (currentFacingAngle < 0f) currentFacingAngle += 360f;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    currentFacingAngle = (currentFacingAngle + 90f * Time.unscaledDeltaTime) % 360f;
                    if (currentFacingAngle < 0f) currentFacingAngle += 360f;
                }

                Quaternion rot = Quaternion.Euler(0f, currentFacingAngle, 0f);

                // Position ghost upright on ground
                if (ghostRoot != null)
                {
                    ghostRoot.transform.position = hit.point + Vector3.up * 0.05f;
                    ghostRoot.transform.rotation = rot;
                }

                // Position placement indicator ring
                if (placementRing != null)
                {
                    placementRing.transform.position = hit.point + Vector3.up * 0.02f;
                    placementRing.transform.rotation = rot;
                }

                // Placement Validation
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                bool slopeOk = slopeAngle <= MaxWalkableSlopeAngle;

                // Overlap test
                Vector3 capBottom = hit.point + Vector3.up * 0.25f;
                Vector3 capTop = hit.point + Vector3.up * 1.6f;
                Collider[] overlaps = Physics.OverlapCapsule(capBottom, capTop, 0.45f, ~0, QueryTriggerInteraction.Ignore);

                bool isBlocked = false;
                foreach (var col in overlaps)
                {
                    if (col == null) continue;
                    if (ghostRoot != null && col.transform.IsChildOf(ghostRoot.transform)) continue;
                    if (placementRing != null && col.transform.IsChildOf(placementRing.transform)) continue;
                    if (col.gameObject == hit.collider.gameObject) continue; // Surface being placed upon

                    // Block if intersecting character controllers or physical solids
                    if (col is CharacterController || col is BoxCollider || col is CapsuleCollider || col is SphereCollider)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                isPositionValid = slopeOk && !isBlocked;

                // Update visual feedback
                Color ringCol = isPositionValid ? validColor : invalidColor;
                if (ringMaterial != null) ringMaterial.color = ringCol;

                Color ghostCol = isPositionValid ? validGhostColor : invalidGhostColor;
                if (ghostBillboardRenderer != null && ghostBillboardRenderer.material != null)
                {
                    ghostBillboardRenderer.material.color = ghostCol;
                }

                // Confirm placement on Left Click
                if (Input.GetMouseButtonDown(0))
                {
                    if (isPositionValid)
                    {
                        ConfirmPlacement();
                    }
                    else
                    {
                        DaggerfallUI.AddHUDText("[RTS Spawner] Ubicación inválida u obstruida.");
                        DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
                    }
                }
            }
            else
            {
                // Aiming at sky or out of range
                isPositionValid = false;
                if (ghostRoot != null) ghostRoot.SetActive(false);
                if (placementRing != null) placementRing.SetActive(false);
            }
        }

        private void ConfirmPlacement()
        {
            Vector3 spawnPos = currentHitPoint + Vector3.up * 0.15f;
            Quaternion spawnRot = Quaternion.Euler(0f, currentFacingAngle, 0f);
            MobileTypes toSpawn = activeEnemyType;
            float hpMult = activeHpMultiplier;

            // Authoritatively spawn on Host
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.SpawnEnemyAtPlacement(toSpawn, spawnPos, spawnRot, hpMult);
                DaggerfallUI.Instance?.PlayOneShot(SoundClips.MakeItem);
            }

            // If holding Shift, keep placing additional units (RTS queue placement)
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!shiftHeld)
            {
                CancelPlacement();
            }
            else
            {
                DaggerfallUI.AddHUDText($"[RTS Spawner] Invocado {toSpawn}. Modo continuo activo (Shift).");
            }
        }

        private bool CreateLocalGhostPreview(MobileTypes enemyType)
        {
            if (DaggerfallUnity.Instance == null || DaggerfallUnity.Instance.Option_EnemyPrefab == null)
                return false;

            // Purely local preview GameObject
            ghostRoot = GameObjectHelper.InstantiatePrefab(
                DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject,
                $"LocalGhost_{enemyType}",
                null,
                Vector3.zero
            );

            if (ghostRoot == null)
                return false;

            // Strip/Disable AI and physics components so it behaves strictly as a silent visual preview
            EnemyMotor motor = ghostRoot.GetComponent<EnemyMotor>();
            if (motor != null) motor.enabled = false;

            EnemyAttack attack = ghostRoot.GetComponent<EnemyAttack>();
            if (attack != null) attack.enabled = false;

            EnemySenses senses = ghostRoot.GetComponent<EnemySenses>();
            if (senses != null) senses.enabled = false;

            EnemySounds sounds = ghostRoot.GetComponent<EnemySounds>();
            if (sounds != null) sounds.enabled = false;

            CharacterController cc = ghostRoot.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            foreach (var col in ghostRoot.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Apply enemy visual billboard
            SetupDemoEnemy setup = ghostRoot.GetComponent<SetupDemoEnemy>();
            if (setup != null)
            {
                setup.ApplyEnemySettings(enemyType, MobileReactions.Passive, MobileGender.Unspecified);
                MobileUnit mobileUnit = setup.GetMobileBillboardChild();
                if (mobileUnit != null)
                {
                    ghostBillboardRenderer = mobileUnit.GetComponent<MeshRenderer>();
                    if (ghostBillboardRenderer != null && ghostBillboardRenderer.material != null)
                    {
                        Shader ghostShader = Shader.Find(MaterialReader._DaggerfallGhostShaderName) ?? Shader.Find("Daggerfall/Billboard");
                        if (ghostShader != null)
                            ghostBillboardRenderer.material.shader = ghostShader;
                        ghostBillboardRenderer.material.color = validGhostColor;
                    }
                }
            }

            // Create circular placement ring on the ground
            placementRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(placementRing.GetComponent<Collider>());
            placementRing.name = "RTS_PlacementRing";
            placementRing.transform.localScale = new Vector3(1.3f, 0.015f, 1.3f);

            Shader unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            ringMaterial = new Material(unlitShader) { color = validColor };
            placementRing.GetComponent<MeshRenderer>().material = ringMaterial;

            // Create forward orientation indicator arrow
            directionPointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(directionPointer.GetComponent<Collider>());
            directionPointer.name = "RTS_DirectionPointer";
            directionPointer.transform.SetParent(placementRing.transform);
            directionPointer.transform.localPosition = new Vector3(0f, 0.6f, 0.45f);
            directionPointer.transform.localScale = new Vector3(0.18f, 0.4f, 0.45f);
            directionPointer.transform.localRotation = Quaternion.identity;

            pointerMaterial = new Material(unlitShader) { color = new Color(1f, 0.85f, 0.2f, 0.85f) };
            directionPointer.GetComponent<MeshRenderer>().material = pointerMaterial;

            return true;
        }

        private Camera GetActiveCamera()
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCamera != null)
                return GameManager.Instance.MainCamera;
            return Camera.main;
        }

        private void OnGUI()
        {
            if (!isPlacing)
                return;

            float bannerWidth = 540f;
            float bannerHeight = 36f;
            Rect rect = new Rect((Screen.width - bannerWidth) * 0.5f, 14f, bannerWidth, bannerHeight);

            GUI.Box(rect, GUIContent.none, DarkFantasyPixelArtTheme.SectionBannerStyle ?? GUI.skin.box);
            GUILayout.BeginArea(rect);
            GUILayout.Space(6);

            string statusText = isPositionValid
                ? "<color=#55FF55><b>[UBICACIÓN VÁLIDA]</b></color>"
                : "<color=#FF5555><b>[OBSTRUIDO]</b></color>";

            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 12
            };

            GUILayout.Label(
                $"<color=#FFDF78><b>RTS Spawner ({activeEnemyType}):</b></color> {statusText} | Rueda/Q-E: Rotar | Click Izq: Colocar | Esc: Cancelar",
                centeredStyle
            );

            GUILayout.EndArea();
        }
    }
}
