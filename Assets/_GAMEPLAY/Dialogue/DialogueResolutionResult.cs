using System.Collections.Generic;

namespace DetectiveGame.Gameplay.Dialogue
{
    public sealed class DialogueResolutionResult
    {
        public DialogueResolutionType ResolutionType { get; set; } = DialogueResolutionType.None;
        public int AnnoyanceDelta { get; set; }
        public int NewAnnoyance { get; set; }
        public int PressureDelta { get; set; }
        public int NewPressure { get; set; }
        public List<string> UnlockedFactIds { get; } = new List<string>();
        public List<string> UnlockedStatementIds { get; } = new List<string>();
        public List<string> UnlockedLayerIds { get; } = new List<string>();
        public List<string> UnlockedTokenIds { get; } = new List<string>();
        public string PunishReason { get; set; } = string.Empty;
    }
}
