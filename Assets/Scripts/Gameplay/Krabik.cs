using System.Collections;
using Gameplay.LevelScripts;
using UI;
using UnityEngine;
using Utility;

namespace Gameplay
{
    public class Krabik : MonoBehaviour, IInteractableObject, ICompleteEvent
    {
        [SerializeField] private EventName EventName;

        [SerializeField] private DialogInfo DialogInfo;

        [SerializeField] private float dissapearSpeed = 0.1f;
        
        private bool hasBeenInteracted = false;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Interact()
        {
            if (hasBeenInteracted)
                return;
            Debug.Log("Start Dialog");
            hasBeenInteracted = true;
            DialogController.Instance.OnDialogEnd += InstanceOnOnDialogEnd;
            DialogController.Instance.StartDialog(DialogInfo.Phrases);
        }

        private void OnDestroy()
        {
            DialogController.Instance.OnDialogEnd -= InstanceOnOnDialogEnd;
        }

        private void InstanceOnOnDialogEnd()
        {
            GameData.Instance.CompleteEvent(EventName);
        }

        public void CompleteEvent()
        {
            StartCoroutine(Dissapear());
        }

        private IEnumerator Dissapear()
        {
            while (_renderer.color.a != 0)
            {
                var newA = _renderer.color.a - Time.deltaTime * dissapearSpeed;
                if (newA < 0)
                    newA = 0;
                _renderer.color = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, newA);
                yield return null;
            }
            Destroy(gameObject);
        }
    }

}
