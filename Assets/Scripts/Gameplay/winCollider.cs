using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
	public class WinCollider : MonoBehaviour
	{
		private void OnTriggerEnter2D(Collider2D other)
		{
			if (other.TryGetComponent<Player>(out var player)) SceneManager.LoadScene("WinScreen");
		}
	}
}