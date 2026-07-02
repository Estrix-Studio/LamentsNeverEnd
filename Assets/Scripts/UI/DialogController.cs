using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utility;
using Yarn;
using Yarn.Compiler;
using Yarn.Unity;

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
		private Dialogue _yarnDialogue;
		private IDictionary<string, StringInfo> _yarnStringTable;
		private InMemoryVariableStorage _variableStorage;
		private bool _isYarnDialogRunning;
		private bool _isDialogOpen;
		public static DialogController instance { get; private set; }

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				enabled = false;
				return;
			}

			instance = this;
		}

		private void Start()
		{
			if (dialogPanel != null)
				dialogPanel.SetActive(false);
			else
				Debug.LogWarning("DialogController has no dialog panel assigned.", this);

			if (nextButton != null)
				nextButton.onClick.AddListener(DisplayNextPhrase);
		}

		public event Action OnDialogStart;
		public event Action OnDialogEnd;

		public void StartDialog(DialogInfo dialogInfo)
		{
			if (dialogInfo == null)
				return;

			if (dialogInfo.HasYarnScript && TryStartYarnDialog(dialogInfo))
				return;

			StartDialog(dialogInfo.phrases);
		}

		public void StartDialog(List<string> dialogPhrases)
		{
			_isYarnDialogRunning = false;
			_yarnDialogue = null;
			_isDialogOpen = true;
			OnDialogStart?.Invoke();
			_phrases.Clear();

			if (dialogPhrases == null)
			{
				EndDialog();
				return;
			}

			foreach (var phrase in dialogPhrases)
				_phrases.Enqueue(phrase);

			DisplayNextPhrase();
		}

		public void DisplayNextPhrase()
		{
			if (_phrases.Count == 0)
			{
				if (_isYarnDialogRunning)
				{
					ContinueYarnDialog();
					return;
				}

				EndDialog();
				return;
			}

			if (dialogPanel != null && !dialogPanel.activeSelf)
				dialogPanel.SetActive(true);

			var phrase = _phrases.Dequeue();

			if (_typingCoroutine != null)
				StopCoroutine(_typingCoroutine);

			_typingCoroutine = StartCoroutine(TypePhrase(phrase));
		}

		private IEnumerator TypePhrase(string phrase)
		{
			if (dialogText == null)
				yield break;

			dialogText.text = "";
			foreach (var c in phrase)
			{
				dialogText.text += c;
				yield return new WaitForSeconds(typingSpeed);
			}
		}

		private void EndDialog()
		{
			if (!_isDialogOpen)
				return;

			_isYarnDialogRunning = false;
			_isDialogOpen = false;
			if (dialogPanel != null)
				dialogPanel.SetActive(false);
			if (dialogText != null)
				dialogText.text = "";
			Debug.Log("Dialog finished");
			OnDialogEnd?.Invoke();
		}

		private bool TryStartYarnDialog(DialogInfo dialogInfo)
		{
			var job = CompilationJob.CreateFromString(dialogInfo.YarnScript.name, dialogInfo.YarnScript.text);
			job.CompilationType = CompilationJob.Type.FullCompilation;

			var result = Compiler.Compile(job);
			if (result.ContainsErrors)
			{
				foreach (var diagnostic in result.Diagnostics)
					Debug.LogError(diagnostic.ToString(), dialogInfo);
				return false;
			}

			_yarnStringTable = result.StringTable;
			_variableStorage ??= gameObject.GetComponent<InMemoryVariableStorage>();
			_variableStorage ??= gameObject.AddComponent<InMemoryVariableStorage>();

			_yarnDialogue = new Dialogue(_variableStorage);
			_yarnDialogue.SetProgram(result.Program);
			_yarnDialogue.LineHandler = HandleYarnLine;
			_yarnDialogue.CommandHandler = HandleYarnCommand;
			_yarnDialogue.OptionsHandler = HandleYarnOptions;
			_yarnDialogue.DialogueCompleteHandler = EndDialog;

			_phrases.Clear();
			_isYarnDialogRunning = true;
			_isDialogOpen = true;
			OnDialogStart?.Invoke();
			try
			{
				_yarnDialogue.SetNode(dialogInfo.YarnNodeName);
				ContinueYarnDialog();
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogError($"Could not start Yarn node '{dialogInfo.YarnNodeName}' from {dialogInfo.name}: {exception.Message}", dialogInfo);
				EndDialog();
				return false;
			}
		}

		private void ContinueYarnDialog()
		{
			if (_yarnDialogue == null)
			{
				EndDialog();
				return;
			}

			_yarnDialogue.Continue();
		}

		private void HandleYarnLine(Line line)
		{
			if (!_yarnStringTable.TryGetValue(line.ID, out var stringInfo) || string.IsNullOrEmpty(stringInfo.text))
			{
				Debug.LogWarning($"Yarn line {line.ID} has no text.");
				_yarnDialogue.Continue();
				return;
			}

			_phrases.Enqueue(ApplySubstitutions(stringInfo.text, line.Substitutions));
			DisplayNextPhrase();
		}

		private void HandleYarnCommand(Command command)
		{
			Debug.LogWarning($"Unhandled Yarn command: {command.Text}");
			_yarnDialogue.Continue();
		}

		private void HandleYarnOptions(OptionSet options)
		{
			if (!options.Options.Any(option => option.IsAvailable))
			{
				EndDialog();
				return;
			}

			var selectedOption = options.Options.First(option => option.IsAvailable);
			_yarnDialogue.SetSelectedOption(selectedOption.ID);
			_yarnDialogue.Continue();
		}

		private static string ApplySubstitutions(string text, string[] substitutions)
		{
			if (substitutions == null)
				return text;

			for (var i = 0; i < substitutions.Length; i++)
				text = text.Replace("{" + i + "}", substitutions[i]);

			return text;
		}
	}
}
