using UnityEngine;

namespace Gameplay
{
	public class CameraController : MonoBehaviour
	{
		[SerializeField] private GameObject player;
		[SerializeField] private Vector3 offset = new(0, 0, -10);
		[SerializeField] private float smoothing = 0.5f;

		// Used for cutscenes
		private bool isCutsceneActive { get; set; }

		private void Awake()
		{
			if (player == null) Debug.LogWarning($"Player not found. Script: {name}");
		}

		private void LateUpdate()
		{
			if (!isCutsceneActive) CameraSmooth();
		}


		private void CameraSmooth()
		{
			var desiredPosition = player.transform.position + offset;
			var smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothing * Time.deltaTime);
			transform.position = smoothedPosition;
		}

		public void SetCutsceneActive(bool active)
		{
			isCutsceneActive = active;
		}
	}
}