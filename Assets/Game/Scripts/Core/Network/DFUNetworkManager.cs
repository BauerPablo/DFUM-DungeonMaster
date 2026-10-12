using UnityEngine;

namespace DaggerfallWorkshop.Game.Network
{
    /// <summary>
    /// The central Network Abstraction Layer (NAL) for DFU.
    /// This manager acts as the entry point and routing hub for all network synchronization.
    /// It remains agnostic of the underlying transport layer (Mirror, NGO, etc.).
    /// </summary>
    public class DFUNetworkManager : MonoBehaviour
    {
        // Singleton instance for easy access across DFU
        public static DFUNetworkManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Registry to track synchronized objects
        public DFUNetworkRegistry Registry { get; } = new DFUNetworkRegistry();

        // Placeholder for future Transport Layer implementation:
        // private ITransportLayer _transport;
        
        // This will eventually hold logic for:
        // 1. Initializing the transport layer.
        // 2. Routing serialized data from IDFU_NetworkObjects to the transport.
        // 3. Handling incoming network messages and routing them to the correct objects.
    }
}
