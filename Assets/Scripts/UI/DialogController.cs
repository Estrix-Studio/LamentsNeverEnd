using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogController : MonoBehaviour
{
    public static DialogController Instance => _instance;
    private static DialogController _instance;
    
    [Header("UI References")]
    public TextMeshProUGUI dialogText;
    public Button nextButton;
    public GameObject dialogPanel;

    [Header("Typing Settings")]
    public float typingSpeed = 0.02f;

    private Queue<string> phrases = new Queue<string>();
    private Coroutine typingCoroutine;

    public event Action OnDialogStart;
    public event Action OnDialogEnd;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    private void Start()
    {
        dialogPanel.SetActive(false);
        if (nextButton != null)
            nextButton.onClick.AddListener(DisplayNextPhrase);
    }

    public void StartDialog(List<string> dialogPhrases)
    {
        OnDialogStart?.Invoke();
        phrases.Clear();

        foreach (var phrase in dialogPhrases)
            phrases.Enqueue(phrase);

        DisplayNextPhrase();
    }

    public void DisplayNextPhrase()
    {
        if (phrases.Count == 0)
        {
            EndDialog();
            return;
        }

        if (!dialogPanel.activeSelf)
            dialogPanel.SetActive(true);
        
        string phrase = phrases.Dequeue();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypePhrase(phrase));
    }

    private IEnumerator TypePhrase(string phrase)
    {
        dialogText.text = "";
        foreach (char c in phrase)
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