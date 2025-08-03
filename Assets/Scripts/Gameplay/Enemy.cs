using UnityEngine;

namespace Gameplay
{
    public class Enemy : MonoBehaviour
    {
        private Player _followPlayer;
        public AudioSource audioSource;

        
        [SerializeField] private float speed;
        private bool _isFollowing;

        public void SetFollowPlayer(Player followPlayer)
        {
            audioSource.Play();
            _followPlayer = followPlayer;
            _isFollowing = true;
        }

        public void StopFollow()
        {
            _isFollowing = false;
        }

        private void Awake()
        {
            audioSource  = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (_followPlayer != null )
            {
                if (_followPlayer.IsTrourchLit && _isFollowing)
                {
                    Follow(_followPlayer.transform.position);
                    return;
                }
            }
            // Follow(StartPosition);
        }

        private void Follow(Vector3 transformPosition)
        {
            var dir =  transformPosition - transform.position;
            dir.Normalize();
            
            transform.position += dir * speed * Time.deltaTime;
        }
    }
}