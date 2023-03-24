using System;
using System.Collections.Generic;

namespace Chess.Core.AI
{
    public class Evaluation
    {

        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 320;
        public const int rookValue = 500;
        public const int queenValue = 900;
        public const int kingValue = 1000;
        
        const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;

        public static int Evaluate(in Board board, AISettings.EvaluationType evaluationType, List<Move> moves = null)
        {
            return evaluationType switch
            {
	            AISettings.EvaluationType.MATERIAL_ONLY => EvaluateMaterialOnly(board),
	            AISettings.EvaluationType.PST_WITH_ENDGAME_WEIGHTS => EvaluateWithPSTandEndgameWeight(board),
	            AISettings.EvaluationType.MOBILITY_AND_THREATS => EvaluateWithMobilityAndThreats(board, moves),
                _ => throw new ArgumentOutOfRangeException(nameof(evaluationType), evaluationType, null)
            };
        }

        /// <summary>
        /// Evaluates the current board position, only looking at material score.
        /// </summary>
        /// <returns>The current material score from the perspective of the current player's colour</returns>
        public static int EvaluateMaterialOnly(in Board board)
        {
            int whiteEval = 0;
            int blackEval = 0;

            int whiteMaterialScore = CalculateMaterial(board, Board.WhiteIndex);
            int blackMaterialScore = CalculateMaterial(board, Board.BlackIndex);

            whiteEval += whiteMaterialScore;
            blackEval += blackMaterialScore;

            int eval = whiteEval - blackEval;
            return board.isWhitesTurn ? eval : -eval;
        }

        private static int CalculateMaterial(in Board board, int colourIndex)
        {
            return board.pawns[colourIndex].Count * pawnValue
                   + board.knights[colourIndex].Count * knightValue
                   + board.bishops[colourIndex].Count * bishopValue
                   + board.rooks[colourIndex].Count * rookValue
                   + board.queens[colourIndex].Count * queenValue;
        }
        
        // Performs static evaluation of the current position.
		// The position is assumed to be 'quiet', i.e no captures are available that could drastically affect the evaluation.
		// The score that's returned is given from the perspective of whoever's turn it is to move.
		// So a positive score means the player who's turn it is to move has an advantage, while a negative score indicates a disadvantage.
		public static int EvaluateWithPSTandEndgameWeight(in Board board) {
			int whiteEval = 0;
			int blackEval = 0;

			int whiteMaterial = CalculateMaterial(board, Board.WhiteIndex);
			int blackMaterial = CalculateMaterial(board, Board.BlackIndex);

			int whiteMaterialWithoutPawns = whiteMaterial - board.pawns[Board.WhiteIndex].Count * pawnValue;
			int blackMaterialWithoutPawns = blackMaterial - board.pawns[Board.BlackIndex].Count * pawnValue;
			float whiteEndgamePhaseWeight = EndgamePhaseWeight(whiteMaterialWithoutPawns);
			float blackEndgamePhaseWeight = EndgamePhaseWeight(blackMaterialWithoutPawns);

			whiteEval += whiteMaterial;
			blackEval += blackMaterial;
			whiteEval += MopUpEval(board, Board.WhiteIndex, Board.BlackIndex, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
			blackEval += MopUpEval(board, Board.BlackIndex, Board.WhiteIndex, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

			whiteEval += EvaluatePieceSquareTables(board, Board.WhiteIndex, blackEndgamePhaseWeight);
			blackEval += EvaluatePieceSquareTables(board, Board.BlackIndex, whiteEndgamePhaseWeight);

			int eval = whiteEval - blackEval;
			return board.isWhitesTurn ? eval : -eval;
		}

		private static float EndgamePhaseWeight (int materialCountWithoutPawns) {
			const float multiplier = 1 / endgameMaterialStart;
			return 1 - Math.Min(1, materialCountWithoutPawns * multiplier);
		}

		private static int MopUpEval(in Board board, int friendlyIndex, int opponentIndex, int myMaterial, int opponentMaterial, float endgameWeight) {
			int mopUpScore = 0;
			if (myMaterial > opponentMaterial + pawnValue * 2 && endgameWeight > 0) {

				int friendlyKingSquare = board.kingSquares[friendlyIndex];
				int opponentKingSquare = board.kingSquares[opponentIndex];
				mopUpScore += PrecomputedMoveData.centreManhattanDistance[opponentKingSquare] * 10;
				// use ortho dst to promote direct opposition
				mopUpScore += (14 - PrecomputedMoveData.NumRookMovesToReachSquare (friendlyKingSquare, opponentKingSquare)) * 4;

				return (int) (mopUpScore * endgameWeight);
			}
			return 0;
		}

		private static int EvaluatePieceSquareTables(in Board board, int colourIndex, float endgamePhaseWeight) {
			int value = 0;
			bool isWhite = colourIndex == Board.WhiteIndex;
			value += EvaluatePieceSquareTable(PieceSquareTable.pawns, board.pawns[colourIndex], isWhite);
			value += EvaluatePieceSquareTable(PieceSquareTable.rooks, board.rooks[colourIndex], isWhite);
			value += EvaluatePieceSquareTable(PieceSquareTable.knights, board.knights[colourIndex], isWhite);
			value += EvaluatePieceSquareTable(PieceSquareTable.bishops, board.bishops[colourIndex], isWhite);
			value += EvaluatePieceSquareTable(PieceSquareTable.queens, board.queens[colourIndex], isWhite);
			int kingEarlyPhase = PieceSquareTable.Read(PieceSquareTable.kingMiddle, board.kingSquares[colourIndex], isWhite);
			value += (int) (kingEarlyPhase * (1 - endgamePhaseWeight));
			//value += PieceSquareTable.Read (PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);

			return value;
		}

		private static int EvaluatePieceSquareTable (int[] table, PieceList pieceList, bool isWhite) {
			int value = 0;
			for (int i = 0; i < pieceList.Count; i++) {
				value += PieceSquareTable.Read(table, pieceList[i], isWhite);
			}
			return value;
		}

		private static int EvaluateWithMobilityAndThreats(in Board board, in List<Move> moves)
		{
			int whiteEval = 0;
			int blackEval = 0;

			int whiteMaterial = CalculateMaterial(board, Board.WhiteIndex);
			int blackMaterial = CalculateMaterial(board, Board.BlackIndex);

			int whiteMaterialWithoutPawns = whiteMaterial - board.pawns[Board.WhiteIndex].Count * pawnValue;
			int blackMaterialWithoutPawns = blackMaterial - board.pawns[Board.BlackIndex].Count * pawnValue;
			float whiteEndgamePhaseWeight = EndgamePhaseWeight(whiteMaterialWithoutPawns);
			float blackEndgamePhaseWeight = EndgamePhaseWeight(blackMaterialWithoutPawns);

			whiteEval += whiteMaterial;
			blackEval += blackMaterial;
			whiteEval += MopUpEval(board, Board.WhiteIndex, Board.BlackIndex, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
			blackEval += MopUpEval(board, Board.BlackIndex, Board.WhiteIndex, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

			whiteEval += EvaluatePieceSquareTables(board, Board.WhiteIndex, blackEndgamePhaseWeight);
			blackEval += EvaluatePieceSquareTables(board, Board.BlackIndex, whiteEndgamePhaseWeight);

			whiteEval += (int) (CalculateMobility(board, moves, Board.WhiteIndex) * (1 - whiteEndgamePhaseWeight));
			blackEval += (int) (CalculateMobility(board, moves, Board.BlackIndex) * (1 - blackEndgamePhaseWeight));

			whiteEval += (int) (CalculateThreats(board, moves, Board.WhiteIndex) * (1 - whiteEndgamePhaseWeight));
			blackEval += (int) (CalculateThreats(board, moves, Board.BlackIndex) * (1 - blackEndgamePhaseWeight));

			int eval = whiteEval - blackEval;
			return board.isWhitesTurn ? eval : -eval;
		}

		private static int CalculateMobility(in Board board, in List<Move> moves, int colourIndex)
		{
			int mobility = 0;
			foreach (var move in moves)
			{
				int piece = board.squares[move.StartSquare];
				if (Piece.GetColour(piece) == colourIndex)
				{
					mobility += (int) (GetPieceValue(piece) * 0.5f);
				}
			}

			return mobility;
		}

		// Try to threaten opponent's high value pieces
		private static int CalculateThreats(in Board board, in List<Move> moves, int colourIndex)
		{
			int threats = 0;
			foreach (var move in moves)
			{
				int movingPiece = board.squares[move.StartSquare];
				int targetPiece = board.squares[move.TargetSquare];
				if (Piece.GetColour(movingPiece) == colourIndex && Piece.GetColour(targetPiece) != colourIndex)
				{
					threats += (int) (GetPieceValue(targetPiece) * 0.5f);
				}
			}

			return threats;
		}
		
		private static int GetPieceValue(int pieceType)
		{
			return pieceType switch
			{
				Piece.Queen => queenValue,
				Piece.Rook => rookValue,
				Piece.Bishop => bishopValue,
				Piece.Knight => knightValue,
				Piece.Pawn => pawnValue,
				_ => 0
			};
		}
        
    }
}