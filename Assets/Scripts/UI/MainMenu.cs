using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {

        public void GoToGameplay()
        {
            SceneManager.LoadScene((int)global::Scene.Gameplay);
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
