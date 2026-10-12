using System;
using UnityEngine;

public class LootMultiplayer : MonoBehaviour
{
	public LootCatcher lootCatcher;
	public Guid guid;
	private bool _isCleaningUp = false;
	
	void OnDisable()
	{
		NotifyPickup();
	}
	
	void OnDestroy()
	{
		NotifyPickup();
	}

	private void NotifyPickup()
	{
		if (_isCleaningUp) return;
		_isCleaningUp = true;

		if (lootCatcher != null && guid != Guid.Empty)
		{
			lootCatcher.RequestPickLoot(guid);
		}
	}
}
