using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class LossScreen : MonoBehaviour
    {
        public void BacktoMenu()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }
}