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

			foreach (var obj in enableObjects)
				if (obj.activeSelf)
					obj.SetActive(false);

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
			foreach (var obj in destroyObjects) Destroy(obj);

			foreach (var obj in enableObjects) obj.SetActive(true);

			foreach (var obj in completeEventObjects)
				if (obj.TryGetComponent<ICompleteEvent>(out var completeEvent))
					completeEvent.CompleteEvent();
			Destroy(gameObject);
		}
	}
}