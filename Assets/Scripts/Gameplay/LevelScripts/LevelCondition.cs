using Gameplay.Data;
using UnityEngine;

namespace Gameplay.LevelScripts
{
    public abstract class LevelCondition : MonoBehaviour
    {
        [SerializeField] private EventName ConditionName;

        private bool _wasStarted = false;
        private void Awake()
        {
            if (GameData.Instance.SpawnedConditions.Contains(ConditionName))
            {
                Destroy(this.gameObject);
                return;
            }

            GameData.Instance.SpawnedConditions.Add(ConditionName);
            GameData.Instance.OnObjectInteracted += InstanceOnOnObjectInteracted;
            GameData.Instance.OnZoneEntered += InstanceOnOnZoneEntered;
            _wasStarted = true;
            
            transform.SetParent(null);
        }

        private void OnDestroy()
        {
            if (_wasStarted)
            {
                GameData.Instance.OnObjectInteracted -= InstanceOnOnObjectInteracted;
                GameData.Instance.OnZoneEntered -= InstanceOnOnZoneEntered;
            }
        }

        protected virtual void InstanceOnOnZoneEntered(CycleZoneID obj)
        {
        }

        protected virtual void InstanceOnOnObjectInteracted(string obj)
        {
        }

        protected virtual void CompleteEvent()
        {
            GameData.Instance.CompleteEvent(ConditionName);
            Destroy(this.gameObject);
        }
    }
}