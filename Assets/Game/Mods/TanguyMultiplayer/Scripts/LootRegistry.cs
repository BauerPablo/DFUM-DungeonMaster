using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;

namespace TanguyMultiplayer
{
    public class LootRegistry : MonoBehaviour
    {
        public static LootRegistry Instance { get; private set; }
        private Dictionary<Guid, DaggerfallLoot> _activeLoots = new Dictionary<Guid, DaggerfallLoot>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Register(Guid guid, DaggerfallLoot loot)
        {
            _activeLoots[guid] = loot;
        }

        public void Unregister(Guid guid)
        {
            if (_activeLoots.ContainsKey(guid))
                _activeLoots.Remove(guid);
        }

        public DaggerfallLoot GetLoot(Guid guid)
        {
            return _activeLoots.ContainsKey(guid) ? _activeLoots[guid] : null;
        }
    }
}
