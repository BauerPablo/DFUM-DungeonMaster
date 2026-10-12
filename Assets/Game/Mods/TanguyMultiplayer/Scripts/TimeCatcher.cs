using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using Mirror;

using DaggerfallWorkshop.Game.Network;

public class TimeCatcher : DFUBridgeComponent
{
	WorldTime worldTime;
	static ulong lastTime;
	
	public override byte[] GetState()
	{
		return System.BitConverter.GetBytes(worldTime.Now.ToSeconds());
	}

	public override void ApplyState(byte[] state)
	{
		ulong timeInSeconds = System.BitConverter.ToUInt64(state, 0);
		
		// Búsqueda segura si worldTime falla para evitar NullReferenceException y prevenir desconexiones
		if (worldTime == null)
		{
			var go = GameObject.Find("DaggerfallUnity");
			if (go != null) worldTime = go.GetComponent<WorldTime>();
		}

		if (worldTime != null)
		{
			worldTime.Now.FromSeconds(timeInSeconds);
			lastTime = timeInSeconds;
		}
	}

	void Awake()
	{
		Debug.Log($"[DFU Native Sync] TimeCatcher Awake en {gameObject.name}. enabled: {enabled}");
	}

	void Start()

	{
		init();
	}
	
	void init()
	{
		worldTime = GameObject.Find("DaggerfallUnity").GetComponent<WorldTime>();

		// Servidor siempre debe ejecutar la lógica de sincronización
		// Cliente solo la ejecuta si es el jugador local
		if (isServer || isLocalPlayer)
			StartCoroutine(Check());
		
	}
	
	
	IEnumerator Check()
	{
		Debug.Log("DEBUG: TimeCatcher Check iniciado");
		lastTime = worldTime.Now.ToClassicDaggerfallTime();
		
		while (true)
		{
			// Verificamos si la diferencia es significativa para sincronizar o prevenir exploits
			long diff = (long)worldTime.Now.ToClassicDaggerfallTime() - (long)lastTime;
			Debug.Log($"DEBUG: TimeCatcher Check loop. Dif: {diff}");
			
			// Si la diferencia es mayor a 40 (clásico del mod) o negativa (desincronización crítica)
			if (diff > 40 || diff < 0){
				
				if (OptionsMultiplayer.timeHost && !isServer){
					cmdReceiveTime();
				}else{
					DaggerfallDateTime now = worldTime.Now;
					ulong timeInSeconds = now.ToSeconds();
					cmdSendTime(timeInSeconds);
					
					// Native Sync Engine update
					SendStateUpdate(System.BitConverter.GetBytes(timeInSeconds));
					
					lastTime = worldTime.Now.ToClassicDaggerfallTime();
				}
			}
			lastTime = worldTime.Now.ToClassicDaggerfallTime();
			yield return new WaitForSeconds(2.25f);
		}
	}
	
	[Command]
	public void cmdSendTime(ulong i/*int year, int month, int day, int hour, int minute, float second*/){
		rpcSendTime(i/*year, month, day, hour, minute, second*/);
	}
	
	[Command]
	public void cmdReceiveTime()
	{
		DaggerfallDateTime now = worldTime.Now;
		receiveTime(now.ToSeconds()/*now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second*/);
	}
	
	[ClientRpc]
	void receiveTime(ulong i/*int year, int month, int day, int hour, int minute, float second*/)
	{
		worldTime.Now.FromSeconds(i);//new DaggerfallDateTime(/*year, month, day, hour, minute, Mathf.Clamp(second, 0, 590917)*/);
		lastTime = worldTime.Now.ToSeconds();
	}
	
	[ClientRpc]
	public void rpcSendTime(ulong i/*int year, int month, int day, int hour, int minute, float second*/){
		worldTime.Now.FromSeconds(i);//(/*year, month, day, hour, minute, Mathf.Clamp(second, 0, 590917)*/);
		lastTime = worldTime.Now.ToSeconds();
	}
	
	
	
}
