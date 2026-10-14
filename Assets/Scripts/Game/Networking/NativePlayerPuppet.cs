// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Visual puppet representation for a connected party member in the game world.
    /// Handles billboard animations, overhead 3D name tag billboard, and smooth snapshot motion interpolation.
    /// Implements IPlayerActivable for interactive character-click menus (map sharing, trade).
    /// </summary>
    public class NativePlayerPuppet : MonoBehaviour, IPlayerActivable
    {
        [Header("Player Metadata")]
        public int connectionId;
        public string playerName;
        public string careerName;
        public string raceName;
        public int gender;

        [Header("Motion Interpolation")]
        public float lerpRate = 12.0f;
        public float snapDistance = 3.5f;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isMoving;
        private bool isCrouching;
        private bool isAttacking;
        private int weaponType;
        private bool lastAttacking;
        private CapsuleCollider capsuleCollider;

        private MobileUnit mobileUnit;
        private GameObject nameTagObject;
        private TextMesh nameTextMesh;

        private GameObject crouchBillboardObject;
        private MeshRenderer crouchRenderer;
        private Material crouchMaterial;
        private Texture2D crouchFrontTex;
        private Texture2D crouchBackTex;
        private bool hasCrouchTextures = false;

        public void Initialize(MsgPlayerProfile profile)
        {
            connectionId = profile.connectionId;
            playerName = profile.playerName;
            careerName = profile.careerName;
            raceName = profile.raceName;
            gender = profile.gender;

            targetPosition = profile.position;
            targetRotation = Quaternion.Euler(0, profile.yaw, 0);

            transform.position = profile.position;
            transform.rotation = targetRotation;

            capsuleCollider = GetComponent<CapsuleCollider>();

            SetupDemoEnemy setup = GetComponent<SetupDemoEnemy>();
            if (setup != null)
                mobileUnit = setup.GetMobileBillboardChild();
            if (mobileUnit == null)
                mobileUnit = GetComponentInChildren<MobileUnit>();

            CreateOverheadNameTag();
            InitializeCrouchBillboard();
        }

        private void InitializeCrouchBillboard()
        {
            try
            {
                string[] candidateDirs = new string[]
                {
                    System.IO.Path.Combine(Application.streamingAssetsPath, "Textures/TEXTURE.488"),
                    System.IO.Path.Combine(Application.streamingAssetsPath, "TEXTURE.488"),
                    System.IO.Path.Combine(Application.dataPath, "StreamingAssets/Textures/TEXTURE.488"),
                    System.IO.Path.Combine(Application.dataPath, "Game/Mods/TanguyMultiplayer/Textures/Sprites/TEXTURE.488 (Medium Fighter)"),
                    System.IO.Path.Combine(Application.dataPath, "../Assets/Game/Mods/TanguyMultiplayer/Textures/Sprites/TEXTURE.488 (Medium Fighter)")
                };

                string foundDir = null;
                foreach (string dir in candidateDirs)
                {
                    if (System.IO.Directory.Exists(dir) && System.IO.File.Exists(System.IO.Path.Combine(dir, "25-0.PNG")))
                    {
                        foundDir = dir;
                        break;
                    }
                }

                if (foundDir != null)
                {
                    string frontPath = System.IO.Path.Combine(foundDir, "25-0.PNG");
                    string backPath = System.IO.Path.Combine(foundDir, "25-1.PNG");

                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] frontBytes = System.IO.File.ReadAllBytes(frontPath);
                        crouchFrontTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        crouchFrontTex.LoadImage(frontBytes);
                        crouchFrontTex.filterMode = FilterMode.Point;
                        hasCrouchTextures = true;
                    }

                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] backBytes = System.IO.File.ReadAllBytes(backPath);
                        crouchBackTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        crouchBackTex.LoadImage(backBytes);
                        crouchBackTex.filterMode = FilterMode.Point;
                    }
                }

                if (hasCrouchTextures)
                {
                    crouchBillboardObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    crouchBillboardObject.name = "CrouchBillboard";
                    crouchBillboardObject.transform.SetParent(transform, false);

                    Collider col = crouchBillboardObject.GetComponent<Collider>();
                    if (col != null)
                        Destroy(col);

                    crouchRenderer = crouchBillboardObject.GetComponent<MeshRenderer>();
                    MeshRenderer unitRenderer = mobileUnit != null ? mobileUnit.GetComponent<MeshRenderer>() : null;
                    if (unitRenderer != null && unitRenderer.sharedMaterial != null)
                    {
                        crouchMaterial = new Material(unitRenderer.sharedMaterial);
                    }
                    else
                    {
                        Shader shader = Shader.Find("Daggerfall/Billboard") ?? Shader.Find("Sprites/Default");
                        crouchMaterial = new Material(shader);
                    }
                    crouchMaterial.mainTexture = crouchFrontTex;
                    crouchRenderer.material = crouchMaterial;

                    crouchBillboardObject.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DFU Puppet] Could not initialize custom crouch billboard: {ex.Message}");
            }
        }

        private void CreateOverheadNameTag()
        {
            if (nameTagObject != null)
                return;

            nameTagObject = new GameObject("NameTag");
            nameTagObject.transform.SetParent(transform, false);
            nameTagObject.transform.localPosition = new Vector3(0, 2.15f, 0);

            nameTextMesh = nameTagObject.AddComponent<TextMesh>();
            nameTextMesh.text = playerName;
            nameTextMesh.alignment = TextAlignment.Center;
            nameTextMesh.anchor = TextAnchor.MiddleCenter;
            nameTextMesh.characterSize = 0.12f;
            nameTextMesh.fontSize = 24;
            nameTextMesh.color = new Color(0.2f, 0.9f, 1.0f); // Bright cyan
        }

        private void Update()
        {
            // Smoothly glide towards target snapshot position & orientation
            float currentDist = Vector3.Distance(transform.position, targetPosition);
            float effectiveLerpRate = currentDist > 1.5f ? lerpRate * 2.5f : lerpRate;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * effectiveLerpRate);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * effectiveLerpRate);

            // Orient 3D name tag towards active camera
            if (nameTagObject != null)
            {
                Camera mainCam = GameManager.Instance.MainCamera;
                if (mainCam != null)
                {
                    nameTagObject.transform.rotation = mainCam.transform.rotation;
                }
            }

            // Crouching handling
            if (nameTagObject != null)
            {
                nameTagObject.transform.localPosition = isCrouching ? new Vector3(0, 1.05f, 0) : new Vector3(0, 2.15f, 0);
            }
            if (capsuleCollider != null)
            {
                capsuleCollider.height = isCrouching ? 0.9f : 1.8f;
                capsuleCollider.center = isCrouching ? Vector3.zero : new Vector3(0, 0.9f, 0);
            }

            if (hasCrouchTextures && crouchBillboardObject != null)
            {
                if (isCrouching)
                {
                    if (mobileUnit != null && mobileUnit.gameObject.activeSelf)
                        mobileUnit.gameObject.SetActive(false);

                    if (!crouchBillboardObject.activeSelf)
                        crouchBillboardObject.SetActive(true);

                    Camera mainCam = GameManager.Instance.MainCamera;
                    if (mainCam != null)
                    {
                        crouchBillboardObject.transform.rotation = mainCam.transform.rotation;

                        Vector3 toCamera = (mainCam.transform.position - transform.position).normalized;
                        float dot = Vector3.Dot(transform.forward, toCamera);
                        Texture2D targetTex = (dot > 0f) ? crouchFrontTex : (crouchBackTex ?? crouchFrontTex);

                        if (crouchMaterial != null && crouchMaterial.mainTexture != targetTex)
                        {
                            crouchMaterial.mainTexture = targetTex;
                        }

                        float aspect = (targetTex != null && targetTex.height > 0) ? (float)targetTex.width / targetTex.height : (56f / 75f);
                        float height = 1.20f;
                        float width = height * aspect;
                        crouchBillboardObject.transform.localScale = new Vector3(width, height, 1f);
                        // Pivot of quad is at center. Crouch root is at Y_ground + 0.45m, so local ground is at -0.45m.
                        // Setting center at -0.45m + (height * 0.5f) grounds the bottom of the sprite perfectly.
                        crouchBillboardObject.transform.localPosition = new Vector3(0, -0.45f + (height * 0.5f), 0);
                    }
                }
                else
                {
                    if (crouchBillboardObject.activeSelf)
                        crouchBillboardObject.SetActive(false);

                    if (mobileUnit != null && !mobileUnit.gameObject.activeSelf)
                        mobileUnit.gameObject.SetActive(true);
                }
            }

            // Attack and motion animations
            if (mobileUnit != null && mobileUnit.gameObject.activeSelf)
            {
                if (isAttacking)
                {
                    MobileStates attackState = (weaponType == (int)WeaponTypes.Bow) ? MobileStates.RangedAttack1 : MobileStates.PrimaryAttack;
                    if (mobileUnit.EnemyState != attackState && (mobileUnit.EnemyState == MobileStates.Idle || !lastAttacking))
                    {
                        mobileUnit.ChangeEnemyState(attackState);

                        if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.DaggerfallAudioSource != null)
                        {
                            SoundClips clip = (weaponType == (int)WeaponTypes.Bow) ? SoundClips.ArrowShoot : SoundClips.SwingMediumPitch;
                            DaggerfallUI.Instance.DaggerfallAudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
                        }
                    }
                }
                else
                {
                    bool actuallyMoving = isMoving || (Vector3.Distance(transform.position, targetPosition) > 0.04f);
                    if (actuallyMoving && mobileUnit.EnemyState != MobileStates.Move && mobileUnit.EnemyState != MobileStates.PrimaryAttack && mobileUnit.EnemyState != MobileStates.RangedAttack1)
                    {
                        mobileUnit.ChangeEnemyState(MobileStates.Move);
                    }
                    else if (!actuallyMoving && mobileUnit.EnemyState == MobileStates.Move)
                    {
                        mobileUnit.ChangeEnemyState(MobileStates.Idle);
                    }
                }
                lastAttacking = isAttacking;
            }
        }

        private void OnDestroy()
        {
            if (crouchMaterial != null)
                Destroy(crouchMaterial);
            if (crouchFrontTex != null)
                Destroy(crouchFrontTex);
            if (crouchBackTex != null)
                Destroy(crouchBackTex);
        }

        /// <summary>
        /// Controls puppet visibility based on whether local player and remote peer are in the same cell context.
        /// </summary>
        public void SetCellVisibility(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
            {
                gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                }
            }
        }

        /// <summary>
        /// Updates the target position, orientation, and action states from incoming network packets.
        /// </summary>
        public void ApplyTransform(Vector3 pos, float yaw, bool moving, bool crouching, bool attacking, int weapon)
        {
            targetPosition = pos;
            targetRotation = Quaternion.Euler(0, yaw, 0);
            isMoving = moving;
            isCrouching = crouching;
            isAttacking = attacking;
            weaponType = weapon;

            if (Vector3.Distance(transform.position, pos) > snapDistance)
            {
                transform.position = pos;
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// Triggered when local player clicks / activates this puppet in the game world within interaction range.
        /// </summary>
        public void Activate(RaycastHit hit)
        {
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.OpenPlayerInteractionMenu(this);
            }
        }
    }
}
