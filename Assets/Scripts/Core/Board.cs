using System.Collections.Generic;

namespace Chess.Core
{
    // TODO UnmakeMove
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

        public int plyCount;
        public int fiftyMoveCounter;

        /// <summary>
        /// Bits 0-3 store white and black king/queenside castling legality
        ///      4-7 store the file of the en passant square (1-8), 0 means there's no en passant square
        ///      8-13 store the last captured piece
        ///      14+ store the fifty move counter
        /// </summary>
        public uint currentGameState;
        private Stack<uint> gameStateHistory;

        /// <summary>
        /// Zobrist Hashing transforms a board position into a number of a set length with an equal distribution over all possible numbers
        /// Zobrist hash codes are used in chess programming to get an almost unique index for any chess position
        /// These indices are used for faster and more space efficient hash tables like transposition tables and opening books
        /// </summary>
        public ulong zobristKey;
        public Stack<ulong> repetitionPosHistory;

        public int[] kingSquares;

        public PieceList[] rooks;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] knights;
        public PieceList[] pawns;

        private PieceList[] pieceLists;

        private const uint whiteCastleKingsideMask = 0b1111111111111110;
        private const uint whiteCastleQueensideMask = 0b1111111111111101;
        private const uint blackCastleKingsideMask = 0b1111111111111011;
        private const uint blackCastleQueensideMask = 0b1111111111110111;

        private const uint whiteCastleMask = whiteCastleKingsideMask & whiteCastleQueensideMask;
        private const uint blackCastleMask = blackCastleKingsideMask & blackCastleQueensideMask;

        private PieceList GetPieceList(int pieceType, int colourIndex)
        {
            return pieceLists[colourIndex * 8 + pieceType];
        }

        public void MakeMove(Move move, bool recordGameHistory = true)
        {
            var oldEnPassantFile = (currentGameState >> 4) & 15;
            var originalCastleState = currentGameState & 15;
            var newCastleState = originalCastleState;
            currentGameState = 0;

            var opponentColourIndex = 1 - colourToMoveIndex;
            var moveFrom = move.StartSquare;
            var moveTo = move.TargetSquare;

            var capturedPieceType = Piece.GetPieceType(squares[moveTo]);
            var pieceToMove = squares[moveFrom];
            var pieceToMoveType = Piece.GetPieceType(pieceToMove);

            var moveFlag = move.MoveFlag;
            var isPromotion = move.IsPromotion;
            var isEnPassant = moveFlag == Move.Flag.EnPassant;
            
            // Captures
            currentGameState |= (ushort)(capturedPieceType << 8);
            if (capturedPieceType != 0 && !isEnPassant)
            {
                zobristKey ^= Zobrist.pieces[capturedPieceType, opponentColourIndex, moveTo];
                GetPieceList(capturedPieceType, opponentColourIndex).RemovePieceFromSquare(moveTo);
            }

            // Move piece in its list
            if (pieceToMoveType == Piece.King)
            {
                kingSquares[colourToMoveIndex] = moveTo;
                newCastleState &= isWhitesTurn ? whiteCastleMask : blackCastleMask;
            }
            else
            {
                GetPieceList(pieceToMoveType, colourToMoveIndex).MovePiece(moveFrom, moveTo);
            }

            var pieceOnTargetSquare = pieceToMove;
            
            // Pawn Promotion
            if (isPromotion)
            {
                var promoteType = 0;
                switch (moveFlag)
                {
                    case Move.Flag.PromoteToQueen:
                        promoteType = Piece.Queen;
                        queens[colourToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToRook:
                        promoteType = Piece.Rook;
                        rooks[colourToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToBishop:
                        promoteType = Piece.Bishop;
                        bishops[colourToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToKnight:
                        promoteType = Piece.Knight;
                        knights[colourToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                }
                pieceOnTargetSquare = promoteType | colourToMove;
                pawns[colourToMoveIndex].RemovePieceFromSquare(moveTo);
            }
            // Other special moves (En Passant, Castling)
            else
            {
                switch (moveFlag)
                {
                    case Move.Flag.EnPassant:
                        var enPassantPawnSquare = moveTo + (colourToMove == Piece.White ? -8 : 8);
                        currentGameState |= (ushort)(squares[enPassantPawnSquare] << 8);
                        squares[enPassantPawnSquare] = 0;
                        pawns[opponentColourIndex].RemovePieceFromSquare(enPassantPawnSquare);
                        zobristKey ^= Zobrist.pieces[Piece.Pawn, opponentColourIndex, enPassantPawnSquare];
                        break;
                    case Move.Flag.Castling:
                        var isKingside = moveTo is BoardRepresentation.g1 or BoardRepresentation.g8;
                        var rookFromIndex = isKingside ? moveTo + 1 : moveTo - 2;
                        var rookToIndex = isKingside ? moveTo - 1 : moveTo + 1;

                        squares[rookFromIndex] = Piece.None;
                        squares[rookToIndex] = Piece.Rook | colourToMove;
                        
                        rooks[colourToMoveIndex].MovePiece(rookFromIndex, rookToIndex);
                        zobristKey ^= Zobrist.pieces[Piece.Rook, colourToMoveIndex, rookFromIndex];
                        zobristKey ^= Zobrist.pieces[Piece.Rook, colourToMoveIndex, rookToIndex];
                        break;
                }
            }
            
            // Update board representation
            squares[moveTo] = pieceOnTargetSquare;
            squares[moveFrom] = 0;
            
            // If a pawn has moved two forwards, mark file with En Passant flag
            if (moveFlag == Move.Flag.PawnTwoForward)
            {
                var file = BoardRepresentation.FileIndex(moveFrom) + 1;
                currentGameState |= (ushort)(file << 4);
                zobristKey ^= Zobrist.enPassantFile[file];
            }
            
            // A piece moving to or from the rook square removes castling right for that side
            if (originalCastleState != 0)
            {
                if (moveTo == BoardRepresentation.h1 || moveFrom == BoardRepresentation.h1)
                {
                    newCastleState &= whiteCastleKingsideMask;
                }
                else if (moveTo == BoardRepresentation.a1 || moveFrom == BoardRepresentation.a1)
                {
                    newCastleState &= whiteCastleQueensideMask;
                }
                
                if (moveTo == BoardRepresentation.h8 || moveFrom == BoardRepresentation.h8)
                {
                    newCastleState &= blackCastleKingsideMask;
                }
                else if (moveTo == BoardRepresentation.a8 || moveFrom == BoardRepresentation.a8)
                {
                    newCastleState &= blackCastleQueensideMask;
                }
            }
            
            // Update zobrist key with new piece positions and side to move
            zobristKey ^= Zobrist.sideToMove;
            zobristKey ^= Zobrist.pieces[pieceToMoveType, colourToMoveIndex, moveFrom];
            zobristKey ^= Zobrist.pieces[Piece.GetPieceType(pieceOnTargetSquare), colourToMoveIndex, moveTo];

            if (oldEnPassantFile != 0)
            {
                zobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];
            }

            if (newCastleState != originalCastleState)
            {
                zobristKey ^= Zobrist.castlingRights[originalCastleState];
                zobristKey ^= Zobrist.castlingRights[newCastleState];
            }

            currentGameState |= newCastleState;
            currentGameState |= (uint)fiftyMoveCounter << 14;
            gameStateHistory.Push(currentGameState);
            
            // Change side to move
            isWhitesTurn = !isWhitesTurn;
            colourToMove = isWhitesTurn ? Piece.White : Piece.Black;
            opponentColour = isWhitesTurn ? Piece.Black : Piece.White;
            colourToMoveIndex = 1 - colourToMoveIndex;
            plyCount++;
            fiftyMoveCounter++;

            if (recordGameHistory)
            {
                if (pieceToMoveType == Piece.Pawn || capturedPieceType != Piece.None)
                {
                    repetitionPosHistory.Clear();
                    fiftyMoveCounter = 0;
                }
                else
                {
                    repetitionPosHistory.Push(zobristKey);
                }
            }
        }

        public void UnmakeMove(Move move, bool recordGameHistory = true)
        {
            var opponentColourIndex = colourToMoveIndex;
            var isWhiteUndo = opponentColour == Piece.White;
            colourToMove = opponentColour;
            opponentColour = isWhiteUndo ? Piece.Black : Piece.White;
            colourToMoveIndex = 1 - colourToMoveIndex;
            isWhitesTurn = !isWhitesTurn;

            var originalCastleState = currentGameState & 0b1111;

            var capturedPieceType = ((int)currentGameState >> 8) & 63;
            var capturedPiece = capturedPieceType == 0 ? 0 : capturedPieceType | opponentColour;

            var movedFrom = move.StartSquare;
            var movedTo = move.TargetSquare;
            var moveFlag = move.MoveFlag;
            var isEnPassant = moveFlag == Move.Flag.EnPassant;
            var isPromotion = move.IsPromotion;

            var toSquarePieceType = Piece.GetPieceType(squares[movedTo]);
            var movedPieceType = isPromotion ? Piece.Pawn : toSquarePieceType;

            zobristKey ^= Zobrist.sideToMove;
            // add piece back to square it moved from
            zobristKey ^= Zobrist.pieces[movedPieceType, colourToMoveIndex, movedFrom];
            // remove piece from square it moved to
            zobristKey ^= Zobrist.pieces[toSquarePieceType, colourToMoveIndex, movedTo];

            var oldEnPassantFile = (currentGameState >> 4) & 15;
            if (oldEnPassantFile != 0)
                zobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

            // en passant captures are handled later
            if (capturedPieceType != 0 && !isEnPassant)
            {
                zobristKey ^= Zobrist.pieces[capturedPieceType, opponentColourIndex, movedTo];
                GetPieceList(capturedPieceType, opponentColourIndex).AddPieceAtSquare(movedTo);
            }

            if (movedPieceType == Piece.King)
            {
                kingSquares[colourToMoveIndex] = movedFrom;
            }
            else
            {
                GetPieceList(movedPieceType, colourToMoveIndex).MovePiece(movedTo, movedFrom);
            }

            // if the move was a promotion, this will put the promoted piece back to the original position
            // instead of the pawn...
            // this is handled in the special move checks below
            squares[movedFrom] = movedPieceType | colourToMove;
            squares[movedTo] = capturedPiece;

            if (isPromotion)
            {
                pawns[colourToMoveIndex].AddPieceAtSquare(movedFrom);
                switch (moveFlag)
                {
                    case Move.Flag.PromoteToQueen:
                        queens[colourToMoveIndex].RemovePieceFromSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToKnight:
                        knights[colourToMoveIndex].RemovePieceFromSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToRook:
                        rooks[colourToMoveIndex].RemovePieceFromSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToBishop:
                        bishops[colourToMoveIndex].RemovePieceFromSquare(movedTo);
                        break;
                }
            }
            // put captured pawn back on the right square
            else if (isEnPassant)
            {
                var enPassantIndex = movedTo + (colourToMove == Piece.White ? -8 : 8);
                squares[movedTo] = 0;
                squares[enPassantIndex] = capturedPiece;
                pawns[opponentColourIndex].AddPieceAtSquare(enPassantIndex);
                zobristKey ^= Zobrist.pieces[Piece.Pawn, opponentColourIndex, enPassantIndex];
            }
            // put castled rook back to starting square
            else if (moveFlag == Move.Flag.Castling)
            {
                var kingside = movedTo is BoardRepresentation.g1 or BoardRepresentation.g8;
                var castlingRookFromIndex = kingside ? movedTo + 1 : movedTo - 2;
                var castlingRookToIndex = kingside ? movedTo - 1 : movedTo + 1;

                squares[castlingRookToIndex] = 0;
                squares[castlingRookFromIndex] = Piece.Rook | colourToMove;
                
                rooks[colourToMoveIndex].MovePiece(castlingRookToIndex, castlingRookFromIndex);
                zobristKey ^= Zobrist.pieces[Piece.Rook, colourToMoveIndex, castlingRookFromIndex];
                zobristKey ^= Zobrist.pieces[Piece.Rook, colourToMoveIndex, castlingRookToIndex];
            }

            gameStateHistory.Pop();
            currentGameState = gameStateHistory.Peek();
            
            fiftyMoveCounter = (int) (currentGameState & 4294950912) >> 14;
            var newEnPassantFile = (int)(currentGameState >> 4) & 15;
            if (newEnPassantFile != 0)
                zobristKey ^= Zobrist.enPassantFile[newEnPassantFile];

            var newCastleState = currentGameState & 0b1111;
            if (newCastleState != originalCastleState)
            {
                zobristKey ^= Zobrist.castlingRights[originalCastleState];
                zobristKey ^= Zobrist.castlingRights[newCastleState];
            }

            plyCount--;

            if (recordGameHistory && repetitionPosHistory.Count > 0)
            {
                repetitionPosHistory.Pop();
            }
        }

        private void InitializeBoard()
        {
            squares = new int[64];
            kingSquares = new int[2];

            gameStateHistory = new Stack<uint>();
            zobristKey = 0;
            repetitionPosHistory = new Stack<ulong>();
            plyCount = 0;
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
                squares[index] = piece;

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
                    case Piece.King: kingSquares[colourIndex] = index; break;
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
            plyCount = loadedPosition.plyCount;

            plyCount = loadedPosition.plyCount;

            zobristKey = Zobrist.CalculateZobristKey(this);
        }

    }
}