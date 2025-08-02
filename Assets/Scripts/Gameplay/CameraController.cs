using UnityEngine;

namespace Gameplay
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
        [SerializeField] private float smoothing = 0.5f;
        
        // Used for cutscenes
        public bool isCutsceneActive { get; set; } = false;

        private void Awake()
        {
            if (player == null) Debug.LogWarning($"Player not found. Script: {this.name}");
        }

        private void LateUpdate()
        {
            if (!isCutsceneActive) CameraSmooth();
        }


        private void CameraSmooth()
        {
            Vector3 desiredPosition = player.transform.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothing * Time.deltaTime);
            transform.position = smoothedPosition;
        }
        
        public void SetCutsceneActive(bool active) => isCutsceneActive = active;
    }
}