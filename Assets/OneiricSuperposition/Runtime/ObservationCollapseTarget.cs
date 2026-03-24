using UnityEngine;

namespace Oneiric.Superposition
{
    public class ObservationCollapseTarget : MonoBehaviour
    {
        public enum ObservationMode
        {
            Proximity,
            Gaze,
            Both
        }

        public enum CollapseResponse
        {
            TowardDream,
            TowardWaking,
            IncreaseDistortion
        }

        [Header("References")]
        [SerializeField] private SuperpositionObject superpositionObject;
        [SerializeField] private Transform player;
        [SerializeField] private Camera observationCamera;

        [Header("Observation")]
        [SerializeField] private ObservationMode mode = ObservationMode.Both;
        [SerializeField] private CollapseResponse collapseResponse = CollapseResponse.TowardDream;
        [SerializeField] private float observationRadius = 5f;
        [SerializeField, Range(0.05f, 1f)] private float gazeSensitivity = 0.35f;
        [SerializeField] private float gazeMaxDistance = 12f;
        [SerializeField] private float transitionSpeed = 2.5f;

        [Header("Blend")]
        [SerializeField, Range(0f, 1f)] private float restingBlend = 0.35f;
        [SerializeField, Range(0f, 1f)] private float observedBlend = 0.85f;
        [SerializeField, Range(0f, 1f)] private float restingDistortion = 0.25f;
        [SerializeField, Range(0f, 1f)] private float observedDistortion = 0.8f;

        private float currentObservation;

        public void Configure(
            SuperpositionObject target,
            Transform observedPlayer,
            Camera observedCamera,
            ObservationMode observationMode,
            CollapseResponse response,
            float radius,
            float sensitivity,
            float maxGazeDistance,
            float speed,
            float idleBlend,
            float focusBlend,
            float idleDistortion,
            float focusDistortion)
        {
            superpositionObject = target;
            player = observedPlayer;
            observationCamera = observedCamera;
            mode = observationMode;
            collapseResponse = response;
            observationRadius = radius;
            gazeSensitivity = sensitivity;
            gazeMaxDistance = maxGazeDistance;
            transitionSpeed = speed;
            restingBlend = idleBlend;
            observedBlend = focusBlend;
            restingDistortion = idleDistortion;
            observedDistortion = focusDistortion;
            currentObservation = 0f;
            ApplyRestingState();
        }

        private void Reset()
        {
            superpositionObject = GetComponent<SuperpositionObject>();
            observationCamera = Camera.main;
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyRestingState();
        }

        private void Update()
        {
            ResolveReferences();
            if (superpositionObject == null || player == null)
            {
                return;
            }

            float desiredObservation = EvaluateObservation();
            currentObservation = Mathf.MoveTowards(currentObservation, desiredObservation, transitionSpeed * Time.deltaTime);

            float targetBlend = collapseResponse == CollapseResponse.IncreaseDistortion
                ? restingBlend
                : Mathf.Lerp(restingBlend, observedBlend, currentObservation);
            float targetDistortion = Mathf.Lerp(restingDistortion, observedDistortion, currentObservation);

            superpositionObject.SetObservedState(targetBlend, targetDistortion);
        }

        private void OnDisable()
        {
            ApplyRestingState();
        }

        private float EvaluateObservation()
        {
            float proximity = 0f;
            float gaze = 0f;

            Vector3 anchor = superpositionObject.AnchorPosition;
            float distance = Vector3.Distance(player.position, anchor);
            if (observationRadius > 0.01f)
            {
                proximity = Mathf.Clamp01(1f - distance / observationRadius);
            }

            if (observationCamera != null && distance <= gazeMaxDistance)
            {
                Vector3 direction = (anchor - observationCamera.transform.position).normalized;
                float alignment = Vector3.Dot(observationCamera.transform.forward, direction);
                gaze = Mathf.Clamp01(Mathf.InverseLerp(1f - gazeSensitivity, 1f, alignment));

                Ray ray = new Ray(observationCamera.transform.position, observationCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, gazeMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    Transform wakingRoot = superpositionObject.WakingObject != null ? superpositionObject.WakingObject.transform : null;
                    bool hitTarget = hit.transform == transform || hit.transform.IsChildOf(transform) ||
                                     (wakingRoot != null && (hit.transform == wakingRoot || hit.transform.IsChildOf(wakingRoot)));
                    if (!hitTarget)
                    {
                        gaze *= 0.25f;
                    }
                }
            }

            return mode switch
            {
                ObservationMode.Proximity => proximity,
                ObservationMode.Gaze => gaze,
                _ => Mathf.Max(proximity, gaze)
            };
        }

        private void ResolveReferences()
        {
            if (superpositionObject == null)
            {
                superpositionObject = GetComponent<SuperpositionObject>();
            }

            if (player == null)
            {
                DreamStateManager manager = DreamStateManager.Instance;
                if (manager != null)
                {
                    player = manager.PlayerTransform;
                }
            }

            if (observationCamera == null)
            {
                DreamStateManager manager = DreamStateManager.Instance;
                if (manager != null)
                {
                    observationCamera = manager.PlayerCamera;
                }
            }

            if (observationCamera == null)
            {
                observationCamera = Camera.main;
            }

            if (player == null && observationCamera != null)
            {
                player = observationCamera.transform.root;
            }
        }

        private void ApplyRestingState()
        {
            if (superpositionObject != null)
            {
                superpositionObject.SetObservedState(restingBlend, restingDistortion);
            }
        }
    }
}
