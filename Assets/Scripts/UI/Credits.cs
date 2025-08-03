using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class Credits : MonoBehaviour
    {
        public AudioSource audioSource;

        private void Start()
        {
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                audioSource.Play();
                SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            }
        }
    }
}