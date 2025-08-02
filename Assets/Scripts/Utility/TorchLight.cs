using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Utility
{
    public class TorchLight : MonoBehaviour
    {
        private Light2D _torchlight;
        [SerializeField] public IntensityData _intensityData;
        
        public float defaultIntensity;
        
        // Switcher for coroutine
        private bool isRunning { get; set; } = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (!_torchlight)
                _torchlight = GetComponent<Light2D>();

            defaultIntensity = _torchlight.intensity;
            
            isRunning = true;
            StartCoroutine(Flickering());
        }


        IEnumerator Flickering()
        {
            while (isRunning)
            {
                float cooldown = Random.Range(_intensityData.flickCooldown.x, _intensityData.flickCooldown.y);
                yield return new WaitForSeconds(cooldown);
                
                // Start of flickering
                float target = Random.Range(_intensityData.minIntensity, _intensityData.maxIntensity);
                yield return StartCoroutine(IntensityRandomizer(_torchlight.intensity,  target, _intensityData.flickTime));
                
                // Return to default intensity
                yield return StartCoroutine(IntensityRandomizer(_torchlight.intensity,  defaultIntensity, _intensityData.flickBackTime));
            }
        }


        IEnumerator IntensityRandomizer(float from, float to, float duration)
        {
            float time = 0;
            while (time < duration)
            {
                _torchlight.intensity = Mathf.Lerp(from, to, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
        }

        void OnDisable()
        {
            isRunning = false;
        }
    }
}
