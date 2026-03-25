using UnityEngine;

namespace Oneiric.Superposition
{
    public enum ObservationMode
    {
        Proximity,
        Gaze,
        Both
    }

    public enum CollapseTargetBehavior
    {
        UseObservedBlend,
        TowardDream,
        TowardWaking
    }

    [DisallowMultipleComponent]
    public class ObservationCollapseTarget : MonoBehaviour
    {
        [SerializeField] private SuperpositionObject superpositionObject;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform observationPoint;
        [SerializeField] private ObservationMode observationMode = ObservationMode.Both;
        [SerializeField] private CollapseTargetBehavior collapseTargetBehavior = CollapseTargetBehavior.UseObservedBlend;
        [SerializeField] private float observationRadius = 4f;
        [SerializeField, Range(0f, 90f)] private float gazeAngle = 18f;
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float restingBlend = 0.45f;
        [SerializeField, Range(0f, 1f)] private float observedBlend = 0.8f;
        [SerializeField] private float evaluationInterval = 0.05f;

        private float nextEvaluationTime;

        public SuperpositionObject Superposition
        {
            get => superpositionObject;
            set => superpositionObject = value;
        }

        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        public ObservationMode Mode
        {
            get => observationMode;
            set => observationMode = value;
        }

        public CollapseTargetBehavior CollapseBehavior
        {
            get => collapseTargetBehavior;
            set => collapseTargetBehavior = value;
        }

        public float ObservationRadius
        {
            get => observationRadius;
            set => observationRadius = Mathf.Max(0.1f, value);
        }

        public float GazeAngle
        {
            get => gazeAngle;
            set => gazeAngle = Mathf.Clamp(value, 0f, 90f);
        }

        public float TransitionSpeed
        {
            get => transitionSpeed;
            set => transitionSpeed = Mathf.Max(0.01f, value);
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

        private void Reset()
        {
            superpositionObject = GetComponent<SuperpositionObject>();
        }

        private void OnValidate()
        {
            observationRadius = Mathf.Max(0.1f, observationRadius);
            gazeAngle = Mathf.Clamp(gazeAngle, 0f, 90f);
            transitionSpeed = Mathf.Max(0.01f, transitionSpeed);
            evaluationInterval = Mathf.Max(0.02f, evaluationInterval);
        }

        private void Awake()
        {
            if (superpositionObject == null)
            {
                superpositionObject = GetComponent<SuperpositionObject>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (playerTransform == null && cameraTransform != null && cameraTransform.parent != null)
            {
                playerTransform = cameraTransform.parent;
            }
        }

        private void Update()
        {
            if (superpositionObject == null)
            {
                return;
            }

            if (Time.time < nextEvaluationTime)
            {
                return;
            }

            nextEvaluationTime = Time.time + Mathf.Max(0.02f, evaluationInterval);

            bool isObserved = EvaluateObservation();
            float targetBlend = ResolveObservedBlend();
            superpositionObject.SetObservationState(isObserved, targetBlend, restingBlend, transitionSpeed);
        }

        public void AssignObserver(Transform player, Transform observerCamera)
        {
            playerTransform = player;
            cameraTransform = observerCamera;
        }

        private bool EvaluateObservation()
        {
            bool proximity = EvaluateProximity();
            bool gaze = EvaluateGaze();

            return observationMode switch
            {
                ObservationMode.Proximity => proximity,
                ObservationMode.Gaze => gaze,
                _ => proximity && gaze
            };
        }

        private bool EvaluateProximity()
        {
            if (playerTransform == null)
            {
                return false;
            }

            float distance = Vector3.Distance(playerTransform.position, ResolveObservationPosition());
            return distance <= observationRadius;
        }

        private bool EvaluateGaze()
        {
            if (cameraTransform == null)
            {
                return false;
            }

            Vector3 toTarget = ResolveObservationPosition() - cameraTransform.position;
            if (toTarget.sqrMagnitude > observationRadius * observationRadius)
            {
                return false;
            }

            float angle = Vector3.Angle(cameraTransform.forward, toTarget.normalized);
            return angle <= gazeAngle;
        }

        private Vector3 ResolveObservationPosition()
        {
            if (observationPoint != null)
            {
                return observationPoint.position;
            }

            Renderer fallbackRenderer = GetComponentInChildren<Renderer>();
            return fallbackRenderer != null ? fallbackRenderer.bounds.center : transform.position;
        }

        private float ResolveObservedBlend()
        {
            return collapseTargetBehavior switch
            {
                CollapseTargetBehavior.TowardDream => 1f,
                CollapseTargetBehavior.TowardWaking => 0f,
                _ => observedBlend
            };
        }
    }
}
