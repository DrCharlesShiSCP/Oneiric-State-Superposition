using UnityEngine;

namespace Oneiric.Superposition
{
    [RequireComponent(typeof(Collider))]
    public class DreamZone : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float zoneBoost = 0.2f;
        [SerializeField] private string requiredTag = string.Empty;

        public float ZoneBoost
        {
            get => zoneBoost;
            set => zoneBoost = Mathf.Clamp01(value);
        }

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (MatchesTarget(other))
            {
                Debug.LogWarning($"DreamZone '{name}' triggered by '{other.name}' with boost {zoneBoost:0.00}.", this);
            }

            TryApply(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryApply(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (MatchesTarget(other) && DreamStateManager.Instance != null)
            {
                DreamStateManager.Instance.ClearZoneBoost(this);
            }
        }

        private bool MatchesTarget(Component other)
        {
            return !string.IsNullOrWhiteSpace(requiredTag) ? other.CompareTag(requiredTag) : true;
        }

        private void TryApply(Component other)
        {
            if (!MatchesTarget(other) || DreamStateManager.Instance == null)
            {
                return;
            }

            DreamStateManager.Instance.SetZoneBoost(this, zoneBoost);
        }
    }
}
