using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TombOfServilii
{
    public class InfoMenuManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("The main container panel of the Info Menu UI.")]
        [SerializeField] private GameObject menuPanel;

        [Tooltip("The panel containing the quiz interface (to be enabled when quiz starts).")]
        [SerializeField] private GameObject quizPanel;

        [Tooltip("The button that triggers the quiz, revealed only after viewing all topics.")]
        [SerializeField] private Button startQuizButton;

        [Header("Display Elements")]
        [Tooltip("Text component that displays the main body of educational text.")]
        [SerializeField] private TextMeshProUGUI contentTextWindow;

        [Tooltip("Text component that displays the title of the current topic.")]
        [SerializeField] private TextMeshProUGUI titleTextWindow;

        [Header("Data & Narration")]
        [Tooltip("The 5 TopicData ScriptableObjects corresponding to the 5 topics.")]
        [SerializeField] private TopicData[] topics = new TopicData[5];

        [Tooltip("The AudioSource component used to play voice narration.")]
        [SerializeField] private AudioSource narrationAudioSource;

        private GameObject playerRoot;
        private bool[] viewedTopics = new bool[5];

        private void Start()
        {
            // Ensure UI elements start in correct states
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }

            if (startQuizButton != null)
            {
                startQuizButton.gameObject.SetActive(false);
            }

            if (titleTextWindow != null)
            {
                titleTextWindow.text = "Select a Topic";
            }

            if (contentTextWindow != null)
            {
                contentTextWindow.text = "Please click on a topic to read and listen to the information.";
            }
        }

        /// <summary>
        /// Opens the Info Menu, stores reference to player and disables player controls.
        /// </summary>
        public void ShowMenu(GameObject player)
        {
            playerRoot = player;
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Played by UI Buttons. topicIndex should be 0 to 4.
        /// </summary>
        public void PlayTopic(int topicIndex)
        {
            if (topicIndex < 0 || topicIndex >= topics.Length)
            {
                Debug.LogError($"InfoMenuManager: Invalid topic index {topicIndex}!");
                return;
            }

            TopicData topic = topics[topicIndex];
            if (topic == null)
            {
                Debug.LogError($"InfoMenuManager: Topic data at index {topicIndex} is missing!");
                return;
            }

            // 1. Update Title and Content Text UI
            if (titleTextWindow != null)
            {
                titleTextWindow.text = topic.topicTitle;
            }

            if (contentTextWindow != null)
            {
                contentTextWindow.text = topic.contentText;
            }

            // 2. Play narration Audio
            float duration = 5f; // Fallback subtitle duration
            if (AudioManager.Instance != null)
            {
                AudioClip clip = (topicIndex >= 0 && topicIndex < AudioManager.Instance.topicNarrations.Length && AudioManager.Instance.topicNarrations[topicIndex] != null)
                    ? AudioManager.Instance.topicNarrations[topicIndex]
                    : topic.narrationClip;

                if (clip != null)
                {
                    duration = AudioManager.Instance.PlayNarration(clip);
                }
            }
            else if (narrationAudioSource != null && topic.narrationClip != null)
            {
                narrationAudioSource.Stop();
                narrationAudioSource.clip = topic.narrationClip;
                narrationAudioSource.Play();
                duration = topic.narrationClip.length;
            }

            // 3. Show subtitle transcription
            if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(topic.subtitleText))
            {
                SubtitleManager.Instance.ShowSubtitle(topic.subtitleText, duration);
            }
            else
            {
                Debug.LogWarning("GateInteraction: SubtitleManager instance not found in scene!");
            }

            // 4. Mark as viewed and check completion
            viewedTopics[topicIndex] = true;
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            bool allViewed = true;
            for (int i = 0; i < viewedTopics.Length; i++)
            {
                if (!viewedTopics[i])
                {
                    allViewed = false;
                    break;
                }
            }

            if (allViewed && startQuizButton != null)
            {
                startQuizButton.gameObject.SetActive(true);
                Debug.Log("InfoMenuManager: All 5 topics viewed! 'Start Quiz' button is now unlocked.");
            }
        }

        /// <summary>
        /// Triggered by the Start Quiz Button.
        /// </summary>
        public void OnClickStartQuiz()
        {
            // Stop any active narration audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopNarration();
            }
            else if (narrationAudioSource != null)
            {
                narrationAudioSource.Stop();
            }

            // Transition state to InQuiz
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.InQuiz);
            }
            else
            {
                // Hide info menu panel
                if (menuPanel != null)
                {
                    menuPanel.SetActive(false);
                }

                // Trigger the QuizManager to begin the quiz
                if (QuizManager.Instance != null)
                {
                    QuizManager.Instance.BeginQuiz();
                }
                else if (quizPanel != null)
                {
                    // Fallback to direct activation if QuizManager isn't active
                    quizPanel.SetActive(true);
                }
            }

            Debug.Log("InfoMenuManager: Launching the quiz system...");
        }

        /// <summary>
        /// Helper to retrieve the current player root reference.
        /// </summary>
        public GameObject GetPlayerRoot()
        {
            return playerRoot;
        }
    }
}
