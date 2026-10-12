using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace TanguyMultiplayer
{
    public struct CmdSyncEntity : NetworkMessage
    {
        public Guid guid;
        public string entityName;
        public int mobileType;
        public int mobileGender;
        public bool alliedToPlayer;
        public Vector3 pos;
        public string parentName;
    }

    public struct CmdAttackEntity : NetworkMessage
    {
        public Guid guid;
        public int damage;
    }

    public struct MsgEntitySpawned : NetworkMessage
    {
        public Guid guid;
        public string entityName;
        public int mobileType;
        public int mobileGender;
        public bool alliedToPlayer;
        public Vector3 pos;
        public string parentName;
    }

    public struct MsgEntityMove : NetworkMessage
    {
        public Guid guid;
        public Vector3 newPos;
        public Quaternion newRot;
    }

    public struct MsgEntityDeath : NetworkMessage
    {
        public Guid guid;
    }
}
