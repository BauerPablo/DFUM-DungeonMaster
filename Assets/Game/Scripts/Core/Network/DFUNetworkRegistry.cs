using System;
using System.Collections.Generic;

namespace DaggerfallWorkshop.Game.Network
{
    /// <summary>
    /// Registry to track all network-synchronized objects in the scene.
    /// This allows the NetworkManager to find objects by ID to route incoming data.
    /// </summary>
    public class DFUNetworkRegistry
    {
        private readonly Dictionary<Guid, IDFU_NetworkObject> _registry = new Dictionary<Guid, IDFU_NetworkObject>();

        public void Register(IDFU_NetworkObject networkObject)
        {
            if (networkObject == null) return;
            
            if (!_registry.ContainsKey(networkObject.NetworkId))
            {
                _registry.Add(networkObject.NetworkId, networkObject);
            }
        }

        public void Unregister(Guid networkId)
        {
            if (_registry.ContainsKey(networkId))
            {
                _registry.Remove(networkId);
            }
        }

        public IDFU_NetworkObject GetObject(Guid networkId)
        {
            _registry.TryGetValue(networkId, out var networkObject);
            return networkObject;
        }
    }
}
