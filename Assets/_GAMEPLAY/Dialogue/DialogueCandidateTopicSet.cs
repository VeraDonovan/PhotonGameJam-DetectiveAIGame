using System.Collections.Generic;
using DetectiveGame.Core;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueCandidateTopicSet
    {
        public string NpcId { get; set; } = string.Empty;
        public GamePhase Phase { get; set; } = GamePhase.Intro;
        public List<DialogueCandidateTopic> Topics { get; } = new List<DialogueCandidateTopic>();
    }
}
