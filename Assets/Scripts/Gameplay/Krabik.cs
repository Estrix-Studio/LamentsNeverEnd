using System.Collections;
using Gameplay.LevelScripts;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Gameplay
{
	public class Krabik : MonoBehaviour, IInteractableObject, ICompleteEvent
	{
		[FormerlySerializedAs("EventName")]
		[SerializeField] private EventName eventName;

		[FormerlySerializedAs("DialogInfo")]
		[SerializeField] private DialogInfo dialogInfo;

		[FormerlySerializedAs("dissapearSpeed")]
		[SerializeField] private float disappearSpeed = 0.1f;

		private SpriteRenderer _renderer;

		private bool _hasBeenInteracted;
		private bool _isDisappearing;
		private bool _isSubscribedToDialogEnd;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		private void OnDestroy()
		{
			UnsubscribeFromDialogEnd();
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
			if (dialogInfo == null)
			{
				Debug.LogWarning($"{name} cannot start dialogue because no DialogInfo is assigned.", this);
				return;
			}

			if (DialogController.instance == null)
			{
				Debug.LogWarning($"{name} cannot start dialogue because no DialogController exists in the scene.", this);
				return;
			}

			Debug.Log("Start Dialog");
			_hasBeenInteracted = true;
			DialogController.instance.OnDialogEnd += InstanceOnOnDialogEnd;
			_isSubscribedToDialogEnd = true;
			DialogController.instance.StartDialog(dialogInfo);
		}

		private void InstanceOnOnDialogEnd()
		{
			UnsubscribeFromDialogEnd();
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

		private void UnsubscribeFromDialogEnd()
		{
			if (!_isSubscribedToDialogEnd || DialogController.instance == null)
				return;

			DialogController.instance.OnDialogEnd -= InstanceOnOnDialogEnd;
			_isSubscribedToDialogEnd = false;
		}
	}
}
