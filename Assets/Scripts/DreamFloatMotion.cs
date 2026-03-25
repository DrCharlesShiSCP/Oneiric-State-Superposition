using UnityEngine;

namespace Oneiric.Superposition
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public class DreamFloatMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 localPositionOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobSpeed = 0.8f;
        [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
        [SerializeField] private float rotationAmplitude = 4f;
        [SerializeField] private float rotationSpeed = 0.55f;
        [SerializeField, Range(0f, 1f)] private float dreamIntensityInfluence = 0.5f;

        private float bobPhase;
        private float rotationPhase;
        private Vector3 lastOffset;
        private Quaternion lastRotation = Quaternion.identity;

        public Vector3 LocalPositionOffset
        {
            get => localPositionOffset;
            set => localPositionOffset = value;
        }

        private void OnEnable()
        {
            bobPhase = Random.Range(0f, Mathf.PI * 2f);
            rotationPhase = Random.Range(0f, Mathf.PI * 2f);
            lastOffset = Vector3.zero;
            lastRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            float dreamScale = 1f;
            if (DreamStateManager.Instance != null)
            {
                dreamScale += DreamStateManager.Instance.CurrentIntensity * dreamIntensityInfluence;
            }

            float bob = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f + bobPhase) * bobAmplitude * dreamScale;
            Vector3 animatedOffset = localPositionOffset * dreamScale;
            animatedOffset.y += bob;

            float rotation = Mathf.Sin(Time.time * rotationSpeed * Mathf.PI * 2f + rotationPhase) * rotationAmplitude * dreamScale;
            Quaternion driftRotation = Quaternion.AngleAxis(rotation, rotationAxis.sqrMagnitude > 0.0001f ? rotationAxis.normalized : Vector3.up);

            transform.localPosition = transform.localPosition - lastOffset + animatedOffset;
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRotation) * driftRotation;

            lastOffset = animatedOffset;
            lastRotation = driftRotation;
        }
    }
}
