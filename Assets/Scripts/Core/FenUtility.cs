using System;
using System.Collections.Generic;

namespace Chess.Core
{
    /// <summary>
    /// The easiest way to save and load gamestates in chess is to use the <a href="https://www.chessprogramming.org/Forsyth-Edwards_Notation">Forsyth-Edwards Notation</a>
    /// Format:
    /// piece_placement side_to_move castling_ability en_passant_target_square halfmove_clock fullmove_counter
    /// </summary>
    public class FenUtility
    {
        private static Dictionary<char, int> pieceTypeFromSymbol = new()
        {
            ['p'] = Piece.Pawn, ['r'] = Piece.Rook, ['n'] = Piece.Knight, ['b'] = Piece.Bishop, ['q'] = Piece.Queen, ['k'] = Piece.King
        };
        
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static LoadedPositionInfo PositionFromFen(string fen)
        {
            var loadedPositionInfo = new LoadedPositionInfo();
            var sections = fen.Split(' ');

            var file = 0;
            var rank = 7;

            foreach (var symbol in sections[0])
            {
                if (symbol == '/')
                {
                    file = 0;
                    rank--;
                }
                else
                {
                    if (char.IsDigit(symbol))
                    {
                        file += (int)char.GetNumericValue(symbol);
                    }
                    else
                    {
                        var colour = char.IsUpper(symbol) ? Piece.White : Piece.Black;
                        var type = pieceTypeFromSymbol[char.ToLower(symbol)];
                        loadedPositionInfo.squares[rank * 8 + file] = type | colour;
                        file++;
                    }
                }
            }

            loadedPositionInfo.whiteToMove = sections[1] == "w";

            var castlingRights = sections[2];
            loadedPositionInfo.whiteCastleKingside = castlingRights.Contains("K");
            loadedPositionInfo.whiteCastleQueenside = castlingRights.Contains("Q");
            loadedPositionInfo.blackCastleKingside = castlingRights.Contains("k");
            loadedPositionInfo.blackCastleQueenside = castlingRights.Contains("q");

            var enPassantFileName = sections[3][0].ToString();
            if (BoardRepresentation.FileNames.Contains(enPassantFileName))
                loadedPositionInfo.enPassantFile = BoardRepresentation.FileNames.IndexOf(enPassantFileName, StringComparison.Ordinal) + 1;

            int.TryParse(sections[4], out loadedPositionInfo.plyCount);

            return loadedPositionInfo;
        }
        
        public static string FenFromPosition(Board board) {
			string fen = "";
			for (int rank = 7; rank >= 0; rank--) {
				int numEmptyFiles = 0;
				for (int file = 0; file < 8; file++) {
					int i = rank * 8 + file;
					int piece = board.squares[i];
					if (piece != 0) {
						if (numEmptyFiles != 0) {
							fen += numEmptyFiles;
							numEmptyFiles = 0;
						}
						bool isBlack = Piece.IsColour (piece, Piece.Black);
						int pieceType = Piece.GetPieceType(piece);
						char pieceChar = pieceType switch
						{
							Piece.Rook => 'R',
							Piece.Knight => 'N',
							Piece.Bishop => 'B',
							Piece.Queen => 'Q',
							Piece.King => 'K',
							Piece.Pawn => 'P',
							_ => ' '
						};
						fen += isBlack ? pieceChar.ToString().ToLower() : pieceChar.ToString();
					} else {
						numEmptyFiles++;
					}

				}
				if (numEmptyFiles != 0) {
					fen += numEmptyFiles;
				}
				if (rank != 0) {
					fen += '/';
				}
			}

			// Side to move
			fen += ' ';
			fen += board.isWhitesTurn ? 'w' : 'b';

			// Castling
			bool whiteKingside = (board.currentGameState & 1) == 1;
			bool whiteQueenside = (board.currentGameState >> 1 & 1) == 1;
			bool blackKingside = (board.currentGameState >> 2 & 1) == 1;
			bool blackQueenside = (board.currentGameState >> 3 & 1) == 1;
			fen += ' ';
			fen += (whiteKingside) ? "K" : "";
			fen += (whiteQueenside) ? "Q" : "";
			fen += (blackKingside) ? "k" : "";
			fen += (blackQueenside) ? "q" : "";
			fen += ((board.currentGameState & 15) == 0) ? "-" : "";

			// En-passant
			fen += ' ';
			int epFile = (int) (board.currentGameState >> 4) & 15;
			if (epFile == 0) {
				fen += '-';
			} else {
				string fileName = BoardRepresentation.FileNames[epFile - 1].ToString ();
				int epRank = (board.isWhitesTurn) ? 6 : 3;
				fen += fileName + epRank;
			}

			// 50 move counter
			fen += ' ';
			fen += board.fiftyMoveCounter;

			// Full-move count (should be one at start, and increase after each move by black)
			fen += ' ';
			fen += (board.plyCount / 2) + 1;

			return fen;
		}
        
        public class LoadedPositionInfo
        {
            public int[] squares;
            public bool whiteCastleKingside;
            public bool whiteCastleQueenside;
            public bool blackCastleKingside;
            public bool blackCastleQueenside;
            public int enPassantFile;
            public bool whiteToMove;
            public int plyCount;

            public LoadedPositionInfo()
            {
                squares = new int[64];
            }
        }
    }
}