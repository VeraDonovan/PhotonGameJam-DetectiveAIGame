using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class EvidenceDatabaseRuntimeExample : MonoBehaviour
    {
        [SerializeField] private TextAsset evidenceJson;
        [SerializeField] private string evidenceIdToPrint = "A-1";

        private void Start()
        {
            if (evidenceJson == null)
            {
                Debug.LogError("Assign an evidence json TextAsset before running the example.", this);
                return;
            }

            var graphData = JsonUtility.FromJson<EvidenceGraphData>(evidenceJson.text);
            var evidenceDatabase = EvidenceDatabaseBuilder.Build(graphData);

            if (evidenceDatabase.TryGetEvidence(evidenceIdToPrint, out var evidenceNode))
            {
                var requirementsText = evidenceNode.requirements == null || evidenceNode.requirements.Count == 0
                    ? "None"
                    : string.Join(", ", evidenceNode.requirements);

                Debug.Log(
                    "Evidence Node\n" +
                    $"evidenceId: {evidenceNode.evidenceId}\n" +
                    $"tier: {evidenceNode.tier}\n" +
                    $"displayName: {evidenceNode.displayName}\n" +
                    $"summary: {evidenceNode.summary}\n" +
                    $"locationId: {evidenceNode.locationId}\n" +
                    $"targetNpcId: {evidenceNode.targetNpcId}\n" +
                    $"mapGroup: {evidenceNode.mapGroup}\n" +
                    $"requirements: {requirementsText}",
                    this);
                return;
            }

            Debug.LogError($"Evidence id '{evidenceIdToPrint}' was not found in the database.", this);
        }
    }
}
