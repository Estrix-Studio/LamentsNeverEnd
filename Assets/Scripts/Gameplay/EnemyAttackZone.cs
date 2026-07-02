using UnityEngine;

namespace Gameplay
{
	public class EnemyAttackZone : MonoBehaviour
	{
		[SerializeField] private Enemy enemy;

		private void Awake()
		{
			if (!enemy) enemy = FindAnyObjectByType<Enemy>();
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (enemy == null)
			{
				Debug.LogWarning($"{name} cannot start enemy follow because no Enemy is assigned or found.", this);
				return;
			}

			if (other.TryGetComponent<Player>(out var player)) enemy.SetFollowPlayer(player);
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			if (enemy != null && other.TryGetComponent<Player>(out var player)) enemy.StopFollow();
		}
	}
}
