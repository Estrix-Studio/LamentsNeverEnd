using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = LamentsNeverEnd.Scripts.Scene;

public class MainMenu : MonoBehaviour
{

    public void GoToGameplay()
    {
        SceneManager.LoadScene((int)Scene.Gameplay);
    }
}
