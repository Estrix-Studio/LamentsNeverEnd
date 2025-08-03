using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Utility
{
    public class TorchController : MonoBehaviour
    {
        private static readonly int Holding = Animator.StringToHash("Holding");
        public LightController torchlight;

        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private TorchData torchData;

        [SerializeField] private Light2D topLight;
        
        // Input Actions
        
        private Light2D _torchlight;
        private bool _isOn = true;

        private Animator _animator;
        public bool IsLit => _isOn;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            if (!_torchlight)
                _torchlight = GetComponent<Light2D>();

            if (!_torchlight)
                Debug.LogWarning("Light2D component not found on this GameObject.", this);

            if (!torchlight)
                torchlight = GetComponent<LightController>();

            _animator = GetComponentInParent<Animator>();
            
            _isOn = false;
            Toggle();

            // Pass data to LightController
            if (torchlight)
                torchlight.torchData = torchData;
        }

        public void Toggle()
        {
            _isOn = !_isOn;
            _torchlight.enabled = _isOn;
            topLight.enabled = _isOn;
            _animator.SetBool(Holding, _isOn);
        }
    }
}