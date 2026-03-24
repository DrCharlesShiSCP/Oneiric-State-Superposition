using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class DreamStateManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Volume globalVolume;
        [SerializeField] private DreamAudioController audioController;
        [SerializeField] private ParticleSystem[] dreamParticles;

        [Header("Intensity")]
        [SerializeField, Range(0f, 1f)] private float baseDreamIntensity = 0.35f;
        [SerializeField] private bool pulseIntensity = true;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmplitude = 0.08f;
        [SerializeField] private float pulseSpeed = 0.45f;

        private readonly List<DreamZone> zones = new();
        private float[] particleBaseRates;
        private Bloom bloom;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private ColorAdjustments colorAdjustments;

        public static DreamStateManager Instance { get; private set; }

        public float CurrentIntensity { get; private set; }
        public Transform PlayerTransform => playerTransform;
        public Camera PlayerCamera => playerCamera;

        public void Configure(
            Transform player,
            Camera camera,
            Volume volume,
            DreamAudioController dreamAudio,
            ParticleSystem[] particles,
            float intensity,
            bool pulse,
            float amplitude,
            float speed)
        {
            playerTransform = player;
            playerCamera = camera;
            globalVolume = volume;
            audioController = dreamAudio;
            dreamParticles = particles;
            baseDreamIntensity = intensity;
            pulseIntensity = pulse;
            pulseAmplitude = amplitude;
            pulseSpeed = speed;

            ResolveReferences();
            CachePostProcessing();
            CacheParticles();
        }

        private void Awake()
        {
            Instance = this;
            ResolveReferences();
            CachePostProcessing();
            CacheParticles();
        }

        private void Update()
        {
            ResolveReferences();

            float pulse = pulseIntensity ? Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude : 0f;
            float zoneContribution = 0f;
            if (playerTransform != null)
            {
                for (int index = zones.Count - 1; index >= 0; index--)
                {
                    DreamZone zone = zones[index];
                    if (zone == null)
                    {
                        zones.RemoveAt(index);
                        continue;
                    }

                    zoneContribution = Mathf.Max(zoneContribution, zone.EvaluateInfluence(playerTransform.position));
                }
            }

            CurrentIntensity = Mathf.Clamp01(baseDreamIntensity + pulse + zoneContribution);
            ApplyGlobalEffects();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterZone(DreamZone zone)
        {
            if (zone != null && !zones.Contains(zone))
            {
                zones.Add(zone);
            }
        }

        public void UnregisterZone(DreamZone zone)
        {
            zones.Remove(zone);
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerTransform == null && playerCamera != null)
            {
                playerTransform = playerCamera.transform.root;
            }
        }

        private void CachePostProcessing()
        {
            bloom = null;
            vignette = null;
            chromaticAberration = null;
            colorAdjustments = null;

            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out bloom);
                globalVolume.profile.TryGet(out vignette);
                globalVolume.profile.TryGet(out chromaticAberration);
                globalVolume.profile.TryGet(out colorAdjustments);
            }
        }

        private void CacheParticles()
        {
            particleBaseRates = new float[dreamParticles != null ? dreamParticles.Length : 0];
            for (int index = 0; index < particleBaseRates.Length; index++)
            {
                ParticleSystem particleSystem = dreamParticles[index];
                if (particleSystem == null)
                {
                    continue;
                }

                particleBaseRates[index] = particleSystem.emission.rateOverTimeMultiplier;
            }
        }

        private void ApplyGlobalEffects()
        {
            if (globalVolume != null)
            {
                globalVolume.weight = CurrentIntensity;
            }

            if (bloom != null)
            {
                bloom.intensity.value = Mathf.Lerp(0.05f, 1.2f, CurrentIntensity);
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.12f, 0.34f, CurrentIntensity);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(0.01f, 0.18f, CurrentIntensity);
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(0f, -0.3f, CurrentIntensity);
                colorAdjustments.saturation.value = Mathf.Lerp(0f, -18f, CurrentIntensity);
                colorAdjustments.contrast.value = Mathf.Lerp(0f, 12f, CurrentIntensity);
            }

            if (audioController != null)
            {
                audioController.SetDreamBlend(CurrentIntensity);
            }

            if (dreamParticles != null)
            {
                for (int index = 0; index < dreamParticles.Length; index++)
                {
                    ParticleSystem particleSystem = dreamParticles[index];
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    var emission = particleSystem.emission;
                    emission.rateOverTimeMultiplier = particleBaseRates[index] * Mathf.Lerp(0.35f, 1.4f, CurrentIntensity);
                }
            }
        }
    }
}
