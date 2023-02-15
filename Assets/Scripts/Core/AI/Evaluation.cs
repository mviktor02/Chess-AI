﻿using System;

namespace Chess.Core.AI
{
    public class Evaluation
    {

        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 320;
        public const int rookValue = 500;
        public const int queenValue = 900;

        public static int Evaluate(in Board board, AISettings.EvaluationType evaluationType)
        {
            return evaluationType switch
            {
                AISettings.EvaluationType.MATERIAL_ONLY => EvaluateMaterialOnly(board),
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

            int whiteMaterialScore = CalculateMaterialScore(board, Board.WhiteIndex);
            int blackMaterialScore = CalculateMaterialScore(board, Board.BlackIndex);

            whiteEval += whiteMaterialScore;
            blackEval += blackMaterialScore;

            int eval = whiteEval - blackEval;

            return board.isWhitesTurn ? eval : -eval;
        }

        private static int CalculateMaterialScore(in Board board, int colourIndex)
        {
            return board.pawns[colourIndex].Count * pawnValue
                   + board.knights[colourIndex].Count * knightValue
                   + board.bishops[colourIndex].Count * bishopValue
                   + board.rooks[colourIndex].Count * rookValue
                   + board.queens[colourIndex].Count * queenValue;
        }
        
    }
}