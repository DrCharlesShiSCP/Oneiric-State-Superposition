using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class DreamAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource wakingSource;
        [SerializeField] private AudioSource dreamSource;
        [SerializeField] private float wakingVolume = 0.8f;
        [SerializeField] private float dreamVolume = 0.7f;
        [SerializeField] private float crossfadeSharpness = 1.5f;

        public void Configure(AudioSource waking, AudioSource dream, float wakingLevel, float dreamLevel, float sharpness = 1.5f)
        {
            wakingSource = waking;
            dreamSource = dream;
            wakingVolume = wakingLevel;
            dreamVolume = dreamLevel;
            crossfadeSharpness = sharpness;
        }

        private void Reset()
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
            if (sources.Length > 0)
            {
                wakingSource = sources[0];
            }

            if (sources.Length > 1)
            {
                dreamSource = sources[1];
            }
        }

        public void SetDreamBlend(float blend)
        {
            float clampedBlend = Mathf.Clamp01(blend);
            float wakingWeight = Mathf.Pow(1f - clampedBlend, crossfadeSharpness);
            float dreamWeight = Mathf.Pow(clampedBlend, crossfadeSharpness);

            if (wakingSource != null)
            {
                wakingSource.volume = wakingWeight * wakingVolume;
                TryAutoPlay(wakingSource);
            }

            if (dreamSource != null)
            {
                dreamSource.volume = dreamWeight * dreamVolume;
                TryAutoPlay(dreamSource);
            }
        }

        private static void TryAutoPlay(AudioSource source)
        {
            if (source.clip != null && source.loop && !source.isPlaying)
            {
                source.Play();
            }
        }
    }
}
