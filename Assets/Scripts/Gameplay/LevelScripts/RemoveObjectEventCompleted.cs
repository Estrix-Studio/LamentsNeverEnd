using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelScripts
{
    public class RemoveObjectEventCompleted : MonoBehaviour
    {
        [SerializeField] private EventName triggerEvent;

        private bool wasSubscribed = false;

        [SerializeField] private List<GameObject> destroyObjects;
        [SerializeField] private List<GameObject> enableObjects;
        [SerializeField] private List<GameObject> completeEventObjects;
        private void Awake()
        {
            if (GameData.Instance.IsEventCompleted(triggerEvent))
            {
                OnEventCompleted();
                return;
            }

            foreach (var obj in  enableObjects)
            {
                if (obj.activeSelf)
                    obj.SetActive(false);
            }
            
            GameData.Instance.OnEventCompleted += InstanceOnOnEventCompleted;
            wasSubscribed = true;
        }

        private void InstanceOnOnEventCompleted(EventName obj)
        {
            if (obj == triggerEvent)
            {
                OnEventCompleted();
            }
        }

        private void OnEventCompleted()
        {
            foreach (var obj in  destroyObjects)
            {
                Destroy(obj);
            }

            foreach (var obj in enableObjects)
            {
                obj.SetActive(true);   
            }

            foreach (var obj in  completeEventObjects)
            {
                if (obj.TryGetComponent<ICompleteEvent>(out var completeEvent))
                    completeEvent.CompleteEvent();
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
           if (wasSubscribed) 
            GameData.Instance.OnEventCompleted -= InstanceOnOnEventCompleted;
        }
    }
}