using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelScripts
{
	public class RemoveObjectEventCompleted : MonoBehaviour
	{
		[SerializeField] private EventName triggerEvent;

		[SerializeField] private List<GameObject> destroyObjects;
		[SerializeField] private List<GameObject> enableObjects;
		[SerializeField] private List<GameObject> completeEventObjects;

		private bool _wasSubscribed;

		private void Awake()
		{
			if (GameData.instance.IsEventCompleted(triggerEvent))
			{
				OnEventCompleted();
				return;
			}

			if (enableObjects != null)
			{
				foreach (var obj in enableObjects)
				{
					if (obj == null)
						continue;
					if (obj.activeSelf)
						obj.SetActive(false);
				}
			}

			GameData.instance.OnEventCompleted += InstanceOnOnEventCompleted;
			_wasSubscribed = true;
		}

		private void OnDestroy()
		{
			if (_wasSubscribed)
				GameData.instance.OnEventCompleted -= InstanceOnOnEventCompleted;
		}

		private void InstanceOnOnEventCompleted(EventName obj)
		{
			if (obj == triggerEvent) OnEventCompleted();
		}

		private void OnEventCompleted()
		{
			if (destroyObjects != null)
			{
				foreach (var obj in destroyObjects)
					if (obj != null)
						Destroy(obj);
			}

			if (enableObjects != null)
			{
				foreach (var obj in enableObjects)
					if (obj != null)
						obj.SetActive(true);
			}

			if (completeEventObjects != null)
			{
				foreach (var obj in completeEventObjects)
				{
					if (obj == null)
						continue;
					if (obj.TryGetComponent<ICompleteEvent>(out var completeEvent))
						completeEvent.CompleteEvent();
				}
			}

			Destroy(gameObject);
		}
	}
}
