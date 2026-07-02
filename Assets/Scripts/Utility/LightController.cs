using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Utility
{
	public class LightController : MonoBehaviour
	{
		[SerializeField] public TorchData torchData;

		//public float defaultIntensity;
		private Light2D _torchlight;
		private float _maxIntensity;

		private float _minIntensity;


		// Switcher for coroutine
		private bool isRunning { get; set; }

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		private void Start()
		{
			if (!_torchlight)
				_torchlight = GetComponent<Light2D>();

			if (_torchlight == null)
			{
				Debug.LogWarning("Light2D component not found on this GameObject.", this);
				enabled = false;
				return;
			}

			if (torchData == null)
			{
				Debug.LogWarning("TorchData is not assigned on LightController.", this);
				enabled = false;
				return;
			}

			_torchlight.intensity = torchData.defaultIntensity;
			// Debug.Log($"Intensity: {_torchlight.intensity}");
			isRunning = true;
			StartCoroutine(Flickering());
		}

		private void OnDisable()
		{
			isRunning = false;
		}


		private IEnumerator Flickering()
		{
			while (isRunning)
			{
				var cooldownMin = Mathf.Min(torchData.flickCooldown.x, torchData.flickCooldown.y);
				var cooldownMax = Mathf.Max(torchData.flickCooldown.x, torchData.flickCooldown.y);
				var cooldown = Random.Range(cooldownMin, cooldownMax);
				yield return new WaitForSeconds(cooldown);
				// Debug.Log("Coldown finished");
				// torchData.flickRange = new Vector2( torchData.defaultIntensity - torchData.flickRange.x,
				//     torchData.defaultIntensity + torchData.flickRange.y);

				// Start of flickering
				var minIntensity = Mathf.Min(torchData.flickRange.x, torchData.flickRange.y);
				var maxIntensity = Mathf.Max(torchData.flickRange.x, torchData.flickRange.y);
				var target = Random.Range(minIntensity, maxIntensity);

				yield return StartCoroutine(IntensityRandomizer(_torchlight.intensity, target, Mathf.Max(0.01f, torchData.flickTime)));

				// Return to default intensity
				// yield return StartCoroutine(IntensityRandomizer(_torchlight.intensity, torchData.defaultIntensity,
				//     torchData.flickBackTime));
			}
		}


		private IEnumerator IntensityRandomizer(float from, float to, float duration)
		{
			// Debug.Log($"Start Intensity Randomizer {from}, {to}, {duration}");
			float time = 0;
			while (time < duration)
			{
				_torchlight.intensity = Mathf.Lerp(from, to, time / duration);
				time += Time.deltaTime;
				yield return null;
			}
			// Debug.Log($"Exit Intensity Randomizer {from}, {to}, {duration}");
		}
	}
}
