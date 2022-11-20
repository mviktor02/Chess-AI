using System;
using System.Linq;

namespace Chess.Core.AI
{
    /*
     * TODO
     * Move Orderer
     * AI Player
     * Iterative Deepening
     * Quiescence Search
     * Transposition Table
     * Debug Info
     */
    /// <summary>
    /// The Search class allows us to look for the next best move from the current board position.
    /// We use a min-max search with alpha-beta pruning and null move heuristic, with added on settings like
    /// Iterative Deepening, Quiescence Searching and the usage of a Transposition Table.
    ///
    /// https://www.cs.cornell.edu/boom/2004sp/ProjectArch/Chess/algorithms.html
    /// </summary>
    public class Search
    {
        private const int immediateMateScore = 100000;
        
        public event Action<Move> onSearchComplete;

        private bool abortSearch;
        
        private int currentSearchDepth, maxSearchDepth;
        private int bestEvalThisIteration, bestEval;
        private Move bestMoveThisIteration, bestMove;
        
        private Board board;
        private MoveGenerator moveGenerator;
        private MoveOrderer moveOrderer;

        private Func<Board, int> evaluate;

        public Search(ref Board searchBoard, Func<Board, int> evaluationFunc, int maxSearchDepth = 8)
        {
            board = searchBoard;
            evaluate = evaluationFunc;
            this.maxSearchDepth = maxSearchDepth;
            
            moveGenerator = new MoveGenerator();
        }

        public void StartSearch()
        {
            bestEvalThisIteration = bestEval = 0;
            bestMoveThisIteration = bestMove = Move.InvalidMove;

            currentSearchDepth = 0;

            SearchMoves(maxSearchDepth, 0);
            bestMove = bestMoveThisIteration;
            bestEval = bestEvalThisIteration;
            
            onSearchComplete?.Invoke(bestMove);
        }

        public void Abort()
        {
            abortSearch = true;
        }

        private int SearchMoves(int depth, int plyFromRoot, int alpha = int.MinValue, int beta = int.MaxValue)
        {
            if (abortSearch)
                return 0;

            if (plyFromRoot > 0)
            {
                // If the same position is repeated 3 times, the game is automatically a draw, so return the draw score
                if (board.repetitionPosHistory.Count(x => x == board.zobristKey) == 3)
                    return 0;

                // Skip this position if a mating sequence has already been found earlier in the search, which would be
                // shorter than any mate we could find from here. This means that alpha can't be worse (therefore beta
                // can't be better) than being mated in the current position...
                alpha = Math.Max(alpha, -immediateMateScore + plyFromRoot);
                beta = Math.Min(beta, immediateMateScore - plyFromRoot);
                if (alpha >= beta)
                    return alpha;
            }

            var moves = moveGenerator.GenerateMoves(board);
            moveOrderer.OrderMoves(board, moves);
            if (moves.Count == 0)
            {
                if (moveGenerator.IsInCheck())
                    return -immediateMateScore + plyFromRoot;
                return 0;
            }

            var bestMoveInThisPosition = Move.InvalidMove;

            foreach (var move in moves)
            {
                board.MakeMove(move);
                var eval = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
                board.UnmakeMove(move);

                // If a move is considered too good, the opponent probably won't allow us to reach this position by
                // choosing a different move earlier on, so we should skip the remaining moves...
                if (eval >= beta)
                    return beta;
                
                // Found a new best move in this position
                if (eval > alpha)
                {
                    bestMoveInThisPosition = move;

                    alpha = eval;
                    if (plyFromRoot == 0)
                    {
                        bestMoveThisIteration = move;
                        bestEvalThisIteration = eval;
                    }
                }
            }

            return alpha;
        }
        
    }
}