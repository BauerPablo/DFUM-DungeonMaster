using System;

namespace DaggerfallWorkshop.Game.Network
{
    /// <summary>
    /// Interface for any object that needs to be synchronized over the network.
    /// This is the core abstraction layer for our native DFU Sync Engine.
    /// </summary>
    public interface IDFU_NetworkObject
    {
        /// <summary>
        /// Unique identifier for the network object.
        /// </summary>
        Guid NetworkId { get; }

        /// <summary>
        /// Serializes the object's current state to a byte array.
        /// </summary>
        byte[] GetState();

        /// <summary>
        /// Applies a serialized state to the object.
        /// </summary>
        void ApplyState(byte[] state);
    }
}
