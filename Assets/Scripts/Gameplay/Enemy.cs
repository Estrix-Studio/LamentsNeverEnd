using UnityEngine;

namespace Gameplay
{
	public class Enemy : MonoBehaviour
	{
		public AudioSource audioSource;


		[SerializeField] private float speed;
		private Player _followPlayer;
		private bool _isFollowing;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (_followPlayer == null) return;
			if (_followPlayer.isTorchLit && _isFollowing)
				Follow(_followPlayer.transform.position);
			// Follow(StartPosition);
		}

		public void SetFollowPlayer(Player followPlayer)
		{
			if (audioSource != null)
				audioSource.Play();
			_followPlayer = followPlayer;
			_isFollowing = true;
		}

		public void StopFollow()
		{
			_isFollowing = false;
		}

		private void Follow(Vector3 transformPosition)
		{
			var dir = transformPosition - transform.position;
			dir.Normalize();

			transform.position += dir * (speed * Time.deltaTime);
		}
	}
}
