using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;

namespace TanguyMultiplayer
{
    public class EntityRegistry : MonoBehaviour
    {
        public static EntityRegistry Instance { get; private set; }
        private Dictionary<Guid, DaggerfallEntityBehaviour> _activeEntities = new Dictionary<Guid, DaggerfallEntityBehaviour>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Register(Guid guid, DaggerfallEntityBehaviour entity)
        {
            if (entity != null)
                _activeEntities[guid] = entity;
        }

        public void Unregister(Guid guid)
        {
            if (_activeEntities.ContainsKey(guid))
                _activeEntities.Remove(guid);
        }

        public DaggerfallEntityBehaviour GetEntity(Guid guid)
        {
            return _activeEntities.ContainsKey(guid) ? _activeEntities[guid] : null;
        }

        public Dictionary<Guid, DaggerfallEntityBehaviour> GetAllEntities()
        {
            return _activeEntities;
        }
    }
}
