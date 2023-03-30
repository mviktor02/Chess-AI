using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.AI;
using UnityEngine;

namespace Chess.Core
{
    public class ArtificialPlayer : Player
    {
        const int bookMoveDelayMillis = 250;

        Search search;
        AISettings settings;
        bool moveFound;
        bool useSyzygy;
        Move move;
        Board board;
        CancellationTokenSource cancelSearchTimer;

        Book book;
        
        [DllImport("Fathom", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetPath(string path);
        
        [DllImport("Fathom", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.BStr)]
        private static extern string SyzygyLookup(string fen);

        public ArtificialPlayer(Board board, AISettings settings)
        {
			this.settings = settings;
			this.board = board;
			settings.requestAbortSearch += TimeOutThreadedSearch;
			search = new Search(ref board, settings);
			search.onSearchComplete += OnSearchComplete;
			search.searchDiagnostics = new Search.SearchDiagnostics ();
			book = BookCreator.LoadBookFromFile(settings.book);

			if (settings.syzygyPath.Length > 0)
			{
				useSyzygy = SetPath(settings.syzygyPath);
			}
        }

        // Update running on Unity main thread. This is used to return the chosen move so as
		// not to end up on a different thread and unable to interface with Unity stuff.
		public override void Update () {
			if (moveFound) {
				moveFound = false;
				MakeMove(move);
			}

			settings.diagnostics = search.searchDiagnostics;

		}

		public override void NotifyTurnToMove () {

			search.searchDiagnostics.isBook = false;
			moveFound = false;

			Move bookMove = Move.InvalidMove;
			if (settings.useBook && board.plyCount <= settings.maxBookPly) {
				if (book.HasPosition (board.zobristKey)) {
					bookMove = book.GetRandomBookMoveWeighted (board.zobristKey);
				}
			}

			if (bookMove.IsInvalid) {
				if (useSyzygy)
				{
					string fen = FenUtility.FenFromPosition(board);
					string fathomMove = SyzygyLookup(fen);
					if (!fathomMove.StartsWith("error"))
					{
						var move = MoveFromFathom(board, fathomMove);
						search.searchDiagnostics.moveVal = PGN.NotationFromMove(fen, move);
						settings.diagnostics = search.searchDiagnostics;
						Task.Delay (bookMoveDelayMillis).ContinueWith ((t) => PlayBookMove (move));
						return;
					}
				}
				if (settings.useThreading) {
					StartThreadedSearch ();
				} else {
					StartSearch ();
				}
			} else {
			
				search.searchDiagnostics.isBook = true;
				search.searchDiagnostics.moveVal = PGN.NotationFromMove(FenUtility.FenFromPosition(board), bookMove);
				settings.diagnostics = search.searchDiagnostics;
				Task.Delay (bookMoveDelayMillis).ContinueWith ((t) => PlayBookMove (bookMove));
				
			}
		}
		
		public static Move MoveFromFathom(in Board board, string fathomMove)
		{
			int flag = Move.Flag.None;
			string[] move = fathomMove.Split(' ');
			int from = int.Parse(move[0]);
			int to = int.Parse(move[1]);
			int fathomFlag = int.Parse(move[2]);

			if (fathomFlag != 0)
			{
				flag = fathomFlag switch
				{
					1 => Move.Flag.PromoteToQueen,
					2 => Move.Flag.PromoteToRook,
					3 => Move.Flag.PromoteToBishop,
					4 => Move.Flag.PromoteToKnight,
					_ => Move.Flag.None
				};
			}
			else if (Piece.GetPieceType(board.squares[from]) == Piece.Pawn)
			{
				if (Math.Abs(to - from) == 16)
				{
					flag = Move.Flag.PawnTwoForward;
				}
				else
				{
					int enPassantFile = ((int)(board.currentGameState >> 4) & 15) - 1;
					int enPassantSquare = -1;
					if (enPassantFile != -1)
					{
						enPassantSquare = 8 * (board.isWhitesTurn ? 5 : 2) + enPassantFile;
						if (to == enPassantSquare)
						{
							flag = Move.Flag.EnPassant;
						}
					}
				}
			}
			
			return new Move(from, to, flag);
		}

		void StartSearch() {
			search.StartSearch();
			moveFound = true;
		}

		void StartThreadedSearch() {
			//Thread thread = new Thread (new ThreadStart (search.StartSearch));
			//thread.Start ();
			Task.Factory.StartNew (() => search.StartSearch(), TaskCreationOptions.LongRunning);

			if (!settings.endlessSearchMode) {
				cancelSearchTimer = new CancellationTokenSource();
				Task.Delay(settings.searchTimeMillis, cancelSearchTimer.Token).ContinueWith((t) => TimeOutThreadedSearch ());
			}

		}

		// Note: called outside of Unity main thread
		void TimeOutThreadedSearch() {
			if (cancelSearchTimer == null || !cancelSearchTimer.IsCancellationRequested) {
				search.Abort();
			}
		}

		void PlayBookMove(Move bookMove) {
			this.move = bookMove;
			moveFound = true;
		}

		void OnSearchComplete(Move move) {
			// Cancel search timer in case search finished before timer ran out (can happen when a mate is found)
			cancelSearchTimer?.Cancel();
			moveFound = true;
			this.move = move;
		}
    }
}