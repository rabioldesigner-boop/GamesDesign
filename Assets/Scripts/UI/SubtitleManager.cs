using System.Collections;
using UnityEngine;
using TMPro;

namespace TombOfServilii
{
    public class SubtitleManager : MonoBehaviour
    {
        public static SubtitleManager Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("The TextMeshPro component used for subtitles.")]
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Tooltip("Optional CanvasGroup on the subtitle panel to handle fading.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine subtitleCoroutine;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure starts hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            else if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Displays subtitles on the screen for a set duration.
        /// </summary>
        public void ShowSubtitle(string text, float duration)
        {
            if (subtitleCoroutine != null)
            {
                StopCoroutine(subtitleCoroutine);
            }

            subtitleCoroutine = StartCoroutine(DisplaySubtitleCoroutine(text, duration));
        }

        private IEnumerator DisplaySubtitleCoroutine(string text, float duration)
        {
            if (subtitleText == null)
            {
                Debug.LogError("SubtitleManager: Subtitle Text reference is missing!");
                yield break;
            }

            // Set text
            subtitleText.text = text;
            subtitleText.gameObject.SetActive(true);

            // Fade In
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            // Display time (subtracting fade times to match total duration roughly)
            float displayTime = Mathf.Max(0.1f, duration - (fadeDuration * 2f));
            yield return new WaitForSeconds(displayTime);

            // Fade Out
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            subtitleText.gameObject.SetActive(false);
            subtitleCoroutine = null;
        }
    }
}
