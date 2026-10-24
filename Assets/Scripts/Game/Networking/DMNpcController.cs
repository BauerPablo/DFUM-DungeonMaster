// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)

using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// DM NPC Possession Controller.
    /// Allows the Dungeon Master to possess and control any NPC or monster,
    /// driving movement, animations, melee attacks, and camera perspective in real-time.
    /// </summary>
    public class DMNpcController : MonoBehaviour
    {
        public static DMNpcController Instance { get; private set; }

        private bool isPossessing = false;
        public bool IsPossessing => isPossessing;

        private GameObject possessedObject = null;
        public GameObject PossessedObject => possessedObject;
        public string PossessedName => possessedObject != null ? possessedObject.name : "None";

        private CharacterController possessedController = null;
        private MobileUnit possessedMobile = null;
        private EnemyMotor possessedMotor = null;
        private EnemyAttack possessedAttack = null;
        private EnemySenses possessedSenses = null;
        private DaggerfallEntityBehaviour possessedBehaviour = null;
        private EnemyEntity possessedEntity = null;

        // Camera handling
        private Transform cameraTransform = null;
        private Transform originalCameraParent = null;
        private Vector3 originalCameraLocalPos;
        private PlayerMouseLook cachedMouseLook = null;
        private PlayerMotor cachedPlayerMotor = null;
        private static bool isQuitting = false;
        private Quaternion originalCameraLocalRot;
        private bool wasFreeCamActiveBeforePossession = false;

        // Perspective & Look
        private bool isThirdPerson = true;
        public bool IsThirdPerson => isThirdPerson;
        private float yaw = 0f;
        private float pitch = 0f;
        public float lookSensitivity = 2.0f;
        public float moveSpeed = 5.5f;

        // Attack cooldown
        private float attackTimer = 0f;
        private const float AttackCooldown = 0.85f;
        private int possessionFrame = -1;

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
            if (!isPossessing || possessedObject == null)
                return;

            // Guard against instant release on the frame possession is initiated
            if (Time.frameCount == possessionFrame)
                return;

            // Attack cooldown decrement
            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;

            // Check release hotkeys (Esc or F8 or Ctrl+F8)
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F8) || (ctrlHeld && Input.GetKeyDown(KeyCode.F8)))
            {
                ReleasePossession();
                return;
            }

            // Toggle 1st / 3rd person perspective with 'Tab' or keypad
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isThirdPerson = !isThirdPerson;
                DaggerfallUI.AddHUDText($"[DM Possession] Perspective: {(isThirdPerson ? "Third-Person Chase" : "First-Person Eyes")}");
            }

            // Do not capture controls if modal UI is open or Master Suite is open
            bool isModalWindowOpen = (DaggerfallUI.UIManager != null &&
                                     DaggerfallUI.UIManager.TopWindow != null &&
                                     !(DaggerfallUI.UIManager.TopWindow is DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallHUD)) ||
                                     (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.IsAnyGuiOpen);

            if (!Application.isFocused || isModalWindowOpen)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            // Lock cursor
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Mouse Look
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            yaw += mouseX;
            pitch = Mathf.Clamp(pitch - mouseY, -65f, 65f);

            possessedObject.transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Movement (WASD)
            Vector3 forward = possessedObject.transform.forward;
            Vector3 right = possessedObject.transform.right;
            Vector3 moveDir = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) moveDir += forward;
            if (Input.GetKey(KeyCode.S)) moveDir -= forward;
            if (Input.GetKey(KeyCode.D)) moveDir += right;
            if (Input.GetKey(KeyCode.A)) moveDir -= right;

            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                currentSpeed *= 1.6f;

            if (possessedController != null && possessedController.enabled)
            {
                Vector3 motion = moveDir.normalized * currentSpeed;
                motion.y += Physics.gravity.y * 1.5f;
                possessedController.Move(motion * Time.deltaTime);
            }
            else
            {
                possessedObject.transform.position += moveDir.normalized * (currentSpeed * Time.deltaTime);
            }

            // Update mobile billboard animation state
            if (possessedMobile != null)
            {
                if (moveDir.sqrMagnitude > 0.01f && attackTimer <= 0.2f)
                {
                    possessedMobile.ChangeEnemyState(MobileStates.Move);
                }
                else if (attackTimer <= 0.1f)
                {
                    possessedMobile.ChangeEnemyState(MobileStates.Idle);
                }
            }

            // Attack (Left Click)
            if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
            {
                PerformPossessedAttack();
            }

            // Update camera position
            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            if (cameraTransform == null || possessedObject == null)
                return;

            float entityHeight = possessedController != null ? possessedController.height : 1.8f;

            if (isThirdPerson)
            {
                Vector3 headPoint = possessedObject.transform.position + Vector3.up * (entityHeight * 0.9f);
                Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 chaseOffset = camRot * new Vector3(0f, 0.4f, -2.6f);

                Vector3 desiredPos = headPoint + chaseOffset;

                // Collision safeguard for chase camera
                if (Physics.Raycast(headPoint, chaseOffset.normalized, out RaycastHit hit, 2.6f))
                {
                    desiredPos = hit.point - chaseOffset.normalized * 0.2f;
                }

                cameraTransform.position = desiredPos;
                cameraTransform.rotation = Quaternion.LookRotation((headPoint + Vector3.up * 0.2f) - cameraTransform.position);
            }
            else
            {
                // First-person eye perspective
                Vector3 eyePos = possessedObject.transform.position + Vector3.up * (entityHeight * 0.85f) + possessedObject.transform.forward * 0.25f;
                cameraTransform.position = eyePos;
                cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        private void PerformPossessedAttack()
        {
            attackTimer = AttackCooldown;

            if (possessedMobile != null)
                possessedMobile.ChangeEnemyState(MobileStates.PrimaryAttack);

            // Play audio
            EnemySounds sounds = possessedObject.GetComponent<EnemySounds>();
            if (sounds != null)
                sounds.PlayAttackSound();
            else if (DaggerfallUI.Instance != null)
                DaggerfallUI.Instance.PlayOneShot(SoundClips.SwingMediumPitch);

            // Raycast melee hit detection
            float reach = 2.5f;
            Vector3 attackOrigin = possessedObject.transform.position + Vector3.up * 1.2f;
            Vector3 attackDir = possessedObject.transform.forward;

            if (Physics.Raycast(attackOrigin, attackDir, out RaycastHit hit, reach))
            {
                DaggerfallEntityBehaviour target = hit.collider.GetComponentInParent<DaggerfallEntityBehaviour>();
                if (target != null && target.gameObject != possessedObject)
                {
                    int damage = 12;
                    if (possessedEntity != null)
                        damage = UnityEngine.Random.Range(possessedEntity.MobileEnemy.MinDamage, possessedEntity.MobileEnemy.MaxDamage + 1);

                    if (target.Entity != null)
                    {
                        target.Entity.DecreaseHealth(damage);
                        if (target.gameObject == GameManager.Instance.PlayerObject)
                        {
                            GameManager.Instance.PlayerObject.SendMessage("RemoveHealth", damage, SendMessageOptions.DontRequireReceiver);
                        }
                        else
                        {
                            NativePlayerPuppet puppet = target.GetComponentInParent<NativePlayerPuppet>();
                            if (puppet != null && DFUNetworkManager.Instance != null)
                            {
                                DFUNetworkManager.Instance.SendPlayerDamage(puppet.connectionId, damage);
                            }
                        }
                    }
                    DaggerfallUI.AddHUDText($"[DM Possession] Struck {target.name} for {damage} damage!");
                }
            }
        }

        private GameObject FindMobileEntityAlongRay(Ray ray, Camera mainCam)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int mask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;

            // 1. Precise RaycastAll
            RaycastHit[] hits = Physics.RaycastAll(ray, 150f, mask);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                GameObject candidate = ExtractTargetCandidate(hit.collider);
                if (candidate != null)
                    return candidate;
            }

            // 2. Generous SphereCastAll (makes targeting thin sprite billboards much more forgiving)
            RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.85f, 150f, mask);
            Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in sphereHits)
            {
                GameObject candidate = ExtractTargetCandidate(hit.collider);
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private GameObject ExtractTargetCandidate(Collider col)
        {
            if (col == null) return null;
            if (GameManager.Instance != null && col.gameObject == GameManager.Instance.PlayerObject) return null;
            if (col.GetComponentInParent<NativePlayerPuppet>() != null) return null;

            EnemyMotor motor = col.GetComponentInParent<EnemyMotor>();
            if (motor != null && motor.gameObject != GameManager.Instance.PlayerObject)
                return motor.gameObject;

            MobilePersonNPC person = col.GetComponentInParent<MobilePersonNPC>();
            if (person != null && person.gameObject != GameManager.Instance.PlayerObject)
                return person.gameObject;

            DaggerfallEntityBehaviour behaviour = col.GetComponentInParent<DaggerfallEntityBehaviour>();
            if (behaviour != null && behaviour.gameObject != GameManager.Instance.PlayerObject)
                return behaviour.gameObject;

            return null;
        }

        public bool PossessTargetUnderCrosshair()
        {
            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam == null)
                return false;

            // 1. First priority: Check center crosshair ray (where the DM is aiming forward)
            Ray centerRay = new Ray(mainCam.transform.position, mainCam.transform.forward);
            GameObject target = FindMobileEntityAlongRay(centerRay, mainCam);
            if (target != null)
                return PossessEntity(target);

            // 2. Second priority: If cursor is active / unlocked, check cursor screen ray
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
                target = FindMobileEntityAlongRay(mouseRay, mainCam);
                if (target != null)
                    return PossessEntity(target);
            }

            // 3. Third priority fallback: Find the closest living enemy or NPC in front of the camera (within 25m, forward cone)
            DaggerfallEntityBehaviour[] allBehaviours = FindObjectsOfType<DaggerfallEntityBehaviour>();
            GameObject bestCandidate = null;
            float bestDist = 25f;
            foreach (var b in allBehaviours)
            {
                if (b == null || (GameManager.Instance != null && b.gameObject == GameManager.Instance.PlayerObject))
                    continue;
                if (b.EntityType != EntityTypes.EnemyMonster && b.EntityType != EntityTypes.EnemyClass)
                    continue;

                Vector3 toEntity = b.transform.position - mainCam.transform.position;
                float dist = toEntity.magnitude;
                if (dist > 25f)
                    continue;

                float dot = Vector3.Dot(mainCam.transform.forward, toEntity.normalized);
                if (dot > 0.5f && dist < bestDist) // In front within ~60 degrees
                {
                    bestDist = dist;
                    bestCandidate = b.gameObject;
                }
            }

            if (bestCandidate != null)
                return PossessEntity(bestCandidate);

            DaggerfallUI.AddHUDText("[DM Possession] No mobile entity targeted. Aim at an enemy or NPC.");
            return false;
        }

        public bool PossessEntity(GameObject target)
        {
            if (target == null || (GameManager.Instance != null && target == GameManager.Instance.PlayerObject))
                return false;

            if (isPossessing)
                ReleasePossession();

            possessedObject = target;
            possessedController = target.GetComponent<CharacterController>();
            possessedMobile = target.GetComponentInChildren<MobileUnit>();
            possessedMotor = target.GetComponent<EnemyMotor>();
            possessedAttack = target.GetComponent<EnemyAttack>();
            possessedSenses = target.GetComponent<EnemySenses>();
            possessedBehaviour = target.GetComponent<DaggerfallEntityBehaviour>();
            possessedEntity = possessedBehaviour != null ? possessedBehaviour.Entity as EnemyEntity : null;

            // Disable autonomous AI routines
            if (possessedMotor != null)
                possessedMotor.enabled = false;
            if (possessedAttack != null)
                possessedAttack.enabled = false;
            if (possessedSenses != null)
                possessedSenses.enabled = false;

            // FreeCam awareness & camera setup
            wasFreeCamActiveBeforePossession = (FreeCamController.Instance != null && FreeCamController.Instance.IsActive);

            Camera mainCam = GameManager.Instance != null ? GameManager.Instance.MainCamera : null;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;

                if (wasFreeCamActiveBeforePossession && FreeCamController.Instance != null && FreeCamController.Instance.OriginalCameraParent != null)
                {
                    originalCameraParent = FreeCamController.Instance.OriginalCameraParent;
                }
                else
                {
                    originalCameraParent = cameraTransform.parent;
                    originalCameraLocalPos = cameraTransform.localPosition;
                    originalCameraLocalRot = cameraTransform.localRotation;
                }

                cameraTransform.SetParent(null);
            }

            // Freeze player motor & mouse look safely
            try
            {
                if (mainCam != null)
                    cachedMouseLook = mainCam.GetComponent<PlayerMouseLook>();
                if (GameManager.Instance != null && GameManager.Instance.PlayerObject != null)
                    cachedPlayerMotor = GameManager.Instance.PlayerObject.GetComponent<PlayerMotor>();

                if (cachedPlayerMotor != null)
                {
                    cachedPlayerMotor.CancelMovement = true;
                    cachedPlayerMotor.enabled = false;
                }
                if (cachedMouseLook != null)
                {
                    cachedMouseLook.enableMouseLook = false;
                    cachedMouseLook.enabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DM Possession] Player freeze warning: {ex.Message}");
            }

            // Automatically close Master Suite window if open so DM immediately has direct control
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.showTtrpgSuite)
            {
                DFUNetworkManager.Instance.showTtrpgSuite = false;
            }

            // Lock cursor
            if (Application.isFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            yaw = possessedObject.transform.eulerAngles.y;
            pitch = 15f;

            possessionFrame = Time.frameCount;
            isPossessing = true;
            string targetName = possessedEntity != null ? possessedEntity.Name : possessedObject.name;
            DaggerfallUI.AddHUDText($"[DM Possession] POSSESSION ACTIVE: {targetName} (WASD: Move, LClick: Attack, Tab: 1st/3rd View, Esc/F8/Ctrl+H: Release)");
            return true;
        }

        public void ReleasePossession()
        {
            if (!isPossessing)
                return;

            // Restore autonomous AI routines
            if (possessedMotor != null)
                possessedMotor.enabled = true;
            if (possessedAttack != null)
                possessedAttack.enabled = true;
            if (possessedSenses != null)
                possessedSenses.enabled = true;

            // Restore camera back to appropriate anchor
            if (wasFreeCamActiveBeforePossession && FreeCamController.Instance != null && FreeCamController.Instance.IsActive)
            {
                FreeCamController.Instance.SetCameraPositionAndFacing(cameraTransform != null ? cameraTransform.position : Vector3.zero, yaw, pitch);
            }
            else if (cameraTransform != null)
            {
                cameraTransform.SetParent(originalCameraParent);
                cameraTransform.localPosition = originalCameraLocalPos;
                cameraTransform.localRotation = originalCameraLocalRot;
            }

            // Re-enable player motor & mouse look if not in FreeCam
            if (!wasFreeCamActiveBeforePossession && (FreeCamController.Instance == null || !FreeCamController.Instance.IsActive))
            {
                try
                {
                    if (cachedPlayerMotor != null)
                    {
                        cachedPlayerMotor.enabled = true;
                        cachedPlayerMotor.CancelMovement = true;
                        cachedPlayerMotor = null;
                    }
                    if (cachedMouseLook != null)
                    {
                        cachedMouseLook.enabled = true;
                        cachedMouseLook.enableMouseLook = true;
                        cachedMouseLook = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DM Possession] Release teardown warning: {ex.Message}");
                }
            }

            isPossessing = false;
            possessedObject = null;
            possessedController = null;
            possessedMobile = null;
            possessedMotor = null;
            possessedAttack = null;
            possessedSenses = null;
            possessedBehaviour = null;
            possessedEntity = null;

            DaggerfallUI.AddHUDText("[DM Possession] Possession RELEASED. Restored previous DM state.");
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDisable()
        {
            if (isQuitting)
            {
                isPossessing = false;
                return;
            }

            if (isPossessing)
                ReleasePossession();
        }

        private void OnDestroy()
        {
            if (isQuitting)
            {
                isPossessing = false;
                return;
            }

            if (isPossessing)
                ReleasePossession();
        }
    }
}
