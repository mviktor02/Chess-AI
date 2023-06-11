using System;
using System.Linq;
using UnityEngine;

namespace Chess.Core.AI
{
    /// <summary>
    /// The Search class allows us to look for the next best move from the current board position.
    /// We use a min-max search with alpha-beta pruning and null move heuristic, with added on settings like
    /// Iterative Deepening, Quiescence Searching and the usage of a Transposition Table.
    ///
    /// https://www.cs.cornell.edu/boom/2004sp/ProjectArch/Chess/algorithms.html
    /// </summary>
    public class Search
    {
        const int transpositionTableSize = 64000;
        private const int immediateMateScore = 100000;
        const int positiveInfinity = 9999999;
        const int negativeInfinity = -positiveInfinity;
        
        public event Action<Move> onSearchComplete;

        private bool abortSearch;
        
        private int currentSearchDepth, maxSearchDepth;
        private int bestEvalThisIteration, bestEval;
        private Move bestMoveThisIteration, bestMove;
        
        private Board board;
        private MoveGenerator moveGenerator;
        private MoveOrderer moveOrderer;
        private TranspositionTable transpositionTable;
        AISettings settings;
        
        private int numNodes;
        private int numQNodes;
        private int numCutoffs;
        private int numTranspositions;
        public SearchDiagnostics searchDiagnostics;
        private System.Diagnostics.Stopwatch searchStopwatch;
        
        public Search(ref Board searchBoard, AISettings aiSettings, int maxSearchDepth = 8)
        {
            board = searchBoard;
            this.maxSearchDepth = maxSearchDepth;
            this.settings = aiSettings;

            transpositionTable = new TranspositionTable(board, transpositionTableSize);
            moveGenerator = new MoveGenerator();
            moveOrderer = new MoveOrderer(moveGenerator, transpositionTable);
        }

        public void StartSearch()
        {
            InitDebugInfo();
            bestEvalThisIteration = bestEval = 0;
            bestMoveThisIteration = bestMove = Move.InvalidMove;
            transpositionTable.enabled = settings.useTranspositionTable;
            
            // Clearing the transposition table before each search seems to help
            // This makes no sense to me, I presume there is a bug somewhere but haven't been able to track it down yet
            if (settings.clearTTEachMove) {
                transpositionTable.Clear();
            }
            
            currentSearchDepth = 0;

            abortSearch = false;
            searchDiagnostics = new SearchDiagnostics();
            
            // Iterative deepening. This means doing a full search with a depth of 1, then with a depth of 2, and so on.
            // This allows the search to be aborted at any time, while still yielding a useful result from the last search.
            if (settings.useIterativeDeepening)
            {
                int targetDepth = (settings.useFixedDepthSearch) ? settings.depth : int.MaxValue;
                const int fourthPawnValue = Evaluation.pawnValue / 4;
                int alpha = negativeInfinity;
                int beta = positiveInfinity;
                for (int searchDepth = 1; searchDepth <= targetDepth; searchDepth++) {
                    SearchMoves (searchDepth, 0, alpha, beta);
                    
                    if (abortSearch) {
                        break;
                    }

                    currentSearchDepth = searchDepth;
                    bestMove = bestMoveThisIteration;
                    bestEval = bestEvalThisIteration;

                    if (settings.useAspirationWindows)
                    {
                        alpha = bestEvalThisIteration - (bestEvalThisIteration < alpha ? 4 : 1) * fourthPawnValue;
                        beta = bestEvalThisIteration + (bestEvalThisIteration > beta ? 4 : 1) * fourthPawnValue;
                    }

                    // Update diagnostics
                    searchDiagnostics.lastCompletedDepth = searchDepth;
                    searchDiagnostics.move = bestMove.Name;
                    searchDiagnostics.eval = bestEval;
                    searchDiagnostics.moveVal = PGN.NotationFromMove(FenUtility.FenFromPosition(board), bestMove);

                    // Exit search if found a mate
                    if (IsMateScore (bestEval) && !settings.endlessSearchMode) {
                        break;
                    }
                }
            } else {
                SearchMoves (settings.depth, 0);
                bestMove = bestMoveThisIteration;
                bestEval = bestEvalThisIteration;
            }

            onSearchComplete?.Invoke(bestMove);
            
            if (!settings.useThreading) {
                LogDebugInfo();
            }
        }

        public void Abort()
        {
            abortSearch = true;
        }

        private int SearchMoves(int depth, int plyFromRoot, int alpha = negativeInfinity, int beta = positiveInfinity)
        {
            if (abortSearch)
                return 0;

            if (plyFromRoot > 0)
            {
                // If the same position is repeated 3 times, the game is automatically a draw, so return the draw score
                // We're returning at a count of 2 since 3 produced draws too often
                int repetitionCount = board.repetitionPosHistory.Count(x => x == board.zobristKey);
                if (repetitionCount == 2)
                {
                    return 0;
                }

                // Skip this position if a mating sequence has already been found earlier in the search, which would be
                // shorter than any mate we could find from here. This means that alpha can't be worse (therefore beta
                // can't be better) than being mated in the current position...
                alpha = Math.Max(alpha, -immediateMateScore + plyFromRoot);
                beta = Math.Min(beta, immediateMateScore - plyFromRoot);
                if (alpha >= beta)
                    return alpha;
            }
            
            // Try looking up the current position in the transposition table.
            // If the same position has already been searched to at least an equal depth
            // to the search we're doing now, we can just use the recorded evaluation.
            int ttVal = transpositionTable.LookupEvaluation(depth, plyFromRoot, alpha, beta);
            if (ttVal != TranspositionTable.lookupFailed) {
                numTranspositions++;
                if (plyFromRoot == 0) {
                    bestMoveThisIteration = transpositionTable.GetStoredMove();
                    bestEvalThisIteration = transpositionTable.entries[transpositionTable.Index].value;
                    //Debug.Log ("move retrieved " + bestMoveThisIteration.Name + " Node type: " + tt.entries[tt.Index].nodeType + " depth: " + tt.entries[tt.Index].depth);
                }
                return ttVal;
            }
            
            if (depth == 0) {
                int evaluation = QuiescenceSearch(alpha, beta);
                return evaluation;
            }

            var moves = moveGenerator.GenerateMoves(board);
            moveOrderer.OrderMoves(board, moves, settings.useTranspositionTable);
            if (moves.Count == 0)
            {
                if (moveGenerator.IsInCheck())
                    return -immediateMateScore + plyFromRoot;
                return 0;
            }

            var bestMoveInThisPosition = Move.InvalidMove;
            var evalType = TranspositionTable.UpperBound;

            foreach (var move in moves)
            {
                board.MakeMove(move, recordGameHistory : false);
                var eval = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
                board.UnmakeMove(move, recordGameHistory : false);
                numNodes++;

                // If a move is considered too good, the opponent probably won't allow us to reach this position by
                // choosing a different move earlier on, so we should skip the remaining moves...
                if (eval >= beta)
                {
                    transpositionTable.StoreEvaluation(depth, plyFromRoot, beta, TranspositionTable.LowerBound, move);
                    numCutoffs++;
                    return beta;
                }

                // Found a new best move in this position
                if (eval > alpha)
                {
                    evalType = TranspositionTable.Exact;
                    bestMoveInThisPosition = move;

                    alpha = eval;
                    if (plyFromRoot == 0)
                    {
                        bestMoveThisIteration = move;
                        bestEvalThisIteration = eval;
                    }
                }
            }
            
            transpositionTable.StoreEvaluation(depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

            return alpha;
        }
        
        // Search capture moves until a 'quiet' position is reached.
        int QuiescenceSearch(int alpha, int beta) {
            // Generate moves first so we can pass it to the evaluation function - needed for advanced eval
            var moves = moveGenerator.GenerateMoves (board, false);
            moveOrderer.OrderMoves(board, moves, false);
            
            // A player isn't forced to make a capture (typically), so see what the evaluation is without capturing anything.
            // This prevents situations where a player ony has bad captures available from being evaluated as bad,
            // when the player might have good non-capture moves available.
            int eval = Evaluation.Evaluate(board, settings.evaluationType, moves);
            searchDiagnostics.numPositionsEvaluated++;
            if (eval >= beta) {
                return beta;
            }
            if (eval > alpha) {
                alpha = eval;
            }

            foreach (var move in moves)
            {
                try
                {
                    board.MakeMove (move, false);
                }
                catch (IndexOutOfRangeException e)
                {
                    Debug.Log(move.StartSquare);
                    Debug.Log(FenUtility.FenFromPosition(board));
                    throw new Exception(e.Message);
                }
                eval = -QuiescenceSearch(-beta, -alpha);
                board.UnmakeMove (move, false);
                numQNodes++;

                if (eval >= beta) {
                    numCutoffs++;
                    return beta;
                }
                if (eval > alpha) {
                    alpha = eval;
                }
            }

            return alpha;
        }
        
        public static bool IsMateScore(int score) {
            const int maxMateDepth = 1000;
            return Math.Abs(score) > immediateMateScore - maxMateDepth;
        }

        public static int NumPlyToMateFromScore(int score) {
            return immediateMateScore - Math.Abs(score);
        }
        
        void AnnounceMate() {

            if (IsMateScore (bestEvalThisIteration)) {
                int numPlyToMate = NumPlyToMateFromScore (bestEvalThisIteration);
                //int numPlyToMateAfterThisMove = numPlyToMate - 1;

                int numMovesToMate = (int) Math.Ceiling(numPlyToMate / 2f);

                string sideWithMate = (bestEvalThisIteration * (board.isWhitesTurn ? 1 : -1) < 0) ? "Black" : "White";

                Debug.Log ($"{sideWithMate} can mate in {numMovesToMate} move{(numMovesToMate>1 ? "s" : "")}");
            }
        }
        
        void LogDebugInfo () {
            AnnounceMate ();
            Debug.Log ($"Best move: {bestMoveThisIteration.Name} Eval: {bestEvalThisIteration} Search time: {searchStopwatch.ElapsedMilliseconds} ms.");
            Debug.Log ($"Num nodes: {numNodes} num Qnodes: {numQNodes} num cutoffs: {numCutoffs} num TThits {numTranspositions}");
        }
        
        void InitDebugInfo() {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            numNodes = 0;
            numQNodes = 0;
            numCutoffs = 0;
            numTranspositions = 0;
        }
        
        [Serializable]
        public class SearchDiagnostics {
            public int lastCompletedDepth;
            public string moveVal;
            public string move;
            public int eval;
            public bool isBook;
            public int numPositionsEvaluated;
        }
        
    }
}