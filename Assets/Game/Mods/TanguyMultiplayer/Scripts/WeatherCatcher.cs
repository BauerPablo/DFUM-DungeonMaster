using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Weather;
using Mirror;

using DaggerfallWorkshop.Game.Network;

public class WeatherCatcher : DFUBridgeComponent
{
	WeatherManager weatherManager;
	PlayerWeather playerWeather;
	WeatherType lastWeather;
	string weatherName;
	
	public override byte[] GetState()
	{
		byte[] data = System.Text.Encoding.UTF8.GetBytes(playerWeather.WeatherType.ToString());
		Debug.Log($"[DFU Native Sync] WeatherCatcher.GetState: {playerWeather.WeatherType}");
		return data;
	}

	public override void ApplyState(byte[] state)
	{
		string weather = System.Text.Encoding.UTF8.GetString(state);
		Debug.Log($"[DFU Native Sync] WeatherCatcher.ApplyState: {weather}");
		weatherName = weather;
		setWeather();
	}

	// Removed OnEnable/OnDisable debugs.

	void Start()
	{
		init();
	}
	
	// Removed Update() debug logic.
	
	void init()
	{
		weatherManager = GameManager.Instance.WeatherManager;
		playerWeather = weatherManager.PlayerWeather;
		if (isLocalPlayer)
			StartCoroutine(Check());
	
	}
	
	
	IEnumerator Check()
	{
		lastWeather = playerWeather.WeatherType;
		
		while (true)
		{
			
			if (lastWeather != playerWeather.WeatherType){
				if (isServer){//Weather is controlled by the host to avoid apocalyptic weather change from every clients
					print("WEATHER SEND " + playerWeather.WeatherType.ToString());
					rpcSendWeather(playerWeather.WeatherType.ToString());
					
					// Native Sync Engine update
					SendStateUpdate(System.Text.Encoding.UTF8.GetBytes(playerWeather.WeatherType.ToString()));
					
					lastWeather = playerWeather.WeatherType;
				}else{
					cmdReceiveWeather();
				}
			}
			
			yield return new WaitForSeconds(2.56f);
		}
	}
	
	[Command]
	void cmdReceiveWeather()
	{
		rpcSendWeather(playerWeather.WeatherType.ToString());
	}
	
	
	
	[ClientRpc]
	public void rpcSendWeather(string weather)
	{
		if (!isServer){
			weatherName = weather;
			setWeather();
		}
	}
	
	void setWeather()
	{
		if (GameObject.Find("Exterior") != null)
			weatherManager.SetWeather(getWeatherType(weatherName));
		else
			Invoke("setWeather", 0.5f); //retry setting weather later if player still isn't outside
	}
	
	
	
	WeatherType getWeatherType(string s)
	{
		switch (s) {
			case "Sunny":
				return WeatherType.Sunny;
			case "Cloudy":
				return WeatherType.Cloudy;
			case "Overcast":
				return WeatherType.Overcast;
			case "Fog":
				return WeatherType.Fog;
			case "Rain":
				return WeatherType.Rain;
			case "Rain_Normal":
				return WeatherType.Rain;
			case "Snow":
				return WeatherType.Snow;
			case "Snow_Normal":
				return WeatherType.Snow;
			case "Thunder":
				return WeatherType.Thunder;
		}
		return WeatherType.Sunny;
	}
}
