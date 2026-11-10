using System;

namespace DaggerfallWorkshop.Game.Network
{
    /// <summary>
    /// Interface that defines how an object acts as a bridge between the legacy 
    /// transport (Mirror) and our new DFU Native Sync Engine.
    /// </summary>
    public interface IDFU_NetworkBridge
    {
        /// <summary>
        /// Registers this bridge component with the DFUNetworkManager registry.
        /// </summary>
        void RegisterBridge();

        /// <summary>
        /// Unregisters this bridge component from the DFUNetworkManager registry.
        /// </summary>
        void UnregisterBridge();

        /// <summary>
        /// Sends a state update through the native sync engine.
        /// </summary>
        void SendStateUpdate(byte[] data);
    }
}
