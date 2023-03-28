namespace Chess.Core.Util
{
    public class PGNCreator
    {
	    public static string CreatePGN(Move[] moves) {
			string pgn = "";
			Board board = new Board ();
			board.LoadStartPosition ();

			for (int plyCount = 0; plyCount < moves.Length; plyCount++) {
				string moveString = NotationFromMove (board, moves[plyCount]);
				board.MakeMove (moves[plyCount]);

				if (plyCount % 2 == 0) {
					pgn += ((plyCount / 2) + 1) + ". ";
				}
				pgn += moveString + " ";
			}

			return pgn;
		}

		public static string NotationFromMove(string currentFen, Move move) {
			Board board = new Board ();
			board.LoadPosition (currentFen);
			return NotationFromMove (board, move);
		}

		static string NotationFromMove (Board board, Move move) {

			MoveGenerator moveGen = new MoveGenerator ();

			int movePieceType = Piece.GetPieceType(board.squares[move.StartSquare]);
			int capturedPieceType = Piece.GetPieceType(board.squares[move.TargetSquare]);

			if (move.MoveFlag == Move.Flag.Castling) {
				int delta = move.TargetSquare - move.StartSquare;
				switch (delta)
				{
					case 2:
						return "O-O";
					case -2:
						return "O-O-O";
				}
			}

			string moveNotation = GetSymbolFromPieceType(movePieceType);

			// check if any ambiguity exists in notation (e.g if e2 can be reached via Nfe2 and Nbe2)
			if (movePieceType != Piece.Pawn && movePieceType != Piece.King) {
				var allMoves = moveGen.GenerateMoves (board);

				foreach (var altMove in allMoves) {

					if (altMove.StartSquare != move.StartSquare && altMove.TargetSquare == move.TargetSquare) { // if moving to same square from different square
						if (Piece.GetPieceType(board.squares[altMove.StartSquare]) == movePieceType) { // same piece type
							int fromFileIndex = BoardRepresentation.FileIndex (move.StartSquare);
							int alternateFromFileIndex = BoardRepresentation.FileIndex (altMove.StartSquare);
							int fromRankIndex = BoardRepresentation.RankIndex (move.StartSquare);
							int alternateFromRankIndex = BoardRepresentation.RankIndex (altMove.StartSquare);

							if (fromFileIndex != alternateFromFileIndex) { // pieces on different files, thus ambiguity can be resolved by specifying file
								moveNotation += BoardRepresentation.FileNames[fromFileIndex];
								break; // ambiguity resolved
							}
							if (fromRankIndex != alternateFromRankIndex) {
								moveNotation += (fromRankIndex + 1);
								break; // ambiguity resolved
							}
						}
					}

				}
			}

			if (capturedPieceType != 0) { // add 'x' to indicate capture
				if (movePieceType == Piece.Pawn) {
					moveNotation += BoardRepresentation.FileNames[BoardRepresentation.FileIndex(move.StartSquare)];
				}
				moveNotation += "x";
			} else { // check if capturing ep
				if (move.MoveFlag == Move.Flag.EnPassant) {
					moveNotation += BoardRepresentation.FileNames[BoardRepresentation.FileIndex(move.StartSquare)] + "x";
				}
			}

			moveNotation += BoardRepresentation.FileNames[BoardRepresentation.FileIndex(move.TargetSquare)];
			moveNotation += (BoardRepresentation.RankIndex(move.TargetSquare) + 1);

			// add promotion piece
			if (move.IsPromotion) {
				int promotionPieceType = move.PromotionPieceType;
				moveNotation += "=" + GetSymbolFromPieceType (promotionPieceType);
			}

			board.MakeMove(move, false);
			var legalResponses = moveGen.GenerateMoves(board);
			// add check/mate symbol if applicable
			if (moveGen.IsInCheck()) {
				if (legalResponses.Count == 0) {
					moveNotation += "#";
				} else {
					moveNotation += "+";
				}
			}
			board.UnmakeMove(move, false);

			return moveNotation;
		}

		static string GetSymbolFromPieceType(int pieceType)
		{
			return pieceType switch
			{
				Piece.Rook => "R",
				Piece.Knight => "N",
				Piece.Bishop => "B",
				Piece.Queen => "Q",
				Piece.King => "K",
				_ => ""
			};
		}
    }
}