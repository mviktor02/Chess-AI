using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chess.Core.AI;
using Chess.Core.Util;
using Chess.UI;
using UnityEngine;

namespace Chess.Core
{
    public class GameManager : MonoBehaviour
    {
        public string exportLocation;
        public int numOfGamesToPlay;
        private int currentGameNum;
        private Result[] results;
        
        private enum Result
        {
            Playing,
            WhiteIsMated,
            BlackIsMated,
            Stalemate,
            Repetition,
            FiftyMoveRule,
            InsufficientMaterial
        }
        
        private BoardUI boardUI;
        public Board board { get; private set; }
        private Board searchBoard; // We need a separate board to perform searches on in order to not have any UI glitches. This is basically just a copy of the normal board.
        public PlayerSettings playerSettings;

        private Result gameResult;
        private Player whitePlayer;
        private Player blackPlayer;
        private Player playerToMove;

        private List<Move> moveHistory;

        private void Start()
        {
            boardUI = FindObjectOfType<BoardUI>();
            board = new Board();
            searchBoard = new Board();
            moveHistory = new List<Move>();
            if (playerSettings.whiteAiSettings) playerSettings.whiteAiSettings.diagnostics = new Search.SearchDiagnostics();
            if (playerSettings.blackAiSettings) playerSettings.blackAiSettings.diagnostics = new Search.SearchDiagnostics();

            results = new Result[numOfGamesToPlay];

            if (!string.IsNullOrWhiteSpace(exportLocation))
            {
                EnsureDirExists(exportLocation + '/');
            }
            
            NewGame(playerSettings.whitePlayer, playerSettings.blackPlayer);
        }

        private void Update()
        {
            if (gameResult == Result.Playing)
            {
                playerToMove?.Update();
            }
        }

        private void NewGame(PlayerType whitePlayerType, PlayerType blackPlayerType)
        {
            moveHistory.Clear();
            board.LoadStartPosition();
            searchBoard.LoadStartPosition();
            boardUI.UpdatePositions(board);
            boardUI.ResetSquareColours();

            CreatePlayer(ref whitePlayer, whitePlayerType);
            CreatePlayer(ref blackPlayer, blackPlayerType, false);

            gameResult = Result.Playing;
            PrintGameResult(gameResult);

            NotifyPlayerToMove();
        }

        private Result GetGamestate()
        {
            var moveGenerator = new MoveGenerator();
            var moves = moveGenerator.GenerateMoves(board);

            // Mate or Stalemate
            if (moves.Count == 0)
            {
                if (moveGenerator.IsInCheck())
                {
                    return board.isWhitesTurn ? Result.WhiteIsMated : Result.BlackIsMated;
                }

                return Result.Stalemate;
            }
            
            // Fifty move rule
            if (board.fiftyMoveCounter >= 100)
            {
                return Result.FiftyMoveRule;
            }

            // Repetition
            var repetitionCount = board.repetitionPosHistory.Count(x => x == board.zobristKey);
            if (repetitionCount == 3)
            {
                return Result.Repetition;
            }
            
            // Insufficient material
            var numOfPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
            var numOfRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
            var numOfQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
            var numOfKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
            var numOfBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

            if (numOfPawns + numOfRooks + numOfQueens == 0)
            {
                if (numOfKnights == 1 || numOfBishops == 1)
                {
                    return Result.InsufficientMaterial;
                }
                if (board.bishops[Board.WhiteIndex].Count == 1 && board.bishops[Board.BlackIndex].Count == 1)
                {
                    var whiteBishopSquare = board.bishops[Board.WhiteIndex][0];
                    var blackBishopSquare = board.bishops[Board.BlackIndex][0];
                    if (BoardRepresentation.IsLightSquare(whiteBishopSquare) ==
                        BoardRepresentation.IsLightSquare(blackBishopSquare))
                    {
                        return Result.InsufficientMaterial;
                    }
                }
                if (numOfKnights + numOfBishops == 0)
                {
                    return Result.InsufficientMaterial;
                }
            }
            
            return Result.Playing;
        }

        private void OnMoveChosen(Move move)
        {
            var animateMove = playerToMove is ArtificialPlayer;
            board.MakeMove(move);
            searchBoard.MakeMove(move);

            moveHistory.Add(move);
            boardUI.OnMoveMade(board, move, animateMove);
            
            NotifyPlayerToMove();
        }

        private void NotifyPlayerToMove()
        {
            gameResult = GetGamestate();
            PrintGameResult(gameResult);

            if (gameResult == Result.Playing) {
                playerToMove = (board.isWhitesTurn) ? whitePlayer : blackPlayer;
                playerToMove.NotifyTurnToMove ();

            } else {
                ExportToPGN($"{currentGameNum+1} - {Enum.GetName(typeof(Result), gameResult)}");
                ExportDiagnostics();
                if (currentGameNum < numOfGamesToPlay - 1)
                {
                    results[currentGameNum] = gameResult;
                    currentGameNum++;
                    NewGame(playerSettings.whitePlayer, playerSettings.blackPlayer);
                }
                else
                {
                    ExportResults();
                }
            }
        }

        private void EnsureDirExists(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
        }

        private void ExportToPGN(string filename = "exported game")
        {
            string pgn = PGNCreator.CreatePGN(moveHistory.ToArray());
            using var writer = new StreamWriter(exportLocation + '/' + filename + ".pgn");
            writer.Write(pgn);
        }

        private void ExportResults()
        {
            using var writer = new StreamWriter(exportLocation + "/results.txt");
            writer.WriteLine($"WhiteIsMated: {results.Count(x => x == Result.WhiteIsMated)}");
            writer.WriteLine($"BlackIsMated: {results.Count(x => x == Result.BlackIsMated)}");
            writer.WriteLine($"Stalemate: {results.Count(x => x == Result.Stalemate)}");
            writer.WriteLine($"Repetition: {results.Count(x => x == Result.Repetition)}");
            writer.WriteLine($"FiftyMoveRule: {results.Count(x => x == Result.FiftyMoveRule)}");
            writer.WriteLine($"InsufficientMaterial: {results.Count(x => x == Result.InsufficientMaterial)}");
        }

        private void ExportDiagnostics()
        {
            if (playerSettings.whitePlayer == PlayerType.AI)
                ExportDiagnostics("white", ((ArtificialPlayer)whitePlayer).diagnosticsExport);
            if (playerSettings.blackPlayer == PlayerType.AI)
                ExportDiagnostics("black", ((ArtificialPlayer)blackPlayer).diagnosticsExport);
        }

        private void ExportDiagnostics(string player, List<ArtificialPlayer.DiagnosticsExport> diagnostics)
        {
            EnsureDirExists(exportLocation + $"/diagnostics/{player}/");
            using var writer = new StreamWriter(exportLocation + $"/diagnostics/{player}/{currentGameNum+1}.txt");
            writer.WriteLine($"Avg Search Depth: {diagnostics.Average(x => x.Depth)}");
            writer.WriteLine($"Avg Num of Positions Evaluated: {diagnostics.Average(x => x.NumPosEvaluated)}");
            writer.WriteLine();
            foreach (var stats in diagnostics)
            {
                writer.WriteLine($"{stats.Depth}: {stats.NumPosEvaluated}");
            }
        }

        private void PrintGameResult(Result result)
        {
            string text;
            switch (result)
            {
                case Result.Playing:
                    text = "";
                    break;
                case Result.WhiteIsMated:
                case Result.BlackIsMated:
                    text = "Checkmate!";
                    break;
                case Result.FiftyMoveRule:
                    text = "Draw";
                    text += "\n(50 move rule)";
                    break;
                case Result.Repetition:
                    text = "Draw";
                    text += "\n(3-fold repetition)";
                    break;
                case Result.Stalemate:
                    text = "Draw";
                    text += "\n(Stalemate)";
                    break;
                case Result.InsufficientMaterial:
                    text = "Draw";
                    text += "\n(Insufficient material)";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            if (string.IsNullOrWhiteSpace(text)) return;
            
            Debug.Log(text);
        }
        
        public void ExportToPGNAndOpenLichess() {
            string pgn = PGN.CreatePGN (moveHistory.ToArray ());
            string baseUrl = "https://www.lichess.org/paste?pgn=";
            string escapedPGN = UnityEngine.Networking.UnityWebRequest.EscapeURL (pgn);
            string url = baseUrl + escapedPGN;

            Application.OpenURL (url);
            TextEditor t = new TextEditor ();
            t.text = pgn;
            t.SelectAll ();
            t.Copy ();
        }

        private void CreatePlayer(ref Player player, PlayerType playerType, bool isWhite = true)
        {
            if (player != null)
            {
                player.onMoveEvent -= OnMoveChosen;
            }

            if (playerType == PlayerType.Human) {
                player = new HumanPlayer (board);
            } else {
                player = new ArtificialPlayer(searchBoard, isWhite ? playerSettings.whiteAiSettings : playerSettings.blackAiSettings);
            }
            player.onMoveEvent += OnMoveChosen;
        }
        
        public enum PlayerType { Human, AI }
    }
}