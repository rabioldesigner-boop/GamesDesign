using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TombOfServilii
{
    public class QuizManager : MonoBehaviour
    {
        public static QuizManager Instance { get; private set; }

        [Header("Quiz Content")]
        [Tooltip("The 5 QuestionData ScriptableObjects corresponding to the quiz questions.")]
        [SerializeField] private QuestionData[] questions = new QuestionData[5];

        [Header("UI Panels")]
        [Tooltip("The main container panel of the Quiz UI.")]
        [SerializeField] private GameObject quizPanel;

        [Tooltip("The container panel displaying the final scores and review key.")]
        [SerializeField] private GameObject resultsPanel;

        [Tooltip("The container panel displaying the Thank You screen.")]
        [SerializeField] private GameObject thankYouPanel;

        [Header("Quiz Controls & Displays")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];
        [SerializeField] private Button nextButton;

        [Header("Results Displays")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text correctAnswersText;

        [Header("Button Colors")]
        [SerializeField] private Color defaultButtonColor = Color.white;
        [SerializeField] private Color correctButtonColor = new Color(0.18f, 0.80f, 0.44f); // #2ECC71 Green
        [SerializeField] private Color wrongButtonColor = new Color(0.91f, 0.30f, 0.24f);   // #E74C3C Red

        private int currentIndex = 0;
        private int score = 0;

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
            // Ensure quiz panels start hidden
            if (quizPanel != null) quizPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (thankYouPanel != null) thankYouPanel.SetActive(false);

            // Hook up next button event
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        /// <summary>
        /// Starts the quiz, resets scores and index, and presents the first question.
        /// </summary>
        public void BeginQuiz()
        {
            score = 0;
            currentIndex = 0;

            if (quizPanel != null) quizPanel.SetActive(true);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (thankYouPanel != null) thankYouPanel.SetActive(false);

            ShowQuestion();
        }

        /// <summary>
        /// Populates the UI with the current question data.
        /// </summary>
        private void ShowQuestion()
        {
            if (currentIndex < 0 || currentIndex >= questions.Length)
            {
                ShowResults();
                return;
            }

            QuestionData q = questions[currentIndex];
            if (q == null)
            {
                Debug.LogError($"QuizManager: Question data at index {currentIndex} is missing!");
                return;
            }

            // Update text elements
            if (progressText != null)
            {
                progressText.text = $"Question {currentIndex + 1} of {questions.Length}";
            }

            if (questionText != null)
            {
                questionText.text = q.questionText;
            }

            // Reset buttons
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null || answerTexts[i] == null) continue;

                // Label button: e.g. "A. Option Text"
                char optionLetter = (char)('A' + i);
                answerTexts[i].text = $"{optionLetter}. {q.options[i]}";

                // Reset appearance
                answerButtons[i].interactable = true;
                Image btnImage = answerButtons[i].GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = defaultButtonColor;
                }

                // Add dynamic click listener
                int index = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }

            // Hide Next button until an option is chosen
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Invoked when the player selects an answer. Highlights correct/incorrect choices.
        /// </summary>
        private void OnAnswerSelected(int index)
        {
            QuestionData q = questions[currentIndex];
            int correctIndex = q.correctAnswerIndex;

            // Disable all answer buttons to lock choice
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i].interactable = false;
                }
            }

            // Highlight choice
            if (index == correctIndex)
            {
                score++;
                SetButtonColor(index, correctButtonColor);
            }
            else
            {
                SetButtonColor(index, wrongButtonColor);
                SetButtonColor(correctIndex, correctButtonColor); // Show correct answer
            }

            // Show Next button
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }
        }

        private void SetButtonColor(int btnIndex, Color color)
        {
            if (btnIndex >= 0 && btnIndex < answerButtons.Length && answerButtons[btnIndex] != null)
            {
                Image img = answerButtons[btnIndex].GetComponent<Image>();
                if (img != null)
                {
                    img.color = color;
                }
            }
        }

        /// <summary>
        /// Invoked when the Next button is clicked. Advances the quiz or displays results.
        /// </summary>
        public void OnNextClicked()
        {
            currentIndex++;
            if (currentIndex < questions.Length)
            {
                ShowQuestion();
            }
            else
            {
                ShowResults();
            }
        }

        /// <summary>
        /// Displays the results panel with the final score and correct answer key.
        /// </summary>
        private void ShowResults()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Results);
            }
            else
            {
                if (quizPanel != null) quizPanel.SetActive(false);
                if (resultsPanel != null) resultsPanel.SetActive(true);
            }

            if (scoreText != null)
            {
                scoreText.text = $"You scored {score} out of {questions.Length}";
            }

            // Build correct answer review string, e.g. "1.B   2.A   3.B   4.C   5.C"
            if (correctAnswersText != null)
            {
                string reviewKey = "";
                for (int i = 0; i < questions.Length; i++)
                {
                    char correctLetter = (char)('A' + questions[i].correctAnswerIndex);
                    reviewKey += $"{i + 1}.{correctLetter}   ";
                }
                correctAnswersText.text = reviewKey.TrimEnd();
            }
        }

        /// <summary>
        /// Invoked when the Review/Continue button is clicked in the results panel.
        /// </summary>
        public void OnReviewClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.End);
            }
            else
            {
                if (resultsPanel != null) resultsPanel.SetActive(false);

                // Interface with ThankYouManager or activate thankYouPanel directly
                MonoBehaviour thankYouMgr = GameObject.FindObjectOfType<MonoBehaviour>(); // Fallback search
                bool triggeredManager = false;

                if (thankYouMgr != null)
                {
                    var method = thankYouMgr.GetType().GetMethod("ShowThankYou");
                    if (method != null)
                    {
                        method.Invoke(thankYouMgr, null);
                        triggeredManager = true;
                    }
                }

                if (!triggeredManager)
                {
                    if (thankYouPanel != null)
                    {
                        thankYouPanel.SetActive(true);
                    }
                }
            }
        }
    }
}
