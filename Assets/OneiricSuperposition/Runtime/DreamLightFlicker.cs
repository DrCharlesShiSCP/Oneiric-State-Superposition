using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class DreamLightFlicker : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float baseIntensity = 1.5f;
        [SerializeField] private float flickerAmplitude = 0.75f;
        [SerializeField] private float flickerSpeed = 2.25f;
        [SerializeField] private Color lowColor = new Color(0.55f, 0.75f, 1f);
        [SerializeField] private Color highColor = new Color(1f, 0.92f, 0.78f);

        private float noiseOffset;

        public void Configure(Light lightSource, float intensity)
        {
            targetLight = lightSource;
            baseIntensity = intensity;
        }

        private void Reset()
        {
            targetLight = GetComponent<Light>();
        }

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            float dreamFactor = DreamStateManager.Instance != null
                ? Mathf.Lerp(0.15f, 1f, DreamStateManager.Instance.CurrentIntensity)
                : 1f;
            float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerSpeed);
            float flicker = Mathf.Lerp(-1f, 1f, noise) * flickerAmplitude * dreamFactor;

            targetLight.intensity = Mathf.Max(0f, baseIntensity + flicker);
            targetLight.color = Color.Lerp(lowColor, highColor, noise);
        }
    }
}
