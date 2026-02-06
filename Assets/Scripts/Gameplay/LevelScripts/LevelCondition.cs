using Gameplay.Data;
using UnityEngine;

namespace Gameplay.LevelScripts
{
	public abstract class LevelCondition : MonoBehaviour
	{
		[SerializeField] private EventName conditionName;

		private bool _wasStarted;

		private void Awake()
		{
			if (!GameData.instance.spawnedConditions.Contains(conditionName))
			{
				Destroy(gameObject);
				return;
			}

			GameData.instance.spawnedConditions.Add(conditionName);
			GameData.instance.OnObjectInteracted += InstanceOnOnObjectInteracted;
			GameData.instance.OnZoneEntered += InstanceOnOnZoneEntered;
			_wasStarted = true;

			transform.SetParent(null);
		}

		private void OnDestroy()
		{
			if (!_wasStarted) return;
			GameData.instance.OnObjectInteracted -= InstanceOnOnObjectInteracted;
			GameData.instance.OnZoneEntered -= InstanceOnOnZoneEntered;
		}

		protected virtual void InstanceOnOnZoneEntered(CycleZoneID obj)
		{
		}

		protected virtual void InstanceOnOnObjectInteracted(string obj)
		{
		}

		protected virtual void CompleteEvent()
		{
			GameData.instance.CompleteEvent(conditionName);
			Destroy(gameObject);
		}
	}
}