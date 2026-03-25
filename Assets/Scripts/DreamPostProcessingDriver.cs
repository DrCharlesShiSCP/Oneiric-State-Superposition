using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class DreamPostProcessingDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DreamStateManager dreamStateManager;
        [SerializeField] private Volume targetVolume;

        [Header("Response")]
        [SerializeField] private bool updateVolumeWeight = true;
        [SerializeField, Range(0f, 1f)] private float baseWeight = 0.2f;
        [SerializeField, Range(0f, 1f)] private float maxWeight = 1f;
        [SerializeField] private float responseSpeed = 2.5f;

        [Header("Bloom")]
        [SerializeField] private float baseBloomIntensity = 0.05f;
        [SerializeField] private float maxBloomIntensity = 0.45f;

        [Header("Color")]
        [SerializeField] private float baseSaturation = -6f;
        [SerializeField] private float maxSaturation = -30f;
        [SerializeField] private float baseContrast = 4f;
        [SerializeField] private float maxContrast = 18f;
        [SerializeField] private Color baseColorFilter = new Color(0.97f, 0.99f, 1f, 1f);
        [SerializeField] private Color maxColorFilter = new Color(0.82f, 0.92f, 1f, 1f);

        [Header("Lens")]
        [SerializeField] private float baseChromaticAberration = 0.02f;
        [SerializeField] private float maxChromaticAberration = 0.16f;
        [SerializeField] private float baseVignetteIntensity = 0.12f;
        [SerializeField] private float maxVignetteIntensity = 0.34f;
        [SerializeField] private float baseLensDistortion = -0.03f;
        [SerializeField] private float maxLensDistortion = -0.16f;
        [SerializeField] private float baseFilmGrain = 0.04f;
        [SerializeField] private float maxFilmGrain = 0.18f;

        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private ChromaticAberration chromaticAberration;
        private Vignette vignette;
        private LensDistortion lensDistortion;
        private FilmGrain filmGrain;
        private float smoothedIntensity;

        private void Reset()
        {
            targetVolume = GetComponent<Volume>();
            if (dreamStateManager == null)
            {
                dreamStateManager = FindFirstObjectByType<DreamStateManager>();
            }
        }

        private void Awake()
        {
            if (targetVolume == null)
            {
                targetVolume = GetComponent<Volume>();
            }

            if (dreamStateManager == null)
            {
                dreamStateManager = FindFirstObjectByType<DreamStateManager>();
            }

            CacheVolumeComponents();
        }

        private void OnEnable()
        {
            CacheVolumeComponents();
            ApplyImmediate();
        }

        private void Update()
        {
            if (dreamStateManager == null || targetVolume == null || targetVolume.sharedProfile == null)
            {
                return;
            }

            float targetIntensity = dreamStateManager.CurrentIntensity;
            smoothedIntensity = Mathf.MoveTowards(smoothedIntensity, targetIntensity, responseSpeed * Time.deltaTime);
            Apply(smoothedIntensity);
        }

        private void CacheVolumeComponents()
        {
            if (targetVolume == null || targetVolume.sharedProfile == null)
            {
                return;
            }

            targetVolume.sharedProfile.TryGet(out bloom);
            targetVolume.sharedProfile.TryGet(out colorAdjustments);
            targetVolume.sharedProfile.TryGet(out chromaticAberration);
            targetVolume.sharedProfile.TryGet(out vignette);
            targetVolume.sharedProfile.TryGet(out lensDistortion);
            targetVolume.sharedProfile.TryGet(out filmGrain);
        }

        private void ApplyImmediate()
        {
            smoothedIntensity = dreamStateManager != null ? dreamStateManager.CurrentIntensity : 0f;
            Apply(smoothedIntensity);
        }

        private void Apply(float intensity)
        {
            if (updateVolumeWeight)
            {
                targetVolume.weight = Mathf.Lerp(baseWeight, maxWeight, intensity);
            }

            if (bloom != null)
            {
                bloom.intensity.Override(Mathf.Lerp(baseBloomIntensity, maxBloomIntensity, intensity));
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.Override(Mathf.Lerp(baseSaturation, maxSaturation, intensity));
                colorAdjustments.contrast.Override(Mathf.Lerp(baseContrast, maxContrast, intensity));
                colorAdjustments.colorFilter.Override(Color.Lerp(baseColorFilter, maxColorFilter, intensity));
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.Override(Mathf.Lerp(baseChromaticAberration, maxChromaticAberration, intensity));
            }

            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Lerp(baseVignetteIntensity, maxVignetteIntensity, intensity));
            }

            if (lensDistortion != null)
            {
                lensDistortion.intensity.Override(Mathf.Lerp(baseLensDistortion, maxLensDistortion, intensity));
            }

            if (filmGrain != null)
            {
                filmGrain.intensity.Override(Mathf.Lerp(baseFilmGrain, maxFilmGrain, intensity));
            }
        }
    }
}
