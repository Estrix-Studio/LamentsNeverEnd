using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class DialogController : MonoBehaviour
	{
		[Header("UI References")] public TextMeshProUGUI dialogText;

		public Button nextButton;
		public GameObject dialogPanel;

		[Header("Typing Settings")] public float typingSpeed = 0.02f;

		private readonly Queue<string> _phrases = new();
		private Coroutine _typingCoroutine;
		public static DialogController instance { get; private set; }

		private void Awake()
		{
			if (instance != null && instance != this)
				Destroy(gameObject);
			else
				instance = this;
		}

		private void Start()
		{
			dialogPanel.SetActive(false);
			if (nextButton != null)
				nextButton.onClick.AddListener(DisplayNextPhrase);
		}

		public event Action OnDialogStart;
		public event Action OnDialogEnd;

		public void StartDialog(List<string> dialogPhrases)
		{
			OnDialogStart?.Invoke();
			_phrases.Clear();

			foreach (var phrase in dialogPhrases)
				_phrases.Enqueue(phrase);

			DisplayNextPhrase();
		}

		public void DisplayNextPhrase()
		{
			if (_phrases.Count == 0)
			{
				EndDialog();
				return;
			}

			if (!dialogPanel.activeSelf)
				dialogPanel.SetActive(true);

			var phrase = _phrases.Dequeue();

			if (_typingCoroutine != null)
				StopCoroutine(_typingCoroutine);

			_typingCoroutine = StartCoroutine(TypePhrase(phrase));
		}

		private IEnumerator TypePhrase(string phrase)
		{
			dialogText.text = "";
			foreach (var c in phrase)
			{
				dialogText.text += c;
				yield return new WaitForSeconds(typingSpeed);
			}
		}

		private void EndDialog()
		{
			dialogPanel.SetActive(false);
			dialogText.text = "";
			Debug.Log("Dialog finished");
			OnDialogEnd?.Invoke();
		}
	}
}