using UnityEngine;

namespace Oneiric.Superposition
{
    public class DreamFloatMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 positionAmplitude = new Vector3(0f, 0.12f, 0f);
        [SerializeField] private Vector3 positionSpeed = new Vector3(0.7f, 1f, 0.6f);
        [SerializeField] private Vector3 rotationAmplitude = new Vector3(0f, 15f, 4f);
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0.2f, 0.5f, 0.3f);
        [SerializeField, Range(0f, 1f)] private float globalIntensityInfluence = 0.7f;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            float intensity = 1f;
            if (DreamStateManager.Instance != null)
            {
                intensity = Mathf.Lerp(0.25f, 1f, DreamStateManager.Instance.CurrentIntensity * globalIntensityInfluence + (1f - globalIntensityInfluence));
            }

            Vector3 positionOffset = new Vector3(
                Mathf.Sin(Time.time * positionSpeed.x),
                Mathf.Sin(Time.time * positionSpeed.y + 0.9f),
                Mathf.Sin(Time.time * positionSpeed.z + 1.7f));
            positionOffset = Vector3.Scale(positionOffset, positionAmplitude) * intensity;

            Vector3 rotationOffset = new Vector3(
                Mathf.Sin(Time.time * rotationSpeed.x),
                Mathf.Sin(Time.time * rotationSpeed.y + 1.1f),
                Mathf.Sin(Time.time * rotationSpeed.z + 2.2f));
            rotationOffset = Vector3.Scale(rotationOffset, rotationAmplitude) * intensity;

            transform.localPosition = initialLocalPosition + positionOffset;
            transform.localRotation = initialLocalRotation * Quaternion.Euler(rotationOffset);
        }
    }
}
