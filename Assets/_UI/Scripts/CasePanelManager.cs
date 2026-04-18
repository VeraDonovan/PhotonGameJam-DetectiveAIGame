using System;
using DetectiveGame.Core;
using TMPro;
using UnityEngine;

namespace DetectiveGame.UI
{
    public sealed class CasePanelManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text caseSummaryText;

        private DatabaseManager databaseManager;

        private void Awake()
        {
            ResolveCoreReferences();
            ResolveTextReference();
            ValidateConfiguration();
            RefreshCaseSummaryText();
        }

        private void ResolveCoreReferences()
        {
            databaseManager = AppRoot.Instance.DatabaseManager;
        }

        private void ResolveTextReference()
        {
            if (caseSummaryText == null)
            {
                caseSummaryText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void ValidateConfiguration()
        {
            if (databaseManager == null)
            {
                throw new InvalidOperationException("CasePanelManager requires AppRoot.DatabaseManager.");
            }

            if (databaseManager.CaseMetaData == null)
            {
                throw new InvalidOperationException("CasePanelManager requires DatabaseManager.CaseMetaData.");
            }

            if (caseSummaryText == null)
            {
                throw new InvalidOperationException("CasePanelManager requires a TMP_Text child or assigned caseSummaryText.");
            }
        }

        private void RefreshCaseSummaryText()
        {
            caseSummaryText.text = databaseManager.CaseMetaData.caseBackground?.briefBackground ?? string.Empty;
        }
    }
}
