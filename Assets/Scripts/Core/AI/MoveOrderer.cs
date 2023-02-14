using System.Collections.Generic;

namespace Chess.Core.AI
{
    /// <summary>
    /// This class is used to order moves by the value they have on the game
    /// </summary>
    public class MoveOrderer
    {
        private int[] moveScores;
        private const int maxMoveCount = 218;

        private const int squareControlledByOpponentPawnPenalty = 350;
        private const int capturedPieceValueMultiplier = 10;

        private MoveGenerator moveGenerator;
        private TranspositionTable transpositionTable;
        private Move invalidMove;
        
        public MoveOrderer(MoveGenerator moveGenerator, TranspositionTable transpositionTable) {
            moveScores = new int[maxMoveCount];
            this.moveGenerator = moveGenerator;
            this.transpositionTable = transpositionTable;
            invalidMove = Move.InvalidMove;
        }
        
        public void OrderMoves(Board board, List<Move> moves, bool useTT)
        {
            Move hashMove = invalidMove;
            if (useTT) {
                hashMove = transpositionTable.GetStoredMove ();
            }

            for (int i = 0; i < moves.Count; i++) {
                int score = 0;
                int movePieceType = Piece.GetPieceType(board.squares[moves[i].StartSquare]);
                int capturePieceType = Piece.GetPieceType(board.squares[moves[i].TargetSquare]);
                int flag = moves[i].MoveFlag;

                if (capturePieceType != Piece.None) {
                    // Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
                    // The capturedPieceValueMultiplier is used to make even 'bad' captures like QxP rank above non-captures
                    score = capturedPieceValueMultiplier * GetPieceValue(capturePieceType) - GetPieceValue(movePieceType);
                }

                if (movePieceType == Piece.Pawn) switch (flag)
                {
                    case Move.Flag.PromoteToQueen:
                        score += Evaluation.queenValue;
                        break;
                    case Move.Flag.PromoteToKnight:
                        score += Evaluation.knightValue;
                        break;
                    case Move.Flag.PromoteToRook:
                        score += Evaluation.rookValue;
                        break;
                    case Move.Flag.PromoteToBishop:
                        score += Evaluation.bishopValue;
                        break;
                } else {
                    // Penalize moving piece to a square attacked by opponent pawn
                    if (BitboardUtil.ContainsSquare(moveGenerator.opponentPawnAttackMap, moves[i].TargetSquare)) {
                        score -= squareControlledByOpponentPawnPenalty;
                    }
                }
                if (Move.IsSameMove(moves[i], hashMove)) {
                    score += 10000;
                }

                moveScores[i] = score;
            }

            Sort (moves);
        }
        
        static int GetPieceValue (int pieceType)
        {
            return pieceType switch
            {
                Piece.Queen => Evaluation.queenValue,
                Piece.Rook => Evaluation.rookValue,
                Piece.Knight => Evaluation.knightValue,
                Piece.Bishop => Evaluation.bishopValue,
                Piece.Pawn => Evaluation.pawnValue,
                _ => 0
            };
        }

        void Sort (List<Move> moves) {
            // Sort the moves list based on scores
            for (int i = 0; i < moves.Count - 1; i++) {
                for (int j = i + 1; j > 0; j--) {
                    int swapIndex = j - 1;
                    if (moveScores[swapIndex] < moveScores[j]) {
                        (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                        (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                    }
                }
            }
        }
    }
}