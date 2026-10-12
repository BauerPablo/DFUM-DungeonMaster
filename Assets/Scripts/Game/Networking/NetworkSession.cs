// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using Mirror;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Global networking role and session provider for Daggerfall Unity.
    /// Provides authoritative role queries without direct coupling to specific transport logic.
    /// </summary>
    public static class NetworkSession
    {
        /// <summary>
        /// Returns true if a multiplayer session is active (either as server, host, or connected client).
        /// Returns false when playing single-player.
        /// </summary>
        public static bool IsActive
        {
            get { return NetworkServer.active || NetworkClient.active; }
        }

        /// <summary>
        /// Returns true if this instance acts as server authority (dedicated server or host).
        /// In single-player, this returns false.
        /// </summary>
        public static bool IsServer
        {
            get { return NetworkServer.active; }
        }

        /// <summary>
        /// Returns true if this instance is a pure client (connected to an external server, not hosting).
        /// </summary>
        public static bool IsClient
        {
            get { return NetworkClient.active && !NetworkServer.active; }
        }

        /// <summary>
        /// Returns true if this instance is acting as host (both local server and client connected).
        /// </summary>
        public static bool IsHost
        {
            get { return NetworkServer.active && NetworkClient.isConnected; }
        }

        /// <summary>
        /// Returns the local network connection ID (0 for host/server, >0 for connected clients).
        /// </summary>
        public static int LocalConnectionId
        {
            get
            {
                if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localConnectionId > 0)
                    return DFUNetworkManager.Instance.localConnectionId;
                return NetworkClient.connection != null ? NetworkClient.connection.connectionId : 0;
            }
        }

        /// <summary>
        /// Returns true if this instance is running single-player (no active network session).
        /// </summary>
        public static bool IsSinglePlayer
        {
            get { return !IsActive; }
        }

        /// <summary>
        /// Returns true if the client is actively connected to a server.
        /// </summary>
        public static bool IsConnected
        {
            get { return NetworkClient.isConnected; }
        }

        /// <summary>
        /// Determines whether this instance has authority to execute game world spawners
        /// (dungeon enemies, wandering foes, quest encounters).
        /// Single-player and Server/Host return true; Pure Clients return false.
        /// </summary>
        public static bool ShouldHandleSpawns
        {
            get { return IsSinglePlayer || IsServer; }
        }

        /// <summary>
        /// Returns a human-readable summary of the current session state.
        /// </summary>
        public static string GetStatusString()
        {
            if (IsSinglePlayer)
                return "Single Player (Offline)";
            if (IsHost)
                return "Host (Server + Client Connected)";
            if (IsServer)
                return "Dedicated Server (Listening)";
            if (IsClient)
                return IsConnected ? "Connected Client" : "Connecting Client...";
            return "Unknown";
        }
    }
}
