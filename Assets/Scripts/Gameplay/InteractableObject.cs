using System.Collections;
using Gameplay.LevelScripts;
using UnityEngine;

namespace Gameplay
{
	public class InteractableObject : MonoBehaviour, IInteractableObject, ICompleteEvent
	{
		[SerializeField] private EventName eventName;

		[SerializeField] private float disappearSpeed = 1f;

		private SpriteRenderer _renderer;

		private bool _hasBeenInteracted;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		public void CompleteEvent()
		{
			StartCoroutine(Dissapear());
		}

		public void Interact()
		{
			if (_hasBeenInteracted)
				return;
			_hasBeenInteracted = true;

			GameData.instance.CompleteEvent(eventName);
		}

		private IEnumerator Dissapear()
		{
			while (_renderer.color.a != 0)
			{
				var newA = _renderer.color.a - Time.deltaTime * disappearSpeed;
				if (newA < 0)
					newA = 0;
				_renderer.color = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, newA);
				yield return null;
			}

			Destroy(gameObject);
		}
	}
}