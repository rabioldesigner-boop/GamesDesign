using System.Collections;
using UnityEngine;

namespace TombOfServilii
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [Tooltip("The welcome narration voice line.")]
        public AudioClip welcomeVoice;

        [Tooltip("The door creak sound effect.")]
        public AudioClip gateSFX;

        [Tooltip("The looping background wind sound.")]
        public AudioClip ambientWind;

        [Tooltip("The 5 narration clips corresponding to the 5 educational topics.")]
        public AudioClip[] topicNarrations = new AudioClip[5];

        private Coroutine ambientFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure AudioSource components exist
            if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();
            if (narrationSource == null) narrationSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            // Setup defaults
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;

            narrationSource.loop = false;
            narrationSource.playOnAwake = false;

            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        private void Start()
        {
            // Play ambient wind audio immediately on loop at volume 0.3f
            if (ambientWind != null)
            {
                PlayAmbient(ambientWind);
            }
        }

        /// <summary>
        /// Plays narration voice line. Stops previous narration, sets clip, plays, and returns length.
        /// </summary>
        public float PlayNarration(AudioClip clip)
        {
            if (narrationSource == null) return 0f;

            narrationSource.Stop();
            narrationSource.clip = clip;

            if (clip != null)
            {
                narrationSource.Play();
                return clip.length;
            }

            return 0f;
        }

        /// <summary>
        /// Stops the current narration audio.
        /// </summary>
        public void StopNarration()
        {
            if (narrationSource != null)
            {
                narrationSource.Stop();
            }
        }

        /// <summary>
        /// Plays a sound effect once.
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Sets and plays looping ambient background music/atmosphere.
        /// </summary>
        public void PlayAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null) return;

            ambientSource.Stop();
            ambientSource.clip = clip;
            ambientSource.volume = 0.3f;
            ambientSource.Play();
        }

        /// <summary>
        /// Fades out the ambient loop source over duration.
        /// </summary>
        public void FadeAmbientOut(float duration)
        {
            if (ambientFadeCoroutine != null)
            {
                StopCoroutine(ambientFadeCoroutine);
            }
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(0f, duration));
        }

        /// <summary>
        /// Fades in the ambient loop source to volume 0.3f over duration.
        /// </summary>
        public void FadeAmbientIn(float duration)
        {
            if (ambientFadeCoroutine != null)
            {
                StopCoroutine(ambientFadeCoroutine);
            }
            ambientFadeCoroutine = StartCoroutine(FadeAmbientVolume(0.3f, duration));
        }

        private IEnumerator FadeAmbientVolume(float targetVolume, float duration)
        {
            if (ambientSource == null) yield break;

            float startVolume = ambientSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            ambientSource.volume = targetVolume;
            if (targetVolume <= 0.01f)
            {
                ambientSource.Pause();
            }
            else if (!ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }

        /// <summary>
        /// Returns true if voice narration is currently playing.
        /// </summary>
        public bool IsNarrationPlaying()
        {
            return narrationSource != null && narrationSource.isPlaying;
        }
    }
}
