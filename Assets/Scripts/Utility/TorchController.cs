using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Utility
{
	public class TorchController : MonoBehaviour
	{
		private static readonly int holding = Animator.StringToHash("Holding");
		public LightController torchlight;

		[SerializeField] private InputActionAsset inputActions;

		[SerializeField] private TorchData torchData;

		[SerializeField] private Light2D topLight;

		private Animator _animator;

		// Input Actions

		private Light2D _torchlight;
		public bool isLit { get; private set; } = true;

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

			isLit = false;
			Toggle();

			// Pass data to LightController
			if (torchlight)
				torchlight.torchData = torchData;
		}

		public void Toggle()
		{
			isLit = !isLit;
			_torchlight.enabled = isLit;
			topLight.enabled = isLit;
			_animator.SetBool(holding, isLit);
		}
	}
}