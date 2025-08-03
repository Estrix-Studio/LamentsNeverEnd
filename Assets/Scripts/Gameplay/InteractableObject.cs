using System.Collections;
using Gameplay.LevelScripts;
using UnityEngine;

namespace Gameplay
{
    public class InteractableObject : MonoBehaviour, IInteractableObject, ICompleteEvent
    {
        [SerializeField] private EventName EventName;

        [SerializeField] private float dissapearSpeed = 1f;
        
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
            hasBeenInteracted = true;
            
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