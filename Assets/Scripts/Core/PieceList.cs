using System;
using UnityEngine;

namespace Chess.Core
{
    /// <summary>
    /// List of pieces of given type and colour currently on the board
    /// </summary>
    public class PieceList
    {
        public int[] occupiedSquares;
        private int[] map; // map[square] returns index of square in occupiedSquares
        private int numPieces;

        public PieceList(int maxPieceCount = 16)
        {
            occupiedSquares = new int[maxPieceCount];
            map = new int[64];
            numPieces = 0;
        }

        public int Count => numPieces;

        public void AddPieceAtSquare(int square)
        {
            occupiedSquares[numPieces] = square;
            map[square] = numPieces;
            numPieces++;
        }

        public void RemovePieceFromSquare(int square)
        {
            var pieceIndex = map[square];
            occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1];
            map[occupiedSquares[pieceIndex]] = pieceIndex;
            numPieces--;
        }

        public void MovePiece(int startSquare, int targetSquare)
        {
            var pieceIndex = map[startSquare];
            occupiedSquares[pieceIndex] = targetSquare;
            map[targetSquare] = pieceIndex;
        }

        public int this[int index] => occupiedSquares[index];
    }
}