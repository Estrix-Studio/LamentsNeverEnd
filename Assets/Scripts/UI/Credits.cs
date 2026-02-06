using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
	public class Credits : MonoBehaviour
	{
		public AudioSource audioSource;

		private void Update()
		{
			if (!Input.GetKeyDown(KeyCode.Escape)) return;
			audioSource.Play();
			SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
		}
	}
}