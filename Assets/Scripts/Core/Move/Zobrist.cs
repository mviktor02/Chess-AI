using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

namespace Chess.Core
{
    public static class Zobrist
    {
        private const int seed = 2361912;
        private const string randomNumbersFileName = "RandomNumbers.txt";

        /// Indices: Piece Type, Colour, Square Index
        public static readonly ulong[,,] pieces = new ulong[8, 2, 64];
        public static readonly ulong[] castlingRights = new ulong[16];
        // no need for rank info as side to move is included in key
        public static readonly ulong[] enPassantFile = new ulong[9];
        public static readonly ulong sideToMove;

        private static Random rnd = new Random(seed);

        private static void WriteRandomNumbers()
        {
            rnd = new Random(seed);
            var randomNumberStringBuilder = new StringBuilder();
            var numberOfRandomNumbers = 64 * 8 * 2 + castlingRights.Length + 9 + 1;
            
            for (var i = 0; i < numberOfRandomNumbers; i++)
            {
                randomNumberStringBuilder.Append(RandomUnsigned64BitNumber());
                if (i != numberOfRandomNumbers - 1)
                {
                    randomNumberStringBuilder.Append(',');
                }
            }

            var writer = new StreamWriter(RandomNumbersFilePath);
            writer.Write(randomNumberStringBuilder.ToString());
            writer.Close();
        }

        private static Queue<ulong> ReadRandomNumbers()
        {
            if (!File.Exists(RandomNumbersFilePath))
            {
                Debug.Log("Writing random numbers file");
                WriteRandomNumbers();
            }
            var randomNumbers = new Queue<ulong>();

            var reader = new StreamReader(RandomNumbersFilePath);
            var numbersString = reader.ReadToEnd();
            reader.Close();

            var numberStrings = numbersString.Split(",");
            foreach (var numberString in numberStrings)
            {
                var number = ulong.Parse(numberString);
                randomNumbers.Enqueue(number);
            }

            return randomNumbers;
        }

        static Zobrist()
        {
            var randomNumbers = ReadRandomNumbers();

            for (var squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                for (var pieceIndex = 0; pieceIndex < 8; pieceIndex++)
                {
                    pieces[pieceIndex, Board.WhiteIndex, squareIndex] = randomNumbers.Dequeue();
                    pieces[pieceIndex, Board.BlackIndex, squareIndex] = randomNumbers.Dequeue();
                }
            }

            for (var i = 0; i < 16; i++)
            {
                castlingRights[i] = randomNumbers.Dequeue();
            }

            for (var i = 0; i < 9; i++)
            {
                enPassantFile[i] = randomNumbers.Dequeue();
            }

            sideToMove = randomNumbers.Dequeue();
        }

        /// This should only be used after setting the board from fen. Otherwise the key should be updated incrementally
        public static ulong CalculateZobristKey(Board board)
        {
            var zobristKey = 0ul;

            for (var squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                var piece = board.squares[squareIndex];
                if (piece == 0) continue;
                
                var pieceType = Piece.GetPieceType(piece);
                zobristKey ^= pieces[
                    pieceType,
                    Piece.IsColour(piece, Piece.White) ? Board.WhiteIndex : Board.BlackIndex, 
                    squareIndex
                ];
            }

            var enPassantIndex = (int)(board.currentGameState >> 4) & 15;
            if (enPassantIndex != -1)
            {
                zobristKey ^= enPassantFile[enPassantIndex];
            }

            if (board.colourToMove == Piece.Black)
            {
                zobristKey ^= sideToMove;
            }

            zobristKey ^= castlingRights[board.currentGameState & 0b1111];

            return zobristKey;
        }

        private static string RandomNumbersFilePath =>
            Path.Combine(Application.streamingAssetsPath, randomNumbersFileName);
        
        private static ulong RandomUnsigned64BitNumber () {
            var buffer = new byte[8];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}