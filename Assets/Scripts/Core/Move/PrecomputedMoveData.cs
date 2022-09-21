using System;
using System.Collections.Generic;

namespace Chess.Core
{
    public static class PrecomputedMoveData
    {
        /// <summary>
        /// Square index offset in given direction      { N,  S,  W,  E, NW, SE, NE, SW }
        /// </summary>
        public static readonly int[] directionOffsets = { 8, -8, -1,  1,  7, -7,  9, -9 };

        /// <summary>
        /// Stores number of moves available in each of the 8 directions for every square on the board
        /// Order of directions: N, S, W, E, NW, SE, NE, SW
        /// First index is the direction, second index is the square index
        /// </summary>
        public static readonly int[][] numberOfSquaresToEdge;

        /// <summary>
        /// Stores an array of indices for each square a knight can land on from any square on the board
        /// Example:
        /// If knightMoves[0] is equal to {10, 17}, it means a knight on 'a1' can jump to 'c2' and 'b3'
        /// </summary>
        public static readonly byte[][] knightMoves;
        public static readonly byte[][] kingMoves;

        /// <summary>
        /// Stores pawn attack direction indices for white and black {{ NW, NE }; { SW, SE }}
        /// First index is the colour index, second index is the direction index
        /// </summary>
        public static readonly byte[][] pawnAttackDirections =
        {
            new byte[] { 4, 6 },
            new byte[] { 7, 5 }
        };

        public static readonly int[][] whitePawnAttacks;
        public static readonly int[][] blackPawnAttacks;
        public static readonly int[] directionLookup;

        public static readonly ulong[] kingAttackBitboards;
        public static readonly ulong[] knightAttackBitboards;
        public static readonly ulong[][] pawnAttackBitboards;

        public static readonly ulong[] rookMoves;
        public static readonly ulong[] bishopMoves;
        public static readonly ulong[] queenMoves;
        
        /// <summary>
        /// Manhattan distance: how many moves does it take for a rook to get from one square to another
        /// </summary>
        public static int[,] orthogonalDistance;
        /// <summary>
        /// Chebyshev distance: how many moves does it take a king to get from one square to another
        /// </summary>
        public static int[,] kingDistance;
        public static int[] centreManhattanDistance;

        static PrecomputedMoveData()
        {
            whitePawnAttacks = new int[64][];
            blackPawnAttacks = new int[64][];
            knightMoves = new byte[64][];
            kingMoves = new byte[64][];
            numberOfSquaresToEdge = new int[64][];

            rookMoves = new ulong[64];
            bishopMoves = new ulong[64];
            queenMoves = new ulong[64];
            
            // Square index offset in given direction for knight jumps
            int[] knightJumps = { 15, 17, -17, -15, 10, -6, 6, -10 };
            knightAttackBitboards = new ulong[64];
            kingAttackBitboards = new ulong[64];
            pawnAttackBitboards = new ulong[64][];

            for (var squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                var y = squareIndex / 8;
                var x = squareIndex - y * 8;

                var north = 7 - y;
                var south = y;
                var west = x;
                var east = 7 - x;
                numberOfSquaresToEdge[squareIndex] = new int[8];
                numberOfSquaresToEdge[squareIndex][0] = north;
                numberOfSquaresToEdge[squareIndex][1] = south;
                numberOfSquaresToEdge[squareIndex][2] = west;
                numberOfSquaresToEdge[squareIndex][3] = east;
                numberOfSquaresToEdge[squareIndex][4] = Math.Min(north, west);
                numberOfSquaresToEdge[squareIndex][5] = Math.Min(south, east);
                numberOfSquaresToEdge[squareIndex][6] = Math.Min(north, east);
                numberOfSquaresToEdge[squareIndex][7] = Math.Min(south, west);

                // Calculate knight jumps
                var legalKnightJumps = new List<byte>();
                ulong knightBitboard = 0;
                foreach (var knightJumpDelta in knightJumps)
                {
                    var knightJumpSquare = squareIndex + knightJumpDelta;
                    if (knightJumpSquare is < 0 or >= 64) continue;
                    
                    var knightSquareY = knightJumpSquare / 8;
                    var knightSquareX = knightJumpSquare - knightSquareY * 8;
                    // Reject indices that wrap around the side of the board
                    var maxCoordMoveDistance = Math.Max(Math.Abs(x - knightSquareX), Math.Abs(y - knightSquareY));
                    if (maxCoordMoveDistance != 2) continue;
                    
                    legalKnightJumps.Add((byte)knightJumpSquare);
                    knightBitboard |= 1ul << knightJumpSquare;
                }
                knightMoves[squareIndex] = legalKnightJumps.ToArray();
                knightAttackBitboards[squareIndex] = knightBitboard;
                
                // Calculate king moves (not including castling)
                var legalKingMoves = new List<byte> ();
                ulong kingBitboard = 0;
                foreach (var kingMoveDelta in directionOffsets) {
                    var kingMoveSquare = squareIndex + kingMoveDelta;
                    if (kingMoveSquare is < 0 or >= 64) continue;
                    
                    var kingSquareY = kingMoveSquare / 8;
                    var kingSquareX = kingMoveSquare - kingSquareY * 8;
                    // Reject indices that wrap around the side of the board
                    var maxCoordMoveDst = Math.Max(Math.Abs(x - kingSquareX), Math.Abs(y - kingSquareY));
                    if (maxCoordMoveDst != 1) continue;
                    
                    legalKingMoves.Add((byte)kingMoveSquare);
                    kingBitboard |= 1ul << kingMoveSquare;
                }
                kingMoves[squareIndex] = legalKingMoves.ToArray ();
                kingAttackBitboards[squareIndex] = kingBitboard;
                
                // Calculate pawn captures
                var whitePawnCaptures = new List<int>();
                var blackPawnCaptures = new List<int>();
                pawnAttackBitboards[squareIndex] = new ulong[2];
                if (x > 0)
                {
                    if (y < 7)
                    {
                        whitePawnCaptures.Add(squareIndex + 7);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 7);
                    }
                    if (y > 0)
                    {
                        blackPawnCaptures.Add(squareIndex - 9);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 9);
                    }
                }

                if (x < 7)
                {
                    if (y < 7)
                    {
                        whitePawnCaptures.Add(squareIndex + 9);
                        pawnAttackBitboards[squareIndex][Board.WhiteIndex] |= 1ul << (squareIndex + 9);
                    }
                    if (y > 0)
                    {
                        blackPawnCaptures.Add(squareIndex - 7);
                        pawnAttackBitboards[squareIndex][Board.BlackIndex] |= 1ul << (squareIndex - 7);
                    }
                }
                whitePawnAttacks[squareIndex] = whitePawnCaptures.ToArray();
                blackPawnAttacks[squareIndex] = blackPawnCaptures.ToArray();
                
                // Calculate rook moves
                for (var directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    var currentDirectionOffset = directionOffsets[directionIndex];
                    for (var n = 0; n < numberOfSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        var targetSquare = squareIndex + currentDirectionOffset * (n + 1);
                        rookMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }
                // Calculate bishop moves
                for (var directionIndex = 4; directionIndex < 8; directionIndex++)
                {
                    var currentDirectionOffset = directionOffsets[directionIndex];
                    for (var n = 0; n < numberOfSquaresToEdge[squareIndex][directionIndex]; n++)
                    {
                        var targetSquare = squareIndex + currentDirectionOffset * (n + 1);
                        bishopMoves[squareIndex] |= 1ul << targetSquare;
                    }
                }
                // Queens can move like a rook and like a bishop, so we can just combine those two bitmaps
                queenMoves[squareIndex] = rookMoves[squareIndex] | bishopMoves[squareIndex];
            }

            // Direction lookup
            directionLookup = new int[127];
            for (var i = 0; i < 127; i++)
            {
                var offset = i - 63;
                var absOffset = Math.Abs(offset);
                var absDir = 1;
                if (absOffset % 9 == 0) {
                    absDir = 9;
                } else if (absOffset % 8 == 0) {
                    absDir = 8;
                } else if (absOffset % 7 == 0) {
                    absDir = 7;
                }

                directionLookup[i] = absDir * Math.Sign(offset);
            }
            
            // Distance lookup
            orthogonalDistance = new int[64, 64];
            kingDistance = new int[64, 64];
            centreManhattanDistance = new int[64];
            for (var startSquare = 0; startSquare < 64; startSquare++)
            {
                var startCoord = BoardRepresentation.CoordFromIndex(startSquare);
                var fileDstFromCentre = Math.Max(3 - startCoord.fileIndex, startCoord.fileIndex - 4);
                var rankDstFromCentre = Math.Max(3 - startCoord.rankIndex, startCoord.rankIndex - 4);
                centreManhattanDistance[startSquare] = fileDstFromCentre + rankDstFromCentre;

                for (var targetSquare = 0; targetSquare < 64; targetSquare++)
                {
                    var targetCoord = BoardRepresentation.CoordFromIndex(targetSquare);
                    var fileDistance = Math.Abs(startCoord.fileIndex - targetCoord.fileIndex);
                    var rankDistance = Math.Abs(startCoord.rankIndex - targetCoord.rankIndex);
                    orthogonalDistance[startSquare, targetSquare] = fileDistance + rankDistance;
                    kingDistance[startSquare, targetSquare] = Math.Max(fileDistance, rankDistance);
                }
            }
        }

        public static int NumRookMovesToReachSquare(int startSquare, int targetSquare)
        {
            return orthogonalDistance[startSquare, targetSquare];
        }

        public static int NumKingMovesToReachSquare(int startSquare, int targetSquare)
        {
            return kingDistance[startSquare, targetSquare];
        }
    }
}