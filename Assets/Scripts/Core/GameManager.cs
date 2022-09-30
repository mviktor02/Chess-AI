using System;
using System.Collections.Generic;
using System.Linq;
using Chess.UI;
using UnityEngine;

namespace Chess.Core
{
    public class GameManager : MonoBehaviour
    {
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
        private Board board { get; set; }

        private Result gameResult;
        private Player whitePlayer;
        private Player blackPlayer;
        private Player playerToMove;

        private List<Move> moveHistory;

        private void Start()
        {
            boardUI = FindObjectOfType<BoardUI>();
            board = new Board();
            moveHistory = new List<Move>();

            Debug.Log("Gamemanager Start");
            
            NewGame();
        }

        private void Update()
        {
            if (gameResult == Result.Playing)
            {
                playerToMove?.Update();
            }
        }

        private void NewGame()
        {
            board.LoadStartPosition();
            boardUI.UpdatePositions(board);
            boardUI.ResetSquareColours();

            CreatePlayer(ref whitePlayer);
            CreatePlayer(ref blackPlayer);

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
                Debug.Log ("Game Over");
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
            // TODO show message on screen
            Debug.Log(text);
        }

        private void CreatePlayer(ref Player player)
        {
            if (player != null)
            {
                player.onMoveEvent -= OnMoveChosen;
            }

            player = new HumanPlayer(board);
            player.onMoveEvent += OnMoveChosen;
        }
    }
}