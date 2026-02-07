using System.Collections;
using Gameplay.LevelScripts;
using UI;
using UnityEngine;
using Utility;

namespace Gameplay
{
	public class Krabik : MonoBehaviour, IInteractableObject, ICompleteEvent
	{
		[SerializeField] private EventName eventName;

		[SerializeField] private DialogInfo dialogInfo;

		[SerializeField] private float disappearSpeed = 0.1f;

		private SpriteRenderer _renderer;

		private bool _hasBeenInteracted;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		private void OnDestroy()
		{
			DialogController.instance.OnDialogEnd -= InstanceOnOnDialogEnd;
		}

		public void CompleteEvent()
		{
			StartCoroutine(Dissapear());
		}

		public void Interact()
		{
			if (_hasBeenInteracted)
				return;
			Debug.Log("Start Dialog");
			_hasBeenInteracted = true;
			DialogController.instance.OnDialogEnd += InstanceOnOnDialogEnd;
			DialogController.instance.StartDialog(dialogInfo.phrases);
		}

		private void InstanceOnOnDialogEnd()
		{
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