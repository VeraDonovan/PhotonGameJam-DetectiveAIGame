using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class DatabaseManager : MonoBehaviour
    {
        [Header("Case JSON Assets")]
        [SerializeField] private TextAsset caseMetaJson;
        [SerializeField] private TextAsset evidenceJson;
        [SerializeField] private TextAsset factsJson;
        [SerializeField] private TextAsset npcJson;
        [SerializeField] private TextAsset statementsJson;
        [SerializeField] private TextAsset truthJson;
        [SerializeField] private TextAsset endingJson;
        [SerializeField] private TextAsset[] npcAiProfileJsons;

        public CaseMetaData CaseMetaData { get; private set; }
        public EvidenceDatabase EvidenceDatabase { get; private set; }
        public FactDatabase FactDatabase { get; private set; }
        public NpcDatabase NpcDatabase { get; private set; }
        public StatementDatabase StatementDatabase { get; private set; }
        public TruthDatabase TruthDatabase { get; private set; }
        public EndingDatabase EndingDatabase { get; private set; }
        public NpcAiProfileDatabase NpcAiProfileDatabase { get; private set; }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            CaseMetaData = ParseJson<CaseMetaData>(caseMetaJson, nameof(caseMetaJson));
            EvidenceDatabase = EvidenceDatabaseBuilder.Build(ParseJson<EvidenceGraphData>(evidenceJson, nameof(evidenceJson)));
            FactDatabase = FactDatabaseBuilder.Build(ParseJson<FactGraphData>(factsJson, nameof(factsJson)));
            NpcDatabase = NpcDatabaseBuilder.Build(ParseJson<NpcSetData>(npcJson, nameof(npcJson)));
            StatementDatabase = StatementDatabaseBuilder.Build(ParseJson<StatementSetData>(statementsJson, nameof(statementsJson)));
            TruthDatabase = TruthDatabaseBuilder.Build(ParseJson<TruthData>(truthJson, nameof(truthJson)));
            EndingDatabase = EndingDatabaseBuilder.Build(ParseJson<EndingSetData>(endingJson, nameof(endingJson)));
            NpcAiProfileDatabase = NpcAiProfileDatabaseBuilder.Build(ParseNpcAiProfiles());
            IsInitialized = true;
        }

        private T ParseJson<T>(TextAsset jsonAsset, string fieldName)
        {
            if (jsonAsset == null)
            {
                throw new InvalidOperationException($"DatabaseManager requires '{fieldName}' to be assigned.");
            }

            var data = JsonUtility.FromJson<T>(jsonAsset.text);
            if (data == null)
            {
                throw new InvalidOperationException(
                    $"DatabaseManager failed to parse '{jsonAsset.name}' for '{fieldName}'.");
            }

            return data;
        }

        private NpcAiProfileData[] ParseNpcAiProfiles()
        {
            if (npcAiProfileJsons == null || npcAiProfileJsons.Length == 0)
            {
                throw new InvalidOperationException("DatabaseManager requires at least one NPC AI profile JSON asset.");
            }

            var profiles = new NpcAiProfileData[npcAiProfileJsons.Length];
            for (var i = 0; i < npcAiProfileJsons.Length; i++)
            {
                profiles[i] = ParseJson<NpcAiProfileData>(npcAiProfileJsons[i], $"{nameof(npcAiProfileJsons)}[{i}]");
                profiles[i].rawJson = npcAiProfileJsons[i].text;
            }

            ValidateNpcAiMetadataCoverage(profiles);
            return profiles;
        }

        private void ValidateNpcAiMetadataCoverage(NpcAiProfileData[] profiles)
        {
            if (CaseMetaData?.linkedDataFiles?.npcAi == null)
            {
                return;
            }

            foreach (var profile in profiles)
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.npcId))
                {
                    continue;
                }

                var expectedFileName = CaseMetaData.linkedDataFiles.npcAi.GetFileNameForNpc(profile.npcId);
                if (string.IsNullOrWhiteSpace(expectedFileName))
                {
                    throw new InvalidOperationException(
                        $"CaseMetaData.linkedDataFiles.npcAi is missing an entry for npcId '{profile.npcId}'.");
                }
            }
        }
    }
}
