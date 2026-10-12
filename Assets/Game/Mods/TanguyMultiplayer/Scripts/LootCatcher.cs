using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop;

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Network;
using TanguyMultiplayer;

public class LootCatcher : DFUBridgeComponent
{
	public GameObject lootPrefab;
	public Transform[] locations;

	void Start()
	{
		Debug.Log("[LootCatcher] Start llamado. Inicializando detección de loot.");
		InitLocations();
	}

	void InitLocations()
	{
		PlayerEnterExit pEnterExit = GameManager.Instance.PlayerEnterExit;
		if (pEnterExit == null)
		{
			StartCoroutine(WaitForPlayerEnterExit());
			return;
		}

		locations = new Transform[3];
		locations[0] = pEnterExit.ExteriorParent != null ? pEnterExit.ExteriorParent.transform : null;
		locations[1] = pEnterExit.InteriorParent != null ? pEnterExit.InteriorParent.transform : null;
		locations[2] = pEnterExit.DungeonParent != null ? pEnterExit.DungeonParent.transform : null;

		StartCoroutine(CheckLocalDrops());
	}

	IEnumerator WaitForPlayerEnterExit()
	{
		while (GameManager.Instance == null || GameManager.Instance.PlayerEnterExit == null)
		{
			yield return null;
		}
		InitLocations();
	}

	int GetLocationEnabled()
	{
		if (locations == null) return 0;
		for (int i = 0; i < locations.Length; i++)
		{
			if (locations[i] != null && locations[i].gameObject.activeSelf)
				return i;
		}
		return 0;
	}

	Transform GetLocationLoot(int loc)
	{
		if (locations[loc] == null) return null;
		if (loc == 1 && locations[loc].childCount > 1)
			return locations[loc].GetChild(1);
		else if (locations[loc].childCount > 2)
			return locations[loc].GetChild(2);
		return locations[loc];
	}

	IEnumerator CheckLocalDrops()
	{
		Debug.Log("[LootCatcher] Corrutina CheckLocalDrops iniciada.");
		while (true)
		{
			int locIndex = GetLocationEnabled();
			Transform locTransform = GetLocationLoot(locIndex);

			if (locTransform != null)
			{
				foreach (Transform t in locTransform)
				{
					if (t.name.Contains("DroppedLoot") && t.GetComponent<LootSyncMarker>() == null)
					{
						DaggerfallLoot loot = t.GetComponent<DaggerfallLoot>();
						if (loot != null && loot.Items != null && loot.Items.Count > 0 && loot.playerOwned)
						{
							Guid newGuid = Guid.NewGuid();
							LootSyncMarker marker = t.gameObject.AddComponent<LootSyncMarker>();
							marker.guid = newGuid;

							byte[] serializedItems = SerializeItems(loot.Items);
							string parentName = t.parent != null ? t.parent.name : "ExteriorParent";

							if (isServer)
							{
								Debug.Log($"[LootCatcher] [SERVER DROP] Detectado drop local en servidor. GUID: {newGuid}, Objeto: {t.name}");
								if (LootRegistry.Instance != null)
									LootRegistry.Instance.Register(newGuid, loot);

								DaggerfallBillboard bb = t.GetComponent<DaggerfallBillboard>();
								if (bb != null)
								{
									bb.SetMaterial(205, 0);
									bb.AlignToBase();
								}

								LootMultiplayer lm = t.gameObject.AddComponent<LootMultiplayer>();
								lm.guid = newGuid;
								lm.lootCatcher = this;

								NetworkServer.SendToAll(new MsgLootSpawned
								{
									guid = newGuid,
									pos = t.position,
									parentName = parentName,
									itemData = serializedItems
								});
							}
							else
							{
								Debug.Log($"[LootCatcher] [CLIENT DROP] Detectado drop local en cliente. GUID: {newGuid}, Objeto: {t.name}");
								NetworkClient.Send(new CmdDropLoot
								{
									guid = newGuid,
									pos = t.position,
									parentName = parentName,
									itemData = serializedItems
								});
								t.gameObject.SetActive(false);
							}
						}
					}
				}
			}

			yield return new WaitForSeconds(1.5f);
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		NetworkServer.RegisterHandler<CmdDropLoot>(OnCmdDropLoot);
		NetworkServer.RegisterHandler<CmdPickLoot>(OnCmdPickLoot);
	}

    public override void OnStopServer()
    {
        base.OnStopServer();
        NetworkServer.UnregisterHandler<CmdDropLoot>();
        NetworkServer.UnregisterHandler<CmdPickLoot>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<MsgLootSpawned>(OnMsgLootSpawned);
        NetworkClient.RegisterHandler<MsgLootDestroyed>(OnMsgLootDestroyed);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        NetworkClient.UnregisterHandler<MsgLootSpawned>();
        NetworkClient.UnregisterHandler<MsgLootDestroyed>();
    }

    private void OnCmdDropLoot(NetworkConnection conn, CmdDropLoot msg)
    {
        Debug.Log($"[LootCatcher] [SERVER CMD] Recibió CmdDropLoot de cliente. GUID: {msg.guid}");
        
        // Verificar por marcador existente
        foreach (var m in GameObject.FindObjectsOfType<LootSyncMarker>())
        {
            if (m.guid == msg.guid) return;
        }

        GameObject parent = GameObject.Find(msg.parentName);
        if (parent == null)
        {
            parent = GameManager.Instance.PlayerEnterExit.ExteriorParent;
        }

        GameObject lootHandler = Instantiate(lootPrefab, msg.pos, Quaternion.identity);
        lootHandler.transform.parent = parent.transform;
        lootHandler.name = "MultiplayerLoot_Server_" + msg.guid.ToString().Substring(0, 4);
        
        DaggerfallLoot loot = lootHandler.GetComponent<DaggerfallLoot>();
        if (loot != null)
        {
            loot.Items = DeserializeItems(msg.itemData);
        }

        DaggerfallBillboard bb = lootHandler.GetComponent<DaggerfallBillboard>();
        if (bb != null)
        {
            bb.SetMaterial(205, 0);
            bb.AlignToBase();
        }

        LootSyncMarker marker = lootHandler.AddComponent<LootSyncMarker>();
        marker.guid = msg.guid;

        LootMultiplayer lm = lootHandler.AddComponent<LootMultiplayer>();
        lm.guid = msg.guid;
        lm.lootCatcher = this;

        if (LootRegistry.Instance != null)
            LootRegistry.Instance.Register(msg.guid, loot);

        NetworkServer.SendToAll(new MsgLootSpawned
        {
            guid = msg.guid,
            pos = msg.pos,
            parentName = msg.parentName,
            itemData = msg.itemData
        });
    }

    private void OnCmdPickLoot(NetworkConnection conn, CmdPickLoot msg)
    {
        Debug.Log($"[LootCatcher] [SERVER CMD] Recibió CmdPickLoot. GUID: {msg.guid}");
        if (LootRegistry.Instance != null)
        {
            DaggerfallLoot loot = LootRegistry.Instance.GetLoot(msg.guid);
            if (loot != null)
            {
                loot.gameObject.SetActive(false);
                LootRegistry.Instance.Unregister(msg.guid);
            }
        }

        // También desactivar por marcador
        foreach (var m in GameObject.FindObjectsOfType<LootSyncMarker>())
        {
            if (m.guid == msg.guid)
            {
                m.gameObject.SetActive(false);
            }
        }

        NetworkServer.SendToAll(new MsgLootDestroyed
        {
            guid = msg.guid
        });
    }

    private void OnMsgLootSpawned(MsgLootSpawned msg)
    {
        Debug.Log($"[LootCatcher] [CLIENT MSG] Recibió MsgLootSpawned. GUID: {msg.guid}");

        // Búsqueda directa por LootSyncMarker en toda la escena (100% infalible contra duplicados en host/servidor)
        foreach (var m in GameObject.FindObjectsOfType<LootSyncMarker>())
        {
            if (m.guid == msg.guid)
            {
                Debug.Log($"[LootCatcher] [CLIENT MSG] GUID {msg.guid} ya existe en la escena. Omitiendo instanciación.");
                return;
            }
        }

        GameObject parent = GameObject.Find(msg.parentName);
        if (parent == null) return;

        GameObject lootHandler = Instantiate(lootPrefab, msg.pos, Quaternion.identity);
        lootHandler.transform.parent = parent.transform;
        lootHandler.name = "MultiplayerLoot_Client_" + msg.guid.ToString().Substring(0, 4);

        DaggerfallLoot loot = lootHandler.GetComponent<DaggerfallLoot>();
        if (loot != null)
        {
            loot.Items = DeserializeItems(msg.itemData);
            LootRegistry.Instance?.Register(msg.guid, loot);
        }

        DaggerfallBillboard bb = lootHandler.GetComponent<DaggerfallBillboard>();
        if (bb != null)
        {
            bb.SetMaterial(205, 0);
            bb.AlignToBase();
        }

        LootSyncMarker marker = lootHandler.AddComponent<LootSyncMarker>();
        marker.guid = msg.guid;

        LootMultiplayer lm = lootHandler.AddComponent<LootMultiplayer>();
        lm.guid = msg.guid;
        lm.lootCatcher = this;
    }

    private void OnMsgLootDestroyed(MsgLootDestroyed msg)
    {
        Debug.Log($"[LootCatcher] [CLIENT MSG] Recibió MsgLootDestroyed. GUID: {msg.guid}");
        if (LootRegistry.Instance != null)
        {
            DaggerfallLoot loot = LootRegistry.Instance.GetLoot(msg.guid);
            if (loot != null)
            {
                loot.gameObject.SetActive(false);
                LootRegistry.Instance.Unregister(msg.guid);
            }
        }

        DaggerfallLoot[] allLoots = GameObject.FindObjectsOfType<DaggerfallLoot>();
        foreach (var l in allLoots)
        {
            LootSyncMarker marker = l.GetComponent<LootSyncMarker>();
            if (marker != null && marker.guid == msg.guid)
            {
                l.gameObject.SetActive(false);
            }
        }
    }

    public void RequestPickLoot(Guid guid)
    {
        if (!isServer)
        {
            Debug.Log($"[LootCatcher] [CLIENT REQ] Solicitando pickup. GUID: {guid}");
            NetworkClient.Send(new CmdPickLoot { guid = guid });
        }
        else
        {
            Debug.Log($"[LootCatcher] [SERVER REQ] Pickup directo en servidor. GUID: {guid}");
            if (LootRegistry.Instance != null)
            {
                DaggerfallLoot loot = LootRegistry.Instance.GetLoot(guid);
                if (loot != null)
                {
                    loot.gameObject.SetActive(false);
                    LootRegistry.Instance.Unregister(guid);
                }
            }

            foreach (var m in GameObject.FindObjectsOfType<LootSyncMarker>())
            {
                if (m.guid == guid)
                {
                    m.gameObject.SetActive(false);
                }
            }

            NetworkServer.SendToAll(new MsgLootDestroyed { guid = guid });
        }
    }

    private byte[] SerializeItems(ItemCollection items)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            List<DaggerfallUnityItem> validItems = new List<DaggerfallUnityItem>();
            for (int i = 0; i < items.Count; i++)
            {
                DaggerfallUnityItem item = items.GetItem(i);
                if (item != null && item.TemplateIndex > 0)
                {
                    validItems.Add(item);
                }
            }

            writer.Write(validItems.Count);
            foreach (var item in validItems)
            {
                writer.Write((int)item.ItemGroup);
                writer.Write(item.TemplateIndex);
                writer.Write(item.NativeMaterialValue);
                writer.Write(item.stackCount);
            }
            return ms.ToArray();
        }
    }

    private ItemCollection DeserializeItems(byte[] data)
    {
        ItemCollection itemsList = new ItemCollection();
        if (data == null || data.Length == 0) return itemsList;

        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ItemGroups group = (ItemGroups)reader.ReadInt32();
                int templateIndex = reader.ReadInt32();
                int material = reader.ReadInt32();
                int stackCount = reader.ReadInt32();

                if (templateIndex <= 0) continue;

                DaggerfallUnityItem item = ItemBuilder.CreateItem(group, templateIndex);
                if (item == null) continue;

                item.stackCount = stackCount;
                if (group == ItemGroups.Weapons)
                {
                    ItemBuilder.ApplyWeaponMaterial(item, (WeaponMaterialTypes)material);
                }
                else
                {
                    item.nativeMaterialValue = material;
                }
                itemsList.AddItem(item);
            }
        }
        return itemsList;
    }

    public override byte[] GetState() => new byte[0];
    public override void ApplyState(byte[] state) { }
}
