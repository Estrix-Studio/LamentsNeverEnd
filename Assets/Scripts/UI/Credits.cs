using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class Credits : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }
}