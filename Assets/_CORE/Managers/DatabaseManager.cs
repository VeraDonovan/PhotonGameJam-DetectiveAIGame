using System;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class DatabaseManager : MonoBehaviour
    {
        [Header("Case JSON Assets")]
        [SerializeField] private TextAsset evidenceJson;
        [SerializeField] private TextAsset factsJson;
        [SerializeField] private TextAsset npcJson;
        [SerializeField] private TextAsset truthJson;
        [SerializeField] private TextAsset sceneJson;
        [SerializeField] private TextAsset endingJson;

        public EvidenceDatabase EvidenceDatabase { get; private set; }
        public FactDatabase FactDatabase { get; private set; }
        public NpcDatabase NpcDatabase { get; private set; }
        public TruthDatabase TruthDatabase { get; private set; }
        public SceneDatabase SceneDatabase { get; private set; }
        public EndingDatabase EndingDatabase { get; private set; }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            EvidenceDatabase = EvidenceDatabaseBuilder.Build(ParseJson<EvidenceGraphData>(evidenceJson, nameof(evidenceJson)));
            FactDatabase = FactDatabaseBuilder.Build(ParseJson<FactGraphData>(factsJson, nameof(factsJson)));
            NpcDatabase = NpcDatabaseBuilder.Build(ParseJson<NpcSetData>(npcJson, nameof(npcJson)));
            TruthDatabase = TruthDatabaseBuilder.Build(ParseJson<TruthData>(truthJson, nameof(truthJson)));
            SceneDatabase = SceneDatabaseBuilder.Build(ParseJson<SceneLayoutData>(sceneJson, nameof(sceneJson)));
            EndingDatabase = EndingDatabaseBuilder.Build(ParseJson<EndingSetData>(endingJson, nameof(endingJson)));
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
    }
}
