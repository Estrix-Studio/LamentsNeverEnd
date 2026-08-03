using UnityEngine;

namespace LoopShift.Samples.MinimalTopDown
{
	public sealed class SampleCameraFollow : MonoBehaviour
	{
		[SerializeField] private Transform target;
		[SerializeField, Min(0f)] private float smoothTime = 0.12f;

		private Vector3 _velocity;
		private float _depth;

		private void Awake()
		{
			_depth = transform.position.z;
		}

		private void LateUpdate()
		{
			if (target == null)
				return;

			var desired = new Vector3(target.position.x, target.position.y, _depth);
			transform.position = smoothTime <= 0f
				? desired
				: Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
		}
	}
}
