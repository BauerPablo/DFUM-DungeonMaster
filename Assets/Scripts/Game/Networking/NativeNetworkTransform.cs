// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Synchronizes spatial transforms and billboard motion animations across the network.
    /// Handles authoritative tick-based broadcasting on Server/Host and smooth snapshot interpolation on Clients.
    /// </summary>
    public class NativeNetworkTransform : MonoBehaviour
    {
        [Header("Entity Link")]
        public NativeNetworkEntity netEntity;
        public bool isClientProxy = false;

        [Header("Streaming Rates")]
        [Tooltip("Network tick interval in seconds (0.05 = 20 Hz)")]
        public float sendInterval = 0.05f;
        public float positionThreshold = 0.02f; // 2 cm
        public float rotationThreshold = 2.0f;  // 2 degrees

        [Header("Interpolation Settings")]
        public float lerpRate = 12.0f;
        public float snapDistance = 6.0f; // Teleport threshold if desynced

        // Snapshot state
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private byte targetAnimState;
        private Vector3 lastSentPosition;
        private float lastSentYaw;
        private float timer;

        private MobileUnit mobileUnit;

        private void Awake()
        {
            if (netEntity == null)
                netEntity = GetComponent<NativeNetworkEntity>();

            targetPosition = transform.position;
            targetRotation = transform.rotation;
            lastSentPosition = transform.position;
            lastSentYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            SetupDemoEnemy setup = GetComponent<SetupDemoEnemy>();
            if (setup != null)
                mobileUnit = setup.GetMobileBillboardChild();
            if (mobileUnit == null)
                mobileUnit = GetComponentInChildren<MobileUnit>();
        }

        private void Update()
        {
            if (isClientProxy)
            {
                InterpolateClientTransform();
            }
            else if (NetworkSession.IsServer && NetworkServer.active)
            {
                StreamServerTransform();
            }
        }

        /// <summary>
        /// Applies incoming server transform snapshot onto proxy.
        /// </summary>
        public void ApplySnapshot(Vector3 pos, float yaw, byte animState)
        {
            targetPosition = pos;
            targetRotation = Quaternion.Euler(0, yaw, 0);
            targetAnimState = animState;

            // If proxy is wildly out of sync, snap immediately
            if (Vector3.Distance(transform.position, pos) > snapDistance)
            {
                transform.position = pos;
                transform.rotation = targetRotation;
            }
        }

        private void InterpolateClientTransform()
        {
            // Smoothly glide towards target snapshot position & rotation
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpRate);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpRate);

            // Synchronize visual animation state on billboard
            if (mobileUnit != null)
            {
                bool isMoving = (targetAnimState == 1) || (Vector3.Distance(transform.position, targetPosition) > 0.04f);
                if (isMoving && mobileUnit.EnemyState != MobileStates.Move)
                {
                    mobileUnit.ChangeEnemyState(MobileStates.Move);
                }
                else if (!isMoving && mobileUnit.EnemyState == MobileStates.Move)
                {
                    mobileUnit.ChangeEnemyState(MobileStates.Idle);
                }
            }
        }

        private void StreamServerTransform()
        {
            if (netEntity == null || netEntity.entityId == Guid.Empty)
                return;

            timer += Time.deltaTime;
            if (timer < sendInterval)
                return;

            timer = 0f;

            Vector3 currentPos = transform.position;
            float currentYaw = transform.eulerAngles.y;

            bool posChanged = Vector3.Distance(currentPos, lastSentPosition) > positionThreshold;
            bool rotChanged = Mathf.Abs(Mathf.DeltaAngle(currentYaw, lastSentYaw)) > rotationThreshold;

            if (posChanged || rotChanged)
            {
                lastSentPosition = currentPos;
                lastSentYaw = currentYaw;

                byte animState = 0; // 0 = Idle
                if (mobileUnit != null)
                {
                    if (mobileUnit.EnemyState == MobileStates.Move)
                        animState = 1;
                    else if (mobileUnit.EnemyState == MobileStates.PrimaryAttack ||
                             mobileUnit.EnemyState == MobileStates.RangedAttack1 ||
                             mobileUnit.EnemyState == MobileStates.RangedAttack2)
                        animState = 2;
                }
                else if (posChanged)
                {
                    animState = 1;
                }

                MsgEntityTransform msg = new MsgEntityTransform
                {
                    entityId = netEntity.entityId,
                    position = currentPos,
                    yaw = currentYaw,
                    animState = animState
                };

                NetworkServer.SendToAll(msg);
            }
        }
    }
}
