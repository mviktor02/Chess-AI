using System;
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
        private ulong opponentKnightAttackMap;
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
            friendlyKingSquare = board.kingSquares[friendlyColourIndex];
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

        /// <summary>
        /// This will only return the correct value after GenerateMoves() has been called in the current position
        /// </summary>
        public bool IsInCheck()
        {
            return isInCheck;
        }

        private bool IsSquarePinned(int square)
        {
            return doPinsExist && ((pinRayBitmask >> square) & 1) != 0;
        }

        private bool IsSquareInCheckRay(int square)
        {
            return isInCheck && ((checkRayBitmask >> square) & 1) != 0;
        }

        private bool IsSquareAttacked(int square)
        {
            return BitboardUtil.ContainsSquare(opponentAttackMap, square);
        }

        private bool IsMoveAlongRay(int rayDir, int startSquare, int targetSquare)
        {
            var moveDir = directionLookup[targetSquare - startSquare + 63];
            return rayDir == moveDir || -rayDir == moveDir;
        }

        private bool HasKingsideCastleRight()
        {
            var mask = board.isWhitesTurn ? 1 : 4;
            return (board.currentGameState & mask) != 0;
        }

        private bool HasQueensideCastleRight()
        {
            var mask = board.isWhitesTurn ? 2 : 8;
            return (board.currentGameState & mask) != 0;
        }

        private void GenerateKingMoves()
        {
            for (var i = 0; i < kingMoves[friendlyKingSquare].Length; i++)
            {
                var targetSquare = kingMoves[friendlyKingSquare][i];
                var pieceOnTargetSquare = board.squares[targetSquare];

                if (Piece.IsColour(pieceOnTargetSquare, friendlyColour))
                    continue;

                var isCapture = Piece.IsColour(pieceOnTargetSquare, opponentColour);
                // King can't move to target square if an enemy piece can attack it, unless that piece is being captured
                // Also skip if we aren't generating quiet moves and the move isn't a capture
                // TODO this might be bugged if a piece is protected by another and we try to capture it
                if (!isCapture && (!generateQuietMoves || IsSquareInCheckRay(targetSquare)))
                    continue;

                if (IsSquareAttacked(targetSquare))
                    continue;
                
                moves.Add(new Move(friendlyKingSquare, targetSquare));

                if (isInCheck || isCapture)
                    continue;
                
                // Kingside castling
                if (targetSquare is f1 or f8 && HasKingsideCastleRight())
                {
                    var kingsideCastleSquare = targetSquare + 1;
                    if (board.squares[kingsideCastleSquare] == Piece.None &&
                        !IsSquareAttacked(kingsideCastleSquare))
                    {
                        moves.Add(new Move(friendlyKingSquare, kingsideCastleSquare, Move.Flag.Castling));
                    }
                }
                // Queenside castling
                else if (targetSquare is d1 or d8 && HasQueensideCastleRight())
                {
                    var queensideCastleSquare = targetSquare - 1;
                    if (board.squares[queensideCastleSquare] == Piece.None &&
                        board.squares[queensideCastleSquare - 1] == Piece.None &&
                        !IsSquareAttacked(queensideCastleSquare))
                    {
                        moves.Add(new Move(friendlyKingSquare, queensideCastleSquare, Move.Flag.Castling));
                    }
                }
            }
        }

        private void GenerateSlidingMoves()
        {
            var rooks = board.rooks[friendlyColourIndex];
            for (var i = 0; i < rooks.Count; i++)
                GenerateSlidingPieceMoves(rooks[i], 0, 4);
            
            var bishops = board.bishops[friendlyColourIndex];
            for (var i = 0; i < bishops.Count; i++)
                GenerateSlidingPieceMoves (bishops[i], 4, 8);

            var queens = board.queens[friendlyColourIndex];
            for (var i = 0; i < queens.Count; i++)
                GenerateSlidingPieceMoves (queens[i], 0, 8);
        }

        private void GenerateSlidingPieceMoves(int startSquare, int startDirIndex, int endDirIndex)
        {
            var isPinned = IsSquarePinned(startSquare);

            // If this piece is pinned and the king is in check, this piece can't move
            if (isInCheck && isPinned)
                return;

            for (var directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                var currentDirectionOffset = directionOffsets[directionIndex];

                // If pinned, this piece can only move along the ray towards or away from the friendly king, so skip other directions
                if (isPinned && !IsMoveAlongRay(currentDirectionOffset, friendlyKingSquare, startSquare))
                    continue;

                for (var n = 0; n < numberOfSquaresToEdge[startSquare][directionIndex]; n++)
                {
                    var targetSquare = startSquare + currentDirectionOffset * (n + 1);
                    var targetSquarePiece = board.squares[targetSquare];
                    
                    // If blocked by friendly piece, stop looking in this direction
                    if (Piece.IsColour(targetSquarePiece, friendlyColour))
                        break;

                    var isCapture = targetSquarePiece != Piece.None;
                    var movePreventsCheck = IsSquareInCheckRay(targetSquare);
                    if (movePreventsCheck || !isInCheck)
                    {
                        if (generateQuietMoves || isCapture)
                        {
                            moves.Add(new Move(startSquare, targetSquare));
                        }
                    }
                    
                    // If square isn't empty, can't move further in this direction.
                    // If this move blocked a check, further moves won't.
                    if (isCapture || movePreventsCheck)
                        break;
                }
            }
        }

        private void GenerateKnightMoves()
        {
            var knights = board.knights[friendlyColourIndex];

            for (var i = 0; i < knights.Count; i++)
            {
                var startSquare = knights[i];
                
                // Knight can't move if it's pinned
                if (IsSquarePinned(startSquare))
                    continue;

                for (var knightMoveIndex = 0; knightMoveIndex < knightMoves[startSquare].Length; knightMoveIndex++)
                {
                    var targetSquare = knightMoves[startSquare][knightMoveIndex];
                    var targetSquarePiece = board.squares[targetSquare];
                    var isCapture = Piece.IsColour(targetSquarePiece, opponentColour);
                    
                    if (!generateQuietMoves && !isCapture) continue;
                    
                    // Skip if square contains a friendly piece or if we're in check and the move wouldn't prevent it
                    if (Piece.IsColour(targetSquarePiece, friendlyColour) || (isInCheck && !IsSquareInCheckRay(targetSquare)))
                        continue;
                        
                    moves.Add(new Move(startSquare, targetSquare));
                }
            }
        }

        private void GeneratePawnMoves()
        {
            var pawns = board.pawns[friendlyColourIndex];
            var pawnMoveOffset = friendlyColour == Piece.White ? 8 : -8;
            var startRank = board.isWhitesTurn ? 1 : 6;
            var finalRankBeforePromotion = board.isWhitesTurn ? 6 : 1;

            var enPassantFile = ((int)(board.currentGameState >> 4) & 15) - 1;
            var enPassantSquare = -1;
            if (enPassantFile != -1)
                enPassantSquare = 8 * (board.isWhitesTurn ? 5 : 2) + enPassantFile;

            for (var i = 0; i < pawns.Count; i++)
            {
                var startSquare = pawns[i];
                var rank = RankIndex(startSquare);
                var isOneStepFromPromotion = rank == finalRankBeforePromotion;

                if (generateQuietMoves)
                {
                    var oneForward = startSquare + pawnMoveOffset;

                    if (board.squares[oneForward] == Piece.None)
                    {
                        if (!IsSquarePinned(startSquare) ||
                            IsMoveAlongRay(pawnMoveOffset, startSquare, friendlyKingSquare))
                        {
                            if (!isInCheck || IsSquareInCheckRay(oneForward))
                            {
                                if (isOneStepFromPromotion)
                                {
                                    MakePromotionMoves(startSquare, oneForward);
                                }
                                else
                                {
                                    moves.Add(new Move(startSquare, oneForward));
                                }
                            }

                            if (rank == startRank)
                            {
                                var twoForward = oneForward + pawnMoveOffset;
                                if (board.squares[twoForward] == Piece.None)
                                {
                                    if (!isInCheck || IsSquareInCheckRay(twoForward))
                                    {
                                        moves.Add(new Move(startSquare, twoForward, Move.Flag.PawnTwoForward));
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Captures
                for (var j = 0; j < 2; j++)
                {
                    if (numberOfSquaresToEdge[startSquare][pawnAttackDirections[friendlyColourIndex][j]] > 0)
                    {
                        var pawnCaptureDirection = directionOffsets[pawnAttackDirections[friendlyColourIndex][j]];
                        var targetSquare = startSquare + pawnCaptureDirection;
                        var targetSquarePiece = board.squares[targetSquare];
                        
                        if (IsSquarePinned(startSquare) && !IsMoveAlongRay(pawnCaptureDirection, friendlyKingSquare, startSquare))
                            continue;

                        if (Piece.IsColour(targetSquarePiece, opponentColour))
                        {
                            if (isInCheck && !IsSquareInCheckRay(targetSquare))
                                continue;
                            
                            if (isOneStepFromPromotion)
                            {
                                MakePromotionMoves(startSquare, targetSquare);
                            }
                            else
                            {
                                moves.Add(new Move(startSquare, targetSquare));
                            }
                        }

                        if (targetSquare == enPassantSquare)
                        {
                            var enPassantCapturedPawnSquare = targetSquare + (board.isWhitesTurn ? -8 : 8);
                            if (!IsInCheckAfterEnPassant(startSquare, targetSquare, enPassantCapturedPawnSquare))
                            {
                                moves.Add(new Move(startSquare, targetSquare, Move.Flag.EnPassant));
                            }
                        }
                    }
                }
            }
        }
        
        private void MakePromotionMoves (int fromSquare, int toSquare) {
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToQueen));
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToKnight));
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToRook));
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToBishop));
        }

        private bool IsInCheckAfterEnPassant(int startSquare, int targetSquare, int enPassantCapturedPawnSquare)
        {
            board.squares[targetSquare] = board.squares[startSquare];
            board.squares[startSquare] = Piece.None;
            board.squares[enPassantCapturedPawnSquare] = Piece.None;

            var isInCheckAfterCapture = IsSquareAttackedAfterEnPassant(enPassantCapturedPawnSquare);

            board.squares[targetSquare] = Piece.None;
            board.squares[startSquare] = Piece.Pawn | friendlyColour;
            board.squares[enPassantCapturedPawnSquare] = Piece.Pawn | opponentColour;
            return isInCheckAfterCapture;
        }

        private bool IsSquareAttackedAfterEnPassant(int enPassantCapturedSquare)
        {
            if (BitboardUtil.ContainsSquare(opponentAttackMapNoPawns, friendlyKingSquare))
                return true;

            // Loop through the horizontal direction towards the en passant capture to see if any enemy piece now attacks the king
            var directionIndex = enPassantCapturedSquare < friendlyKingSquare ? 2 : 3;
            for (var i = 0; i < numberOfSquaresToEdge[friendlyKingSquare][directionIndex]; i++)
            {
                var squareIndex = friendlyKingSquare + directionOffsets[directionIndex] * (i + 1);
                var piece = board.squares[squareIndex];
                
                if (piece == Piece.None) continue;
                
                // Friendly piece is blocking view of this square
                if (Piece.IsColour(piece, friendlyColour))
                    break;

                if (Piece.IsRookOrQueen(piece))
                    return true;
                    
                // This piece is not able to move in the current direction (it also blocks any checks along this line)
                break;
            }

            // Check if enemy pawn is controlling this square (can't use pawn attack bitboard, because the pawn has been captured)
            for (var i = 0; i < 2; i++)
            {
                // Check if a square exists diagonal to the friendly king, from which an enemy pawn could be attacking it.
                // If that's not the case, skip to next iteration.
                if (numberOfSquaresToEdge[friendlyKingSquare][pawnAttackDirections[friendlyColourIndex][i]] <= 0)
                    continue;
                
                // Move in direction friendly pawns attack to get square from which an enemy pawn would attack
                var piece = board.squares[
                    friendlyKingSquare + directionOffsets[pawnAttackDirections[friendlyColourIndex][i]]
                ];
                if (piece == (Piece.Pawn | opponentColour))
                    return true;
            }

            return false;
        }

        private void CalculateAttackData()
        {
            GenerateSlidingAttackMap();
            // Search squares in all directions around the friendly king for checks or pins by the enemy sliding pieces
            var startDirIndex = 0;
            var endDirIndex = 8;

            if (board.queens[opponentColourIndex].Count == 0)
            {
                startDirIndex = board.rooks[opponentColourIndex].Count > 0 ? 0 : 4;
                endDirIndex = board.bishops[opponentColourIndex].Count > 0 ? 8 : 4;
            }

            for (var dir = startDirIndex; dir < endDirIndex; dir++)
            {
                var isDiagonal = dir > 3;

                var n = numberOfSquaresToEdge[friendlyKingSquare][dir];
                var directionOffset = directionOffsets[dir];
                var isFriendlyPieceAlongRay = false;
                ulong rayMask = 0;

                for (var i = 0; i < n; i++)
                {
                    var squareIndex = friendlyKingSquare + directionOffset * (i + 1);
                    rayMask |= 1ul << squareIndex;
                    var piece = board.squares[squareIndex];
                    if (piece == Piece.None) continue;
                    
                    if (Piece.IsColour(piece, friendlyColour))
                    {
                        // First friendly piece we've come across in this direction, pin is still possible
                        if (!isFriendlyPieceAlongRay)
                        {
                            isFriendlyPieceAlongRay = true;
                        }
                        // Second friendly piece we've found in this direction, pin isn't possible
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        var pieceType = Piece.GetPieceType(piece);
                        
                        // Check if the piece is in the bitmask of pieces able to move in current direction
                        if ((isDiagonal && Piece.IsBishopOrQueen(pieceType)) ||
                            (!isDiagonal && Piece.IsRookOrQueen(pieceType)))
                        {
                            // Friendly piece blocks the check -> that piece is pinned
                            if (isFriendlyPieceAlongRay)
                            {
                                doPinsExist = true;
                                pinRayBitmask |= rayMask;
                            }
                            // No friendly pieces block the attack -> we're in check
                            else
                            {
                                checkRayBitmask |= rayMask;
                                isInDoubleCheck = isInCheck; // If we're already in check, then this is a double check
                                isInCheck = true;
                            }
                        }
                        
                        break;
                    }
                }

                // Stop searching for pins if we're in a double check, as the king is the only piece able to move in that case
                if (isInDoubleCheck)
                    break;
            }
            
            // Knight attacks
            var opponentKnights = board.knights[opponentColourIndex];
            opponentKnightAttackMap = 0;
            var isKnightCheck = false;

            for (var knightIndex = 0; knightIndex < opponentKnights.Count; knightIndex++)
            {
                var startSquare = opponentKnights[knightIndex];
                opponentKnightAttackMap |= knightAttackBitboards[startSquare];

                if (isKnightCheck || !BitboardUtil.ContainsSquare(opponentKnightAttackMap, friendlyKingSquare))
                    continue;
                
                isKnightCheck = true;
                isInDoubleCheck = isInCheck;
                isInCheck = true;
                checkRayBitmask |= 1ul << startSquare;
            }
            
            // Pawn attacks
            var opponentPawns = board.pawns[opponentColourIndex];
            opponentPawnAttackMap = 0;
            var isPawnCheck = false;

            for (var pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++)
            {
                var pawnSquare = opponentPawns[pawnIndex];
                var pawnAttacks = pawnAttackBitboards[pawnSquare][opponentColourIndex];
                opponentPawnAttackMap |= pawnAttacks;

                if (isPawnCheck || !BitboardUtil.ContainsSquare(pawnAttacks, friendlyKingSquare))
                    continue;

                isPawnCheck = true;
                isInDoubleCheck = isInCheck;
                isInCheck = true;
                checkRayBitmask |= 1ul << pawnSquare;
            }

            var enemyKingSquare = board.kingSquares[opponentColourIndex];

            opponentAttackMapNoPawns =
                opponentSlidingAttackMap | opponentKnightAttackMap | kingAttackBitboards[enemyKingSquare];
            opponentAttackMap = opponentAttackMapNoPawns | opponentPawnAttackMap;
        }

        private void GenerateSlidingAttackMap()
        {
            opponentSlidingAttackMap = 0;

            var enemyRooks = board.rooks[opponentColourIndex];
            for (var i = 0; i < enemyRooks.Count; i++)
                UpdateSlidingAttackPiece(enemyRooks[i], 0, 4);
            
            var enemyQueens = board.queens[opponentColourIndex];
            for (var i = 0; i < enemyQueens.Count; i++)
                UpdateSlidingAttackPiece(enemyQueens[i], 0, 8);
            
            var enemyBishops = board.bishops[opponentColourIndex];
            for (var i = 0; i < enemyBishops.Count; i++)
                UpdateSlidingAttackPiece(enemyBishops[i], 4, 8);
        }
        
        private void UpdateSlidingAttackPiece(int startSquare, int startDirIndex, int endDirIndex) {
            for (var directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++) {
                var currentDirOffset = directionOffsets[directionIndex];
                for (var n = 0; n < numberOfSquaresToEdge[startSquare][directionIndex]; n++) {
                    var targetSquare = startSquare + currentDirOffset * (n + 1);
                    var targetSquarePiece = board.squares[targetSquare];
                    opponentSlidingAttackMap |= 1ul << targetSquare;
                    if (targetSquare == friendlyKingSquare)
                        continue;
                    if (targetSquarePiece != Piece.None)
                        break;
                }
            }
        }
    }
}