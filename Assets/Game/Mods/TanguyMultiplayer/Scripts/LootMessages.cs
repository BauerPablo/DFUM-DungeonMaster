using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace TanguyMultiplayer
{
    public struct CmdDropLoot : NetworkMessage
    {
        public Guid guid;
        public Vector3 pos;
        public string parentName;
        public byte[] itemData;
    }

    public struct CmdPickLoot : NetworkMessage
    {
        public Guid guid;
    }

    public struct MsgLootSpawned : NetworkMessage
    {
        public Guid guid;
        public Vector3 pos;
        public string parentName;
        public byte[] itemData;
    }

    public struct MsgLootDestroyed : NetworkMessage
    {
        public Guid guid;
    }
}
