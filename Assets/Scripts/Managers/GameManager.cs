using UnityEngine;

namespace TombOfServilii
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Exploring,
            ViewingInfo,
            InQuiz,
            Results,
            End
        }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Exploring;

        [Header("Player & Hand References")]
        [Tooltip("The root GameObject of the player (must contain CharacterController, FirstPersonController, and StarterAssetsInputs).")]
        [SerializeField] private GameObject playerRoot;

        [Tooltip("The HandAnimationController component driving player hand mesh visibility and movement animations.")]
        [SerializeField] private HandAnimationController handAnim;

        [Header("UI Panel References")]
        [SerializeField] private GameObject infoMenuPanel;
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject thankYouPanel;

        public GameState CurrentState => currentState;

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
            // Automatic lookup for player root if not assigned
            if (playerRoot == null)
            {
                CharacterController cc = FindObjectOfType<CharacterController>();
                if (cc != null)
                {
                    playerRoot = cc.gameObject;
                }
                else
                {
                    playerRoot = GameObject.FindWithTag("Player");
                }
            }

            // Automatic lookup for hand animation controller if not assigned
            if (handAnim == null)
            {
                handAnim = FindObjectOfType<HandAnimationController>();
            }

            // Lock in initial state
            SetState(GameState.Exploring);
        }

        /// <summary>
        /// Swaps the current game state and executes player locking and UI layout adjustments.
        /// </summary>
        public void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"GameManager: Switched state to '{newState}'");

            switch (newState)
            {
                case GameState.Exploring:
                    // 1. Enable movement/look, lock cursor
                    PlayerControllerState.SetPlayerControlActive(playerRoot, true);
                    
                    // 2. Show hands
                    if (handAnim != null) handAnim.ShowHands();

                    // 3. Configure panel visibilities
                    TogglePanels(infoActive: false, quizActive: false, resultsActive: false, thankYouActive: false);
                    break;

                case GameState.ViewingInfo:
                    // 1. Disable movement/look, unlock cursor
                    PlayerControllerState.SetPlayerControlActive(playerRoot, false);
                    
                    // 2. Hide hands
                    if (handAnim != null) handAnim.HideHands();

                    // 3. Configure panels
                    TogglePanels(infoActive: true, quizActive: false, resultsActive: false, thankYouActive: false);
                    break;

                case GameState.InQuiz:
                    // 1. Disable movement/look, unlock cursor
                    PlayerControllerState.SetPlayerControlActive(playerRoot, false);
                    
                    // 2. Hide hands
                    if (handAnim != null) handAnim.HideHands();

                    // 3. Configure panels
                    TogglePanels(infoActive: false, quizActive: true, resultsActive: false, thankYouActive: false);
                    break;

                case GameState.Results:
                    // 1. Disable movement/look, unlock cursor
                    PlayerControllerState.SetPlayerControlActive(playerRoot, false);
                    
                    // 2. Hide hands
                    if (handAnim != null) handAnim.HideHands();

                    // 3. Configure panels
                    TogglePanels(infoActive: false, quizActive: false, resultsActive: true, thankYouActive: false);
                    break;

                case GameState.End:
                    // 1. Disable movement/look, unlock cursor
                    PlayerControllerState.SetPlayerControlActive(playerRoot, false);
                    
                    // 2. Hide hands
                    if (handAnim != null) handAnim.HideHands();

                    // 3. Configure panels
                    TogglePanels(infoActive: false, quizActive: false, resultsActive: false, thankYouActive: false);

                    // 4. Trigger Thank You Manager transition with smooth fade-in
                    if (ThankYouManager.Instance != null)
                    {
                        ThankYouManager.Instance.ShowThankYou();
                    }
                    else if (thankYouPanel != null)
                    {
                        thankYouPanel.SetActive(true); // Direct activation fallback
                    }

                    // 5. Fade out ambient sound loop over 2 seconds
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.FadeAmbientOut(2f);
                    }
                    break;
            }
        }

        private void TogglePanels(bool infoActive, bool quizActive, bool resultsActive, bool thankYouActive)
        {
            if (infoMenuPanel != null) infoMenuPanel.SetActive(infoActive);
            if (quizPanel != null) quizPanel.SetActive(quizActive);
            if (resultsPanel != null) resultsPanel.SetActive(resultsActive);
            if (thankYouPanel != null) thankYouPanel.SetActive(thankYouActive);
        }
    }
}
