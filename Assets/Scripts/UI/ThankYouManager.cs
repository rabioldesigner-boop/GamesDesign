using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TombOfServilii
{
    public class ThankYouManager : MonoBehaviour
    {
        public static ThankYouManager Instance { get; private set; }

        [Header("UI Panels")]
        [Tooltip("The main container panel of the Thank You screen.")]
        [SerializeField] private GameObject thankYouPanel;

        [Tooltip("CanvasGroup component on the thank you panel to handle the fade-in animation.")]
        [SerializeField] private CanvasGroup thankYouCanvasGroup;

        [Header("Settings")]
        [Tooltip("Time in seconds to fade in the panel.")]
        [SerializeField] private float fadeDuration = 1.0f;

        [Tooltip("Optional AudioSource to play a soft background ambient track during the end screen.")]
        [SerializeField] private AudioSource optionalAmbientSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Ensure thank you panel starts hidden
            if (thankYouPanel != null)
            {
                thankYouPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Displays the Thank You panel and fades it in smoothly.
        /// </summary>
        public void ShowThankYou()
        {
            if (thankYouPanel == null)
            {
                Debug.LogError("ThankYouManager: Thank You Panel reference is missing!");
                return;
            }

            // 1. Activate the panel
            thankYouPanel.SetActive(true);

            // 2. Play soft ambient track if assigned
            if (optionalAmbientSource != null)
            {
                optionalAmbientSource.Play();
            }

            // 3. Start the fade-in animation
            if (thankYouCanvasGroup != null)
            {
                StartCoroutine(FadeIn(thankYouCanvasGroup, fadeDuration));
            }
            else
            {
                Debug.LogWarning("ThankYouManager: CanvasGroup reference is missing. Smooth fade-in bypassed.");
            }
        }

        /// <summary>
        /// Triggered by the Exit Button.
        /// </summary>
        public void OnExitClicked()
        {
            Debug.Log("ThankYouManager: Exit requested.");

#if UNITY_EDITOR
            // Exit Play Mode in the Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Quit the standalone application build
            Application.Quit();
#endif
        }

        /// <summary>
        /// Triggered by the Restart Button. Reloads the active scene.
        /// </summary>
        public void OnRestartClicked()
        {
            Debug.Log("ThankYouManager: Restarting virtual tour...");
            
            // Reload the current active scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Reusable Coroutine that lerps CanvasGroup alpha from 0 to 1.
        /// </summary>
        private IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            cg.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            cg.alpha = 1f;
        }
    }
}
