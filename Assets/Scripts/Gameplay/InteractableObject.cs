using System.Collections;
using Gameplay.LevelScripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
	public class InteractableObject : MonoBehaviour, IInteractableObject, ICompleteEvent
	{
		[FormerlySerializedAs("EventName")]
		[SerializeField] private EventName eventName;

		[FormerlySerializedAs("dissapearSpeed")]
		[SerializeField] private float disappearSpeed = 1f;

		private SpriteRenderer _renderer;

		private bool _hasBeenInteracted;
		private bool _isDisappearing;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		public void CompleteEvent()
		{
			if (_isDisappearing)
				return;
			_isDisappearing = true;

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
			if (_renderer == null)
			{
				Destroy(gameObject);
				yield break;
			}

			if (disappearSpeed <= 0)
			{
				Destroy(gameObject);
				yield break;
			}

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
