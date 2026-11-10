using UnityEngine;
using Mirror;
using System;

namespace DaggerfallWorkshop.Game.Network
{
    /// <summary>
    /// Base class that bridges Mirror's NetworkBehaviour with our Native DFU Sync Engine.
    /// Components inheriting from this will work with Mirror today, but can easily 
    /// transition to our native engine later.
    /// </summary>
    public abstract class DFUBridgeComponent : NetworkBehaviour, IDFU_NetworkBridge, IDFU_NetworkObject
    {
        [SerializeField]
        protected string networkIdString = Guid.NewGuid().ToString();

        public Guid NetworkId => Guid.Parse(networkIdString);

        public virtual void RegisterBridge()
        {
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.Registry.Register(this);
            }
            else
            {
                Debug.LogWarning($"[DFU Native] NetworkManager no listo aún para {gameObject.name}");
            }
        }

        public virtual void UnregisterBridge()
        {
            if (DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.Registry.Unregister(NetworkId);
            }
        }

        public abstract byte[] GetState();
        public abstract void ApplyState(byte[] state);

        public virtual void SendStateUpdate(byte[] data)
        {
            if (isServer)
            {
                Debug.Log($"[DFU Bridge] SendStateUpdate (Servidor). Enviando RPC a clientes.");
                RpcReceiveState(data);
            }
            else
            {
                Debug.LogWarning("[DFU Bridge] SendStateUpdate llamado desde Cliente. Esto no debería pasar en el patrón Server-Authoritative.");
            }
        }

        [ClientRpc]
        protected virtual void RpcReceiveState(byte[] data)
        {
            ApplyState(data);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            StartCoroutine(RegisterWithRetry());
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(RegisterWithRetry());
        }

        private System.Collections.IEnumerator RegisterWithRetry()
        {
            // Wait until the manager is initialized
            while (DFUNetworkManager.Instance == null)
            {
                yield return null; // Wait for next frame
            }
            
            RegisterBridge();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            UnregisterBridge();
        }
    }
}
