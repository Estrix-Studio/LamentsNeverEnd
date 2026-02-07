using Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
	public class MainMenu : MonoBehaviour
	{
		public void GoToGameplay()
		{
			GameData.instance.Reset();
			SceneManager.LoadScene((int)Scene.Gameplay);
		}

		public void ExitGame()
		{
			Application.Quit();
		}

		public void Options()
		{
			SceneManager.LoadScene("Options", LoadSceneMode.Single);
		}

		public void Credits()
		{
			SceneManager.LoadScene("Credits", LoadSceneMode.Single);
		}
	}
}