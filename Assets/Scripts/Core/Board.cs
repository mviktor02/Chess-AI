using System.Collections.Generic;

namespace Chess.Core
{
    // TODO
    public class Board
    {
        public const int WhiteIndex = 0;
        public const int BlackIndex = 1;

        // storing pieces : colour | type
        public int[] squares;

        public bool isWhitesTurn;
        public int colourToMove;
        public int colourToMoveIndex;
        public int opponentColour;

        public int plyCounter;
        public int fiftyMoveCounter;

        /// <summary>
        /// Bits 0-3 store white and black king/queenside castling legality
        ///      4-7 store the file of the en passant square (1-8), 0 means there's no en passant square
        ///      8-13 store the last captured piece
        ///      14+ store the fifty move counter
        /// </summary>
        public uint currentGameState;
        private Stack<uint> gameStateHistory;
        
        public int[] kingSquareIndexes;

        public PieceList[] rooks;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] knights;
        public PieceList[] pawns;

        private PieceList[] pieceLists;

        private PieceList GetPieceList(int pieceType, int colourIndex)
        {
            return pieceLists[colourIndex * 8 + pieceType];
        }

        public void MakeMove(Move move)
        {
            
        }

        public void UnmakeMove(Move move)
        {
            
        }

        private void InitializeBoard()
        {
            squares = new int[64];
            kingSquareIndexes = new int[2];

            gameStateHistory = new Stack<uint>();
            plyCounter = 0;
            fiftyMoveCounter = 0;

            knights = new PieceList[] { new(10), new(10) };
            pawns = new PieceList[] { new(8), new(8) };
            rooks = new PieceList[] { new(10), new(10) };
            bishops = new PieceList[] { new(10), new(10) };
            queens = new PieceList[] { new(9), new(9) };
            var emptyList = new PieceList(0);
            pieceLists = new[] {
                emptyList,
                emptyList,
                pawns[WhiteIndex],
                knights[WhiteIndex],
                emptyList,
                bishops[WhiteIndex],
                rooks[WhiteIndex],
                queens[WhiteIndex],
                emptyList,
                emptyList,
                pawns[BlackIndex],
                knights[BlackIndex],
                emptyList,
                bishops[BlackIndex],
                rooks[BlackIndex],
                queens[BlackIndex],
            };
        }

        public void LoadStartPosition()
        {
            LoadPosition(FenUtility.StartFen);
        }

        public void LoadPosition(string fen)
        {
            InitializeBoard();
            var loadedPosition = FenUtility.PositionFromFen(fen);

            for (var index = 0; index < 64; index++)
            {
                var piece = loadedPosition.squares[index];

                if (piece == Piece.None) continue;
                
                var type = Piece.GetPieceType(piece);
                var colourIndex = Piece.IsColour(piece, Piece.White) ? WhiteIndex : BlackIndex;
                if (Piece.IsSlidingPiece(piece))
                {
                    switch (type)
                    {
                        case Piece.Queen: queens[colourIndex].AddPieceAtSquare(index); break;
                        case Piece.Rook: rooks[colourIndex].AddPieceAtSquare(index); break;
                        case Piece.Bishop: bishops[colourIndex].AddPieceAtSquare(index); break;
                    }
                }
                else switch (type)
                {
                    case Piece.Knight: knights[colourIndex].AddPieceAtSquare(index); break;
                    case Piece.Pawn: pawns[colourIndex].AddPieceAtSquare(index); break;
                    case Piece.King: kingSquareIndexes[colourIndex] = index; break;
                }
            }

            isWhitesTurn = loadedPosition.whiteToMove;
            colourToMove = isWhitesTurn ? Piece.White : Piece.Black;
            opponentColour = isWhitesTurn ? Piece.Black : Piece.White;
            colourToMoveIndex = isWhitesTurn ? WhiteIndex : BlackIndex;
            
            var whiteCastle = (loadedPosition.whiteCastleKingside  ? 1 << 0 : 0) | 
                                 (loadedPosition.whiteCastleQueenside ? 1 << 1 : 0);
            var blackCastle = (loadedPosition.blackCastleKingside  ? 1 << 2 : 0) | 
                                 (loadedPosition.blackCastleQueenside ? 1 << 3 : 0);
            var epState = loadedPosition.enPassantFile << 4;
            var initialGameState = (ushort) (whiteCastle | blackCastle | epState);
            gameStateHistory.Push(initialGameState);
            currentGameState = initialGameState;
            plyCounter = loadedPosition.plyCount;

            plyCounter = loadedPosition.plyCount;
        }

    }
}