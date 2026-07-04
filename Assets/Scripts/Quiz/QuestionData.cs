using UnityEngine;

namespace TombOfServilii
{
    [CreateAssetMenu(fileName = "NewQuestionData", menuName = "Tomb of Servilii/Question Data")]
    public class QuestionData : ScriptableObject
    {
        [Tooltip("The text of the quiz question.")]
        [TextArea(2, 5)]
        public string questionText;

        [Tooltip("The 4 multiple choice options (A, B, C, D).")]
        public string[] options = new string[4];

        [Tooltip("Index of the correct answer (0=A, 1=B, 2=C, 3=D).")]
        [Range(0, 3)]
        public int correctAnswerIndex;
    }
}
