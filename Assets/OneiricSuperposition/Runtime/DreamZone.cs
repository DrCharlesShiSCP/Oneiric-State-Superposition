using UnityEngine;
using UnityEngine.Rendering;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class DreamZone : MonoBehaviour
    {
        [Header("Influence")]
        [SerializeField] private float radius = 6f;
        [SerializeField, Range(0f, 1f)] private float extraDreamIntensity = 0.25f;
        [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Local Effects")]
        [SerializeField] private Volume localVolume;
        [SerializeField] private float localVolumeWeight = 1f;

        private void OnEnable()
        {
            if (DreamStateManager.Instance != null)
            {
                DreamStateManager.Instance.RegisterZone(this);
            }
        }

        private void OnDisable()
        {
            if (DreamStateManager.Instance != null)
            {
                DreamStateManager.Instance.UnregisterZone(this);
            }
        }

        public float EvaluateInfluence(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, transform.position);
            if (distance >= radius || radius <= 0.01f)
            {
                SetLocalWeight(0f);
                return 0f;
            }

            float normalized = 1f - distance / radius;
            float influence = Mathf.Clamp01(falloff.Evaluate(normalized));
            SetLocalWeight(influence * localVolumeWeight);
            return influence * extraDreamIntensity;
        }

        private void SetLocalWeight(float weight)
        {
            if (localVolume != null)
            {
                localVolume.weight = weight;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
