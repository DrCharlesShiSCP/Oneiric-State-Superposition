using System.Collections.Generic;
using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class DreamStateManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float globalDreamIntensity = 0.2f;
        [SerializeField] private bool pulseOverTime = true;
        [SerializeField] private float pulseSpeed = 0.35f;
        [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.1f;

        private readonly Dictionary<DreamZone, float> activeZoneBoosts = new Dictionary<DreamZone, float>();

        public static DreamStateManager Instance { get; private set; }

        public float GlobalDreamIntensity
        {
            get => globalDreamIntensity;
            set => globalDreamIntensity = Mathf.Clamp01(value);
        }

        public bool PulseOverTime
        {
            get => pulseOverTime;
            set => pulseOverTime = value;
        }

        public float PulseSpeed
        {
            get => pulseSpeed;
            set => pulseSpeed = Mathf.Max(0f, value);
        }

        public float PulseAmplitude
        {
            get => pulseAmplitude;
            set => pulseAmplitude = Mathf.Clamp01(value);
        }

        public float CurrentIntensity
        {
            get
            {
                float pulse = pulseOverTime ? Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude : 0f;
                float zones = 0f;

                foreach (KeyValuePair<DreamZone, float> entry in activeZoneBoosts)
                {
                    zones = Mathf.Max(zones, entry.Value);
                }

                return Mathf.Clamp01(globalDreamIntensity + pulse + zones);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple DreamStateManager instances found. Keeping the newest one.", this);
            }

            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetZoneBoost(DreamZone zone, float amount)
        {
            if (zone == null)
            {
                return;
            }

            activeZoneBoosts[zone] = Mathf.Clamp01(amount);
        }

        public void ClearZoneBoost(DreamZone zone)
        {
            if (zone == null)
            {
                return;
            }

            activeZoneBoosts.Remove(zone);
        }
    }
}
