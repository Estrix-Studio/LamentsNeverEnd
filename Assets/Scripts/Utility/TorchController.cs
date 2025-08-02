using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Utility
{
    public class TorchController : MonoBehaviour
    {
        private Light2D _torchlight;
        public TorchLight torchlight;

        [SerializeField] InputActionAsset inputActions;

        [SerializeField] TorchData _torchData;

        private bool _isOn = true;
        private float _targetIntensity;

        // Input Actions
        private InputAction _dimAction;
        private InputAction _normalAction;
        private InputAction _toggleAction;
        private void Awake()
        {
        }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (!_torchlight)
                _torchlight = GetComponent<Light2D>();
            
            if (!_torchlight)
                Debug.LogWarning("Light2D component not found on this GameObject.", this);
            
            if (!torchlight)
                torchlight = GetComponent<TorchLight>();
            
            _targetIntensity = _torchData.normalIntensity;
            
            
            _toggleAction = inputActions.FindAction("Torch/Toggle");
            _dimAction = inputActions.FindAction("Torch/Dim");
            _normalAction = inputActions.FindAction("Torch/Normal");
            
            _toggleAction.Enable();
            _dimAction.Enable();
            _normalAction.Enable();
            
            _toggleAction.performed += ctx => Toggle();
        }

        // Update is called once per frame
        void Update()
        {
            if (!_isOn)
                return;
            
            if (_dimAction.ReadValue<float>() > 0) _targetIntensity = _torchData.dimIntensity;
            if (_normalAction.ReadValue<float>() > 0) _targetIntensity =  _torchData.normalIntensity;
            
            _torchlight.intensity = Mathf.MoveTowards(_torchlight.intensity, _targetIntensity,
                _torchData.transitionSpeed * Time.deltaTime);
            
            torchlight.defaultIntensity = _torchlight.intensity;
        }

        void Toggle()
        {
            _isOn = !_isOn;
            _torchlight.enabled = _isOn;
        }

        void OnDestroy()
        {
            _dimAction.Disable();
            _normalAction.Disable();
            _toggleAction.Disable();
        }
    }
}
