using System.Collections.Generic;
using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class SuperpositionObject : MonoBehaviour
    {
        private sealed class RendererState
        {
            public Renderer Renderer;
            public readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
            public bool HasColor;
            public int ColorPropertyId;
            public Color BaseColor;
            public bool HasEmission;
            public Color BaseEmission;
        }

        [Header("Roots")]
        [SerializeField] private Transform wakingRoot;
        [SerializeField] private Transform dreamRoot;

        [Header("Blend")]
        [SerializeField, Range(0f, 1f)] private float restingBlend = 0.45f;
        [SerializeField, Range(0f, 1f)] private float observedBlend = 0.8f;
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField] private Vector3 dreamOffset = new Vector3(0.18f, 0.08f, 0.14f);
        [SerializeField, Range(0f, 1f)] private float globalDreamInfluence = 0.35f;

        [Header("Dream Motion")]
        [SerializeField] private bool enableDreamScalePulse = true;
        [SerializeField] private float dreamScalePulseAmplitude = 0.05f;
        [SerializeField] private float dreamScalePulseSpeed = 1.25f;
        [SerializeField] private bool enableDreamRotationDrift = true;
        [SerializeField] private Vector3 dreamRotationDrift = new Vector3(0f, 2f, 0.6f);

        [Header("Visuals")]
        [SerializeField] private bool enableRendererFading = false;
        [SerializeField, Range(0f, 1f)] private float minimumPresence = 0.2f;
        [SerializeField] private bool enableEmissionBoost = true;
        [SerializeField] private float dreamEmissionMultiplier = 2f;

        private readonly List<RendererState> wakingRenderers = new List<RendererState>();
        private readonly List<RendererState> dreamRenderers = new List<RendererState>();

        private Vector3 wakingBasePosition;
        private Quaternion wakingBaseRotation;
        private Vector3 wakingBaseScale;
        private Vector3 dreamBasePosition;
        private Quaternion dreamBaseRotation;
        private Vector3 dreamBaseScale;
        private float currentBlend;
        private bool isObserved;
        private float? restingBlendOverride;
        private float? observedBlendOverride;
        private float? transitionSpeedOverride;

        public Transform WakingRoot
        {
            get => wakingRoot;
            set => wakingRoot = value;
        }

        public Transform DreamRoot
        {
            get => dreamRoot;
            set => dreamRoot = value;
        }

        public float RestingBlend
        {
            get => restingBlend;
            set => restingBlend = Mathf.Clamp01(value);
        }

        public float ObservedBlend
        {
            get => observedBlend;
            set => observedBlend = Mathf.Clamp01(value);
        }

        public float TransitionSpeed
        {
            get => transitionSpeed;
            set => transitionSpeed = Mathf.Max(0.01f, value);
        }

        public Vector3 DreamOffset
        {
            get => dreamOffset;
            set => dreamOffset = value;
        }

        public float GlobalDreamInfluence
        {
            get => globalDreamInfluence;
            set => globalDreamInfluence = Mathf.Clamp01(value);
        }

        private void Reset()
        {
            if (transform.childCount > 0)
            {
                wakingRoot = transform.GetChild(0);
            }

            if (transform.childCount > 1)
            {
                dreamRoot = transform.GetChild(1);
            }
        }

        private void OnEnable()
        {
            CacheInitialState();
            currentBlend = ResolveRestingBlend();
            ApplyBlend(currentBlend, true);
        }

        private void OnDisable()
        {
            RestoreInitialState();
        }

        private void OnValidate()
        {
            transitionSpeed = Mathf.Max(0.01f, transitionSpeed);
        }

        private void LateUpdate()
        {
            if (wakingRoot == null || dreamRoot == null)
            {
                return;
            }

            float targetBlend = isObserved ? ResolveObservedBlend() : ResolveRestingBlend();
            if (DreamStateManager.Instance != null)
            {
                targetBlend = Mathf.Lerp(targetBlend, 1f, DreamStateManager.Instance.CurrentIntensity * globalDreamInfluence);
            }

            currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, ResolveTransitionSpeed() * Time.deltaTime);
            ApplyBlend(currentBlend, false);
        }

        public void SetRoots(Transform waking, Transform dream)
        {
            wakingRoot = waking;
            dreamRoot = dream;
            CacheInitialState();
            ApplyBlend(currentBlend, true);
        }

        public void SetObservationState(bool observed, float? observedBlendValue = null, float? restingBlendValue = null, float? speedOverride = null)
        {
            isObserved = observed;
            observedBlendOverride = observedBlendValue;
            restingBlendOverride = restingBlendValue;
            transitionSpeedOverride = speedOverride;
        }

        public void SetBlendImmediate(float blend)
        {
            currentBlend = Mathf.Clamp01(blend);
            ApplyBlend(currentBlend, true);
        }

        private float ResolveRestingBlend()
        {
            return restingBlendOverride ?? restingBlend;
        }

        private float ResolveObservedBlend()
        {
            return observedBlendOverride ?? observedBlend;
        }

        private float ResolveTransitionSpeed()
        {
            return transitionSpeedOverride ?? transitionSpeed;
        }

        private void CacheInitialState()
        {
            wakingRenderers.Clear();
            dreamRenderers.Clear();

            if (wakingRoot != null)
            {
                wakingBasePosition = wakingRoot.localPosition;
                wakingBaseRotation = wakingRoot.localRotation;
                wakingBaseScale = wakingRoot.localScale;
                CacheRenderers(wakingRoot, wakingRenderers);
            }

            if (dreamRoot != null)
            {
                dreamBasePosition = dreamRoot.localPosition;
                dreamBaseRotation = dreamRoot.localRotation;
                dreamBaseScale = dreamRoot.localScale;
                CacheRenderers(dreamRoot, dreamRenderers);
            }
        }

        private void RestoreInitialState()
        {
            if (wakingRoot != null)
            {
                wakingRoot.localPosition = wakingBasePosition;
                wakingRoot.localRotation = wakingBaseRotation;
                wakingRoot.localScale = wakingBaseScale;
            }

            if (dreamRoot != null)
            {
                dreamRoot.localPosition = dreamBasePosition;
                dreamRoot.localRotation = dreamBaseRotation;
                dreamRoot.localScale = dreamBaseScale;
            }

            RestoreRendererState(wakingRenderers);
            RestoreRendererState(dreamRenderers);
        }

        private void CacheRenderers(Transform root, List<RendererState> destination)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Material sharedMaterial = renderer.sharedMaterial;
                RendererState state = new RendererState
                {
                    Renderer = renderer
                };

                if (sharedMaterial.HasProperty("_BaseColor"))
                {
                    state.HasColor = true;
                    state.ColorPropertyId = Shader.PropertyToID("_BaseColor");
                    state.BaseColor = sharedMaterial.GetColor("_BaseColor");
                }
                else if (sharedMaterial.HasProperty("_Color"))
                {
                    state.HasColor = true;
                    state.ColorPropertyId = Shader.PropertyToID("_Color");
                    state.BaseColor = sharedMaterial.GetColor("_Color");
                }

                if (sharedMaterial.HasProperty("_EmissionColor"))
                {
                    state.HasEmission = true;
                    state.BaseEmission = sharedMaterial.GetColor("_EmissionColor");
                }

                destination.Add(state);
            }
        }

        private void ApplyBlend(float blend, bool immediate)
        {
            if (wakingRoot == null || dreamRoot == null)
            {
                return;
            }

            float wakingWeight = 1f - blend;
            float dreamWeight = blend;
            float pulse = immediate ? 0f : Mathf.Sin(Time.time * dreamScalePulseSpeed * Mathf.PI * 2f);

            wakingRoot.localPosition = wakingBasePosition - dreamOffset * (blend * 0.12f);
            wakingRoot.localRotation = wakingBaseRotation;
            wakingRoot.localScale = wakingBaseScale;

            Vector3 dreamPositionalOffset = Vector3.Lerp(dreamOffset, Vector3.zero, dreamWeight);
            dreamRoot.localPosition = dreamBasePosition + dreamPositionalOffset;

            if (enableDreamRotationDrift)
            {
                Vector3 angularDrift = dreamRotationDrift * dreamWeight * (immediate ? 0f : Mathf.Sin(Time.time * 0.8f));
                dreamRoot.localRotation = dreamBaseRotation * Quaternion.Euler(angularDrift);
            }
            else
            {
                dreamRoot.localRotation = dreamBaseRotation;
            }

            float pulseScale = enableDreamScalePulse ? 1f + pulse * dreamScalePulseAmplitude * dreamWeight : 1f;
            dreamRoot.localScale = dreamBaseScale * pulseScale;

            ApplyRendererWeights(wakingRenderers, wakingWeight, false);
            ApplyRendererWeights(dreamRenderers, dreamWeight, true);
        }

        private void ApplyRendererWeights(List<RendererState> renderers, float weight, bool dreamSide)
        {
            float visibility = enableRendererFading ? Mathf.Lerp(minimumPresence, 1f, weight) : 1f;
            float emissionMultiplier = dreamSide && enableEmissionBoost ? Mathf.Lerp(1f, dreamEmissionMultiplier, weight) : 1f;

            foreach (RendererState state in renderers)
            {
                if (state.Renderer == null)
                {
                    continue;
                }

                state.Renderer.GetPropertyBlock(state.PropertyBlock);

                if (state.HasColor)
                {
                    Color color = state.BaseColor;
                    color.a *= visibility;
                    state.PropertyBlock.SetColor(state.ColorPropertyId, color);
                }

                if (state.HasEmission)
                {
                    state.PropertyBlock.SetColor("_EmissionColor", state.BaseEmission * emissionMultiplier);
                }

                state.Renderer.SetPropertyBlock(state.PropertyBlock);
            }
        }

        private static void RestoreRendererState(List<RendererState> renderers)
        {
            foreach (RendererState state in renderers)
            {
                if (state.Renderer == null)
                {
                    continue;
                }

                state.PropertyBlock.Clear();
                state.Renderer.SetPropertyBlock(state.PropertyBlock);
            }
        }
    }
}
