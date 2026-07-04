using System.Collections;
using UnityEngine;

namespace TombOfServilii
{
    public class GateInteraction : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("The main/left door transform that will swing open.")]
        [SerializeField] private Transform doorTransform;

        [Tooltip("How many degrees the main/left door will rotate when opening.")]
        [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);

        [Tooltip("Optional secondary/right door transform that will swing open.")]
        [SerializeField] private Transform secondaryDoorTransform;

        [Tooltip("How many degrees the secondary/right door will rotate when opening.")]
        [SerializeField] private Vector3 secondaryOpenRotationOffset = new Vector3(0f, -90f, 0f);

        [Tooltip("Time in seconds to open the gate.")]
        [SerializeField] private float openDuration = 1.5f;

        [Header("Audio Sources")]
        [Tooltip("Audio source for the creak sound effect.")]
        [SerializeField] private AudioSource creakAudioSource;

        [Tooltip("Audio source for the welcome voice narration.")]
        [SerializeField] private AudioSource narrationAudioSource;

        [Tooltip("Audio source for the ambient environment audio.")]
        [SerializeField] private AudioSource ambientAudioSource;

        [Header("Ambient Audio Fade Settings")]
        [Tooltip("Time in seconds to fade in the ambient audio.")]
        [SerializeField] private float ambientFadeDuration = 3f;

        [Tooltip("Target volume for the ambient audio once fully faded in.")]
        [Range(0f, 1f)]
        [SerializeField] private float ambientTargetVolume = 0.5f;

        [Header("Subtitle Settings")]
        [Tooltip("The text to show when the welcome voice plays.")]
        [SerializeField] [TextArea] private string welcomeSubtitleText = "Welcome to the Tomb of the Servilii.";

        private bool hasOpened = false;

        private void Start()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }
        }

        /// <summary>
        /// Triggers the gate swinging and audio playback sequence.
        /// </summary>
        public virtual void TriggerOpen()
        {
            if (hasOpened) return;
            hasOpened = true;

            StartCoroutine(OpenGateSequence());
        }

        private IEnumerator OpenGateSequence()
        {
            float narrationDuration = 4.0f; // Default fallback duration

            // 1. Play creak sound
            if (AudioManager.Instance != null)
            {
                AudioClip creakClip = AudioManager.Instance.gateSFX != null ? AudioManager.Instance.gateSFX : (creakAudioSource != null ? creakAudioSource.clip : null);
                if (creakClip != null)
                {
                    AudioManager.Instance.PlaySFX(creakClip);
                }
            }
            else if (creakAudioSource != null)
            {
                creakAudioSource.Play();
            }

            // 2. Play welcome narration and display subtitle
            if (AudioManager.Instance != null)
            {
                AudioClip narrationClip = AudioManager.Instance.welcomeVoice != null ? AudioManager.Instance.welcomeVoice : (narrationAudioSource != null ? narrationAudioSource.clip : null);
                if (narrationClip != null)
                {
                    narrationDuration = AudioManager.Instance.PlayNarration(narrationClip);
                }
            }
            else if (narrationAudioSource != null)
            {
                narrationAudioSource.Play();
                if (narrationAudioSource.clip != null)
                {
                    narrationDuration = narrationAudioSource.clip.length;
                }
            }

            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle(welcomeSubtitleText, narrationDuration);
            }
            else
            {
                Debug.LogWarning("GateInteraction: SubtitleManager instance not found in scene!");
            }

            // 3. Start ambient audio fade-in
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeAmbientIn(2.0f);
            }
            else if (ambientAudioSource != null)
            {
                StartCoroutine(FadeInAmbientAudio());
            }

            // 4. Smoothly swing the gate open
            Quaternion startRotationLeft = Quaternion.identity;
            Quaternion endRotationLeft = Quaternion.identity;
            bool hasLeft = (doorTransform != null);
            if (hasLeft)
            {
                startRotationLeft = doorTransform.localRotation;
                endRotationLeft = Quaternion.Euler(doorTransform.localEulerAngles + openRotationOffset);
            }

            Quaternion startRotationRight = Quaternion.identity;
            Quaternion endRotationRight = Quaternion.identity;
            bool hasRight = (secondaryDoorTransform != null);
            if (hasRight)
            {
                startRotationRight = secondaryDoorTransform.localRotation;
                endRotationRight = Quaternion.Euler(secondaryDoorTransform.localEulerAngles + secondaryOpenRotationOffset);
            }

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / openDuration;

                // Smoothstep interpolation for a premium look (slow-fast-slow acceleration profile)
                t = t * t * (3f - 2f * t);

                if (hasLeft)
                {
                    doorTransform.localRotation = Quaternion.Slerp(startRotationLeft, endRotationLeft, t);
                }
                if (hasRight)
                {
                    secondaryDoorTransform.localRotation = Quaternion.Slerp(startRotationRight, endRotationRight, t);
                }
                yield return null;
            }

            // Lock in exact target orientations at the end
            if (hasLeft) doorTransform.localRotation = endRotationLeft;
            if (hasRight) secondaryDoorTransform.localRotation = endRotationRight;
        }

        private IEnumerator FadeInAmbientAudio()
        {
            ambientAudioSource.volume = 0f;
            ambientAudioSource.loop = true;
            ambientAudioSource.Play();

            float elapsed = 0f;
            while (elapsed < ambientFadeDuration)
            {
                elapsed += Time.deltaTime;
                ambientAudioSource.volume = Mathf.Lerp(0f, ambientTargetVolume, elapsed / ambientFadeDuration);
                yield return null;
            }

            ambientAudioSource.volume = ambientTargetVolume;
        }
    }
}
