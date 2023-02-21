using UnityEngine;

namespace Chess.Core.AI
{
    [CreateAssetMenu (menuName = "AI/Settings")]
    public class AISettings : ScriptableObject {

        public event System.Action requestAbortSearch;

        public int depth;
        public bool useIterativeDeepening;
        public bool useTranspositionTable;

        public bool useThreading;
        public bool useFixedDepthSearch;
        public int searchTimeMillis = 1000;
        public bool endlessSearchMode;
        public bool clearTTEachMove;
        public EvaluationType evaluationType;

        public bool useBook;
        public TextAsset book;
        public int maxBookPly = 10;

        public Search.SearchDiagnostics diagnostics;

        public void RequestAbortSearch () {
            requestAbortSearch?.Invoke ();
        }

        public enum EvaluationType
        {
            MATERIAL_ONLY,
            PST_WITH_ENDGAME_WEIGHTS
        }
    }
}