using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class SuperpositionObject : MonoBehaviour
    {
        [Serializable]
        private sealed class MaterialState
        {
            public Renderer renderer;
            public Material[] materials;
            public Color[] baseColors;
            public Color[] emissionColors;
        }

        [Header("State References")]
        [SerializeField] private GameObject wakingObject;
        [SerializeField] private GameObject dreamObject;

        [Header("Blend")]
        [SerializeField, Range(0f, 1f)] private float baseBlend = 0.4f;
        [SerializeField, Range(0f, 1f)] private float globalBlendInfluence = 0.25f;
        [SerializeField, Range(0f, 1f)] private float globalDistortionInfluence = 0.35f;
        [SerializeField, Range(0f, 1f)] private float dreamMinimumOpacity = 0.18f;
        [SerializeField, Range(0f, 1f)] private float dreamMaximumOpacity = 0.95f;
        [SerializeField] private bool affectDreamOpacity = true;
        [SerializeField] private bool affectWakingOpacity;
        [SerializeField, Range(0f, 1f)] private float wakingMinimumOpacity = 0.75f;
        [SerializeField, Range(0f, 1f)] private float wakingMaximumOpacity = 1f;

        [Header("Dream Motion")]
        [SerializeField] private bool applyPositionDrift = true;
        [SerializeField] private Vector3 positionDriftAmplitude = new Vector3(0.05f, 0.1f, 0.05f);
        [SerializeField] private Vector3 positionDriftSpeed = new Vector3(0.7f, 0.9f, 0.6f);
        [SerializeField] private bool applyScalePulse = true;
        [SerializeField] private Vector3 scalePulseAmplitude = new Vector3(0.02f, 0.03f, 0.02f);
        [SerializeField] private float scalePulseSpeed = 0.8f;
        [SerializeField] private bool applyRotationDrift = true;
        [SerializeField] private Vector3 rotationDriftAmplitude = new Vector3(1f, 4f, 1f);
        [SerializeField] private Vector3 rotationDriftSpeed = new Vector3(0.4f, 0.8f, 0.6f);

        [Header("Emission")]
        [SerializeField] private bool animateDreamEmission = true;
        [SerializeField] private float dreamEmissionIntensity = 2.25f;
        [SerializeField] private float wakingEmissionIntensity = 0.25f;

        private readonly List<MaterialState> wakingMaterials = new();
        private readonly List<MaterialState> dreamMaterials = new();

        private Transform dreamTransform;
        private Vector3 dreamInitialLocalPosition;
        private Quaternion dreamInitialLocalRotation;
        private Vector3 dreamInitialLocalScale;

        private float observationBlend;
        private float observationDistortion;
        private bool cacheInitialized;

        public GameObject WakingObject => wakingObject;
        public GameObject DreamObject => dreamObject;

        public Vector3 AnchorPosition
        {
            get
            {
                Bounds? bounds = CalculateBounds();
                if (bounds.HasValue)
                {
                    return bounds.Value.center;
                }

                if (dreamObject != null)
                {
                    return dreamObject.transform.position;
                }

                if (wakingObject != null)
                {
                    return wakingObject.transform.position;
                }

                return transform.position;
            }
        }

        public void Configure(
            GameObject waking,
            GameObject dream,
            float blend,
            float dreamOpacityMin,
            float dreamOpacityMax,
            Vector3 driftAmplitude,
            Vector3 pulseAmplitude,
            Vector3 rotationAmplitude)
        {
            wakingObject = waking;
            dreamObject = dream;
            baseBlend = blend;
            dreamMinimumOpacity = dreamOpacityMin;
            dreamMaximumOpacity = dreamOpacityMax;
            positionDriftAmplitude = driftAmplitude;
            scalePulseAmplitude = pulseAmplitude;
            rotationDriftAmplitude = rotationAmplitude;

            InitializeCache();
        }

        private void Awake()
        {
            InitializeCache();
        }

        private void OnEnable()
        {
            InitializeCache();
        }

        private void Update()
        {
            if (!cacheInitialized)
            {
                InitializeCache();
            }

            DreamStateManager manager = DreamStateManager.Instance;
            float globalIntensity = manager != null ? manager.CurrentIntensity : 0f;

            float blend = Mathf.Clamp01(observationBlend + globalBlendInfluence * globalIntensity);
            float distortion = Mathf.Clamp01(observationDistortion + globalDistortionInfluence * globalIntensity);

            ApplyOpacity(blend);
            ApplyEmission(blend, globalIntensity);
            ApplyDreamMotion(distortion);
        }

        public void SetObservedState(float blend, float distortion)
        {
            observationBlend = Mathf.Clamp01(blend);
            observationDistortion = Mathf.Clamp01(distortion);
        }

        public void ResetObservedState()
        {
            observationBlend = baseBlend;
            observationDistortion = baseBlend;
        }

        public Bounds? CalculateBounds()
        {
            Renderer[] renderers = GetRelevantRenderers();
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private Renderer[] GetRelevantRenderers()
        {
            List<Renderer> renderers = new();
            if (wakingObject != null)
            {
                renderers.AddRange(wakingObject.GetComponentsInChildren<Renderer>(true));
            }

            if (dreamObject != null)
            {
                renderers.AddRange(dreamObject.GetComponentsInChildren<Renderer>(true));
            }

            return renderers.ToArray();
        }

        private void InitializeCache()
        {
            wakingMaterials.Clear();
            dreamMaterials.Clear();

            if (wakingObject != null)
            {
                CacheMaterialStates(wakingObject, wakingMaterials);
            }

            if (dreamObject != null)
            {
                CacheMaterialStates(dreamObject, dreamMaterials);
                dreamTransform = dreamObject.transform;
                dreamInitialLocalPosition = dreamTransform.localPosition;
                dreamInitialLocalRotation = dreamTransform.localRotation;
                dreamInitialLocalScale = dreamTransform.localScale;
            }
            else
            {
                dreamTransform = null;
            }

            observationBlend = baseBlend;
            observationDistortion = baseBlend;
            cacheInitialized = true;
        }

        private static void CacheMaterialStates(GameObject rootObject, List<MaterialState> destination)
        {
            Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                Color[] baseColors = new Color[materials.Length];
                Color[] emissionColors = new Color[materials.Length];

                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    baseColors[index] = GetBaseColor(material);
                    emissionColors[index] = GetEmissionColor(material);
                }

                destination.Add(new MaterialState
                {
                    renderer = renderer,
                    materials = materials,
                    baseColors = baseColors,
                    emissionColors = emissionColors
                });
            }
        }

        private void ApplyOpacity(float blend)
        {
            float dreamOpacity = Mathf.Lerp(dreamMinimumOpacity, dreamMaximumOpacity, blend);
            float wakingOpacity = Mathf.Lerp(wakingMinimumOpacity, wakingMaximumOpacity, 1f - blend);

            foreach (MaterialState state in dreamMaterials)
            {
                ApplyOpacityToState(state, affectDreamOpacity ? dreamOpacity : 1f);
            }

            foreach (MaterialState state in wakingMaterials)
            {
                ApplyOpacityToState(state, affectWakingOpacity ? wakingOpacity : 1f);
            }
        }

        private void ApplyEmission(float blend, float globalIntensity)
        {
            float dreamEmission = animateDreamEmission
                ? Mathf.Lerp(0.35f, dreamEmissionIntensity, Mathf.Clamp01(blend + globalIntensity * 0.5f))
                : 1f;
            float wakingEmission = Mathf.Lerp(wakingEmissionIntensity, 1f, 1f - blend * 0.5f);

            foreach (MaterialState state in dreamMaterials)
            {
                ApplyEmissionToState(state, dreamEmission);
            }

            foreach (MaterialState state in wakingMaterials)
            {
                ApplyEmissionToState(state, wakingEmission);
            }
        }

        private void ApplyDreamMotion(float distortion)
        {
            if (dreamTransform == null)
            {
                return;
            }

            Vector3 localPosition = dreamInitialLocalPosition;
            if (applyPositionDrift)
            {
                Vector3 driftSignal = new Vector3(
                    Mathf.Sin(Time.time * positionDriftSpeed.x),
                    Mathf.Sin(Time.time * positionDriftSpeed.y + 1.4f),
                    Mathf.Sin(Time.time * positionDriftSpeed.z + 2.8f));
                localPosition += Vector3.Scale(positionDriftAmplitude, driftSignal) * distortion;
            }

            Vector3 localScale = dreamInitialLocalScale;
            if (applyScalePulse)
            {
                float pulse = Mathf.Sin(Time.time * scalePulseSpeed + transform.position.sqrMagnitude * 0.05f);
                localScale += Vector3.Scale(scalePulseAmplitude, Vector3.one * pulse * distortion);
            }

            Quaternion localRotation = dreamInitialLocalRotation;
            if (applyRotationDrift)
            {
                Vector3 rotationOffset = new Vector3(
                    Mathf.Sin(Time.time * rotationDriftSpeed.x),
                    Mathf.Sin(Time.time * rotationDriftSpeed.y + 0.8f),
                    Mathf.Sin(Time.time * rotationDriftSpeed.z + 1.7f));
                rotationOffset = Vector3.Scale(rotationOffset, rotationDriftAmplitude) * distortion;
                localRotation = dreamInitialLocalRotation * Quaternion.Euler(rotationOffset);
            }

            dreamTransform.localPosition = localPosition;
            dreamTransform.localScale = localScale;
            dreamTransform.localRotation = localRotation;
        }

        private static void ApplyOpacityToState(MaterialState state, float opacity)
        {
            for (int index = 0; index < state.materials.Length; index++)
            {
                Material material = state.materials[index];
                Color baseColor = state.baseColors[index];
                baseColor.a *= opacity;

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", baseColor);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", baseColor);
                }
            }
        }

        private static void ApplyEmissionToState(MaterialState state, float intensity)
        {
            for (int index = 0; index < state.materials.Length; index++)
            {
                Material material = state.materials[index];
                if (!material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                Color emission = state.emissionColors[index] * intensity;
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }
        }

        private static Color GetBaseColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.white;
        }

        private static Color GetEmissionColor(Material material)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                return material.GetColor("_EmissionColor");
            }

            return Color.black;
        }
    }
}
