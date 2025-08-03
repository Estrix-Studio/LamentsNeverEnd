using UnityEngine;

namespace Gameplay
{
    public class EnemyAttackZone : MonoBehaviour
    {
        [SerializeField] private Enemy enemy;
        
        private void Awake()
        {

            if (!enemy)
            {
                enemy = FindFirstObjectByType<Enemy>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Player>(out var player))
            {
                enemy.SetFollowPlayer(player);
            }
        }

        private void OnTriggerExit2D(Collider other)
        {
            if (other.TryGetComponent<Player>(out var player))
            {
                enemy.StopFollow();
            }
        }
    }
}