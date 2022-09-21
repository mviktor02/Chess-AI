using System.Collections.Generic;
using static Chess.Core.BoardRepresentation;
using static Chess.Core.PrecomputedMoveData;

namespace Chess.Core
{
    public class MoveGenerator
    {
        private List<Move> moves;
        private int friendlyColour;
        private int opponentColour;
        private int friendlyKingSquare;
        private int friendlyColourIndex;
        private int opponentColourIndex;

        private bool isInCheck;
        private bool isInDoubleCheck;
        private bool doPinsExist;
        private ulong checkRayBitmask;
        private ulong pinRayBitmask;
        private ulong opponentKnightAttacks;
        private ulong opponentAttackMapNoPawns;
        private ulong opponentAttackMap;
        private ulong opponentPawnAttackMap;
        private ulong opponentSlidingAttackMap;

        private bool generateQuietMoves;
        private Board board;

        private void Init()
        {
            moves = new List<Move>(64);
            isInCheck = false;
            isInDoubleCheck = false;
            doPinsExist = false;
            checkRayBitmask = 0;
            pinRayBitmask = 0;

            friendlyColour = board.colourToMove;
            opponentColour = board.opponentColour;
            friendlyColourIndex = board.colourToMoveIndex;
            opponentColourIndex = 1 - friendlyColourIndex;
            friendlyKingSquare = board.kingSquareIndexes[friendlyColourIndex];
        }

        /// <summary>
        /// This will only return the correct value after GenerateMoves() has been called in the current position
        /// </summary>
        public bool IsInCheck()
        {
            return isInCheck;
        }

        /// <summary>
        /// Generates a list of all legal moves for the current position
        /// </summary>
        /// <param name="generateQuiets">Non-capture moves are called Quiet moves. This is used in quiescence search</param>
        public List<Move> GenerateMoves(Board board, bool generateQuiets = true)
        {
            this.board = board;
            generateQuietMoves = generateQuiets;
            Init();
            
            CalculateAttackData();
            GenerateKingMoves();

            // Only king moves are valid in a double check position
            if (isInDoubleCheck)
                return moves;

            GenerateSlidingMoves();
            GenerateKnightMoves();
            GeneratePawnMoves();
            
            return moves;
        }

        private void GenerateKingMoves()
        {
            throw new System.NotImplementedException();
        }

        private void GenerateSlidingMoves()
        {
            throw new System.NotImplementedException();
        }

        private void GenerateKnightMoves()
        {
            throw new System.NotImplementedException();
        }

        private void GeneratePawnMoves()
        {
            throw new System.NotImplementedException();
        }

        private void CalculateAttackData()
        {
            throw new System.NotImplementedException();
        }
    }
}