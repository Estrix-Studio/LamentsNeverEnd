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
    }
}
