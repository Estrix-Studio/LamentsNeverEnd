using UnityEngine;
using LoopShift.Runtime;

namespace Gameplay
{
	public class Enemy : MonoBehaviour, ILevelZoneLifecycle
	{
		public AudioSource audioSource;


		[SerializeField] private float speed;
		private Player _followPlayer;
		private bool _isFollowing;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
            CaptureInitialPose();
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
            _followPlayer = null;
            if (audioSource != null) audioSource.Stop();
		}

		
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _hasInitialPose;

        private void CaptureInitialPose()
        {
            if (_hasInitialPose) return;
            _initialPosition = transform.localPosition;
            _initialRotation = transform.localRotation;
            _hasInitialPose = true;
        }

        public void OnZoneSpawned(LevelZoneEvent context)
        {
            CaptureInitialPose();
            StopFollow();
            transform.localPosition = _initialPosition;
            transform.localRotation = _initialRotation;
        }

        public void OnZoneDespawned(LevelZoneEvent context) => StopFollow();

        private void Follow(Vector3 transformPosition)
		{
			var dir = transformPosition - transform.position;
			dir.Normalize();

			transform.position += dir * (speed * Time.deltaTime);
		}
	}
}
