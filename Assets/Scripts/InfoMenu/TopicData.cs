using UnityEngine;

namespace TombOfServilii
{
    [CreateAssetMenu(fileName = "NewTopicData", menuName = "Tomb of Servilii/Topic Data")]
    public class TopicData : ScriptableObject
    {
        [Tooltip("The title of the topic (e.g. 'What is it?').")]
        public string topicTitle;

        [Tooltip("The detailed text shown in the main UI display window.")]
        [TextArea(5, 10)]
        public string contentText;

        [Tooltip("The audio voice line narration for this topic.")]
        public AudioClip narrationClip;

        [Tooltip("The subtitle text shown when this narration audio plays.")]
        [TextArea(2, 5)]
        public string subtitleText;
    }
}
