using Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class winCollider : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            SceneManager.LoadScene("WinScreen");
        }
    }
}
