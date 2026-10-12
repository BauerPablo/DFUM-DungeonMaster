using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Network;
using TanguyMultiplayer;

public class EntityCatcher : DFUBridgeComponent
{
	public float radius = 500f;
	public LayerMask layer;

	void Start()
	{
		// Si DFUNetworkManager nativo está activo, la arquitectura nativa tiene el control total
		// Desactivar este componente legacy para evitar duplicación de entidades en clientes
		if (DaggerfallWorkshop.Game.Networking.DFUNetworkManager.Instance != null)
		{
			enabled = false;
			return;
		}

		if (isServer)
		{
			StartCoroutine(CheckEncountersRoutine());
			StartCoroutine(CheckMovementRoutine());
			StartCoroutine(CheckDeathsRoutine());
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		if (DaggerfallWorkshop.Game.Networking.DFUNetworkManager.Instance != null)
			return;

		NetworkServer.RegisterHandler<MsgEntityMove>(OnServerEntityMove);
		NetworkServer.RegisterHandler<MsgEntityDeath>(OnServerEntityDeath);
		NetworkServer.RegisterHandler<CmdAttackEntity>(OnServerAttackEntity);
	}

	public override void OnStopServer()
	{
		base.OnStopServer();
		NetworkServer.UnregisterHandler<MsgEntityMove>();
		NetworkServer.UnregisterHandler<MsgEntityDeath>();
		NetworkServer.UnregisterHandler<CmdAttackEntity>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (DaggerfallWorkshop.Game.Networking.DFUNetworkManager.Instance != null)
			return;

		NetworkClient.RegisterHandler<MsgEntitySpawned>(OnMsgEntitySpawned);
		NetworkClient.RegisterHandler<MsgEntityMove>(OnMsgEntityMoveClient);
		NetworkClient.RegisterHandler<MsgEntityDeath>(OnMsgEntityDeathClient);
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		NetworkClient.UnregisterHandler<MsgEntitySpawned>();
		NetworkClient.UnregisterHandler<MsgEntityMove>();
		NetworkClient.UnregisterHandler<MsgEntityDeath>();
	}

	// El Servidor escanea y registra entidades nativas
	IEnumerator CheckEncountersRoutine()
	{
		yield return new WaitForSeconds(2.0f);
		while (true)
		{
			if (isServer)
			{
				Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, layer);
				foreach (var hitCollider in hitColliders)
				{
					GameObject g = hitCollider.gameObject;
					DaggerfallEntityBehaviour ed = g.GetComponent<DaggerfallEntityBehaviour>();
					
					if (ed != null && g.GetComponent<DaggerfallEnemy>() != null && !g.name.Contains("Quest"))
					{
						EntitySyncMarker marker = g.GetComponent<EntitySyncMarker>();
						if (marker == null)
						{
							Guid newGuid = Guid.NewGuid();
							marker = g.AddComponent<EntitySyncMarker>();
							marker.guid = newGuid;

							SetupDemoEnemy setupEnemy = g.GetComponent<SetupDemoEnemy>();
							int mobileType = setupEnemy != null ? (int)setupEnemy.EnemyType : 0;
							int mobileGender = setupEnemy != null ? (int)setupEnemy.EnemyGender : 0;
							bool allied = setupEnemy != null && setupEnemy.AlliedToPlayer;
							string parentName = g.transform.parent != null ? g.transform.parent.name : "ExteriorParent";

							if (EntityRegistry.Instance != null)
								EntityRegistry.Instance.Register(newGuid, ed);

							NetworkServer.SendToAll(new MsgEntitySpawned
							{
								guid = newGuid,
								entityName = g.name,
								mobileType = mobileType,
								mobileGender = mobileGender,
								alliedToPlayer = allied,
								pos = g.transform.position,
								parentName = parentName
							});
						}
					}
				}
			}
			yield return new WaitForSeconds(3.0f);
		}
	}

	// El Servidor transmite posiciones y rotaciones periódicamente
	IEnumerator CheckMovementRoutine()
	{
		yield return new WaitForSeconds(2.5f);
		Dictionary<Guid, Vector3> lastPositions = new Dictionary<Guid, Vector3>();

		while (true)
		{
			if (isServer && EntityRegistry.Instance != null)
			{
				var allEntities = EntityRegistry.Instance.GetAllEntities();
				foreach (var kvp in allEntities)
				{
					if (kvp.Value != null)
					{
						Vector3 currentPos = kvp.Value.transform.position;
						Quaternion currentRot = kvp.Value.transform.rotation;
						if (!lastPositions.ContainsKey(kvp.Key) || Vector3.Distance(lastPositions[kvp.Key], currentPos) > 0.1f)
						{
							lastPositions[kvp.Key] = currentPos;
							NetworkServer.SendToAll(new MsgEntityMove
							{
								guid = kvp.Key,
								newPos = currentPos,
								newRot = currentRot
							});
						}
					}
				}
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

	IEnumerator CheckDeathsRoutine()
	{
		while (true)
		{
			if (isServer && EntityRegistry.Instance != null)
			{
				var allEntities = EntityRegistry.Instance.GetAllEntities();
				List<Guid> deadGuids = new List<Guid>();

				foreach (var kvp in allEntities)
				{
					if (kvp.Value != null && kvp.Value.Entity.CurrentHealth <= 0f)
					{
						deadGuids.Add(kvp.Key);
					}
				}

				foreach (var guid in deadGuids)
				{
					DaggerfallEntityBehaviour ed = EntityRegistry.Instance.GetEntity(guid);
					if (ed != null)
					{
						ed.Entity.SetHealth(-1);
					}
					EntityRegistry.Instance.Unregister(guid);

					NetworkServer.SendToAll(new MsgEntityDeath { guid = guid });
				}
			}
			yield return new WaitForSeconds(1.0f);
		}
	}



	private void OnServerEntityMove(NetworkConnection conn, MsgEntityMove msg)
	{
		NetworkServer.SendToAll(msg);
	}

	private void OnServerEntityDeath(NetworkConnection conn, MsgEntityDeath msg)
	{
		if (EntityRegistry.Instance != null)
		{
			DaggerfallEntityBehaviour ed = EntityRegistry.Instance.GetEntity(msg.guid);
			if (ed != null)
			{
				ed.Entity.SetHealth(-1);
			}
			EntityRegistry.Instance.Unregister(msg.guid);
		}
		NetworkServer.SendToAll(msg);
	}

	private void OnMsgEntitySpawned(MsgEntitySpawned msg)
	{
		// Si ya existe un marcador con este GUID, salir
		foreach (var m in GameObject.FindObjectsOfType<EntitySyncMarker>())
		{
			if (m.guid == msg.guid) return;
		}

		// En lugar de instanciar uno nuevo que duplique el del mundo, 
		// buscamos si hay un enemigo local DFU sin marcar cerca de la posición del spawn del servidor
		DaggerfallEntityBehaviour closestEntity = null;
		float minDistance = float.MaxValue;

		DaggerfallEntityBehaviour[] allEntities = GameObject.FindObjectsOfType<DaggerfallEntityBehaviour>();
		foreach (var ed in allEntities)
		{
			if (ed.GetComponent<DaggerfallEnemy>() != null && ed.GetComponent<EntitySyncMarker>() == null)
			{
				float dist = Vector3.Distance(ed.transform.position, msg.pos);
				if (dist < minDistance && dist < 5.0f) // En un radio de 5 metros
				{
					minDistance = dist;
					closestEntity = ed;
				}
			}
		}

		if (closestEntity != null)
		{
			// ¡Reutilizamos el enemigo local nativo de DFU! Le ponemos el marcador GUID y desactivamos su motor local y físicas en clientes puros
			EntitySyncMarker marker = closestEntity.gameObject.AddComponent<EntitySyncMarker>();
			marker.guid = msg.guid;

			if (!isServer)
			{
				EnemyMotor motor = closestEntity.GetComponent<EnemyMotor>();
				if (motor != null) motor.enabled = false;

				CharacterController cc = closestEntity.GetComponent<CharacterController>();
				if (cc != null) cc.enabled = false;

				Rigidbody rb = closestEntity.GetComponent<Rigidbody>();
				if (rb != null)
				{
					rb.isKinematic = true;
					rb.detectCollisions = false;
				}

				AudioSource audio = closestEntity.GetComponent<AudioSource>();
				if (audio != null) audio.enabled = false;
			}

			EntityRegistry.Instance?.Register(msg.guid, closestEntity);
			Debug.Log($"[EntityCatcher] [CLIENT] Enemigo local mapeado al GUID: {msg.guid}");
		}
		else
		{
			// Fallback: si no hay enemigo local cercano, instanciamos el replicado
			GameObject parent = GameObject.Find(msg.parentName);
			if (parent == null) parent = GameManager.Instance.PlayerEnterExit.ExteriorParent;

			GameObject g = GameObjectHelper.CreateEnemy(msg.entityName, (MobileTypes)msg.mobileType, msg.pos, (MobileGender)msg.mobileGender, parent.transform, msg.alliedToPlayer ? MobileReactions.Passive : MobileReactions.Hostile);
			if (g != null)
			{
				g.transform.position = msg.pos;
				g.name = msg.entityName;

				EntitySyncMarker marker = g.AddComponent<EntitySyncMarker>();
				marker.guid = msg.guid;

				DaggerfallEntityBehaviour ed = g.GetComponent<DaggerfallEntityBehaviour>();
				if (ed != null)
				{
					if (!isServer)
					{
						EnemyMotor motor = ed.GetComponent<EnemyMotor>();
						if (motor != null) motor.enabled = false;

						CharacterController cc = ed.GetComponent<CharacterController>();
						if (cc != null) cc.enabled = false;

						Rigidbody rb = ed.GetComponent<Rigidbody>();
						if (rb != null)
						{
							rb.isKinematic = true;
							rb.detectCollisions = false;
						}

						AudioSource audio = ed.GetComponent<AudioSource>();
						if (audio != null) audio.enabled = false;
					}

					EntityRegistry.Instance?.Register(msg.guid, ed);
				}
				Debug.Log($"[EntityCatcher] [CLIENT] Enemigo replicado creado por fallback: {msg.entityName}");
			}
		}
	}

	private void OnMsgEntityMoveClient(MsgEntityMove msg)
	{
		if (EntityRegistry.Instance != null)
		{
			DaggerfallEntityBehaviour ed = EntityRegistry.Instance.GetEntity(msg.guid);
			if (ed != null && !isServer)
			{
				EntitySyncMarker marker = ed.GetComponent<EntitySyncMarker>();
				if (marker != null)
				{
					marker.SetTarget(msg.newPos, msg.newRot);
				}
				else
				{
					// Fallback directo si no tiene marcador
					ed.transform.position = msg.newPos;
					ed.transform.rotation = msg.newRot;
				}
			}
		}
	}

	private void OnServerAttackEntity(NetworkConnection conn, CmdAttackEntity msg)
	{
		if (EntityRegistry.Instance != null)
		{
			DaggerfallEntityBehaviour ed = EntityRegistry.Instance.GetEntity(msg.guid);
			if (ed != null && ed.Entity != null)
			{
				ed.Entity.DecreaseHealth(msg.damage);
				if (ed.Entity.CurrentHealth <= 0)
				{
					EntityRegistry.Instance.Unregister(msg.guid);
					NetworkServer.SendToAll(new MsgEntityDeath { guid = msg.guid });
				}
			}
		}
	}

	private void OnMsgEntityDeathClient(MsgEntityDeath msg)
	{
		if (EntityRegistry.Instance != null)
		{
			DaggerfallEntityBehaviour ed = EntityRegistry.Instance.GetEntity(msg.guid);
			if (ed != null)
			{
				ed.Entity.SetHealth(-1);
			}
			EntityRegistry.Instance.Unregister(msg.guid);
		}

		foreach (var m in GameObject.FindObjectsOfType<EntitySyncMarker>())
		{
			if (m.guid == msg.guid)
			{
				DaggerfallEntityBehaviour ed = m.GetComponent<DaggerfallEntityBehaviour>();
				if (ed != null)
				{
					ed.Entity.SetHealth(-1);
				}
			}
		}
	}

	public override byte[] GetState() => new byte[0];
	public override void ApplyState(byte[] state) { }
}
