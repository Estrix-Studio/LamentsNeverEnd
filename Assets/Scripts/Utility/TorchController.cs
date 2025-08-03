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

        // Input Actions
        private InputAction _dimAction;
        private InputAction _normalAction;
        private InputAction _toggleAction;
        
        private Light2D _torchlight;
        private bool _isOn = true;

        private Animator _animator;

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
            
            _toggleAction = inputActions.FindAction("Torch/Toggle");
            _dimAction = inputActions.FindAction("Torch/Dim");
            _normalAction = inputActions.FindAction("Torch/Normal");

            _toggleAction.Enable();
            _dimAction.Enable();
            _normalAction.Enable();

            _isOn = false;
            _toggleAction.performed += ToggleActionOnperformed;

            // Pass data to LightController
            if (torchlight)
                torchlight.torchData = torchData;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_isOn)
                return;

            if (_dimAction.ReadValue<float>() > 0)
            {
                _torchlight.intensity = Mathf.MoveTowards(_torchlight.intensity, torchData.dimIntensity, torchData.transitionSpeed * Time.deltaTime);
                // target = torchData.flickRange.x;
                // Debug.Log($"torchData.defaultIntensity: {torchData.defaultIntensity}");
            }

            if (_normalAction.ReadValue<float>() > 0)
            {
                // target = torchData.flickRange.y;
                _torchlight.intensity = Mathf.MoveTowards(_torchlight.intensity, torchData.normalIntensity, torchData.transitionSpeed * Time.deltaTime);
                // Debug.Log($"torchData.defaultIntensity: {torchData.defaultIntensity}");
            }

            // _torchlight.intensity = Mathf.MoveTowards(_torchlight.intensity, target, torchData.transitionSpeed * Time.deltaTime);

            // if (torchlight)
            //torchData.defaultIntensity = _torchlight.intensity;
            // Debug.Log($"TorchData.defaultIntensity: {torchData.defaultIntensity}");
        }

        private void OnDestroy()
        {
            _toggleAction.performed -= ToggleActionOnperformed;
            _dimAction.Disable();
            _normalAction.Disable();
            _toggleAction.Disable();
        }

        private void ToggleActionOnperformed(InputAction.CallbackContext obj)
        {
            Toggle();
        }

        private void Toggle()
        {
            _isOn = !_isOn;
            _torchlight.enabled = _isOn;
            _animator.SetBool(Holding, _isOn);
        }
    }
}