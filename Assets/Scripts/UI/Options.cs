using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class Options : MonoBehaviour
    {
        public AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
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