using System;
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
        private Board Board { get; set; }

        private Result gameResult;
        private Player whitePlayer;
        private Player blackPlayer;
        private Player playerToMove;

        private void Start()
        {
            boardUI = FindObjectOfType<BoardUI>();
            Board = new Board();

            Debug.Log("Gamemanager Start");
            
            NewGame();
        }

        private void Update()
        {
            if (gameResult == Result.Playing)
            {
                playerToMove.Update();
            }
        }

        private void NewGame()
        {
            Board.LoadStartPosition();
            boardUI.UpdatePositions(Board);
            boardUI.ResetSquareColours();

            CreatePlayer(ref whitePlayer);
            CreatePlayer(ref blackPlayer);

            gameResult = Result.Playing;
            PrintGameResult(gameResult);

            NotifyPlayerToMove();
        }

        private Result GetGamestate()
        {
            // TODO
            return Result.Playing;
        }

        private void OnMoveChosen(Move obj)
        {
            // TODO
        }

        private void NotifyPlayerToMove()
        {
            gameResult = GetGamestate();
            PrintGameResult(gameResult);

            if (gameResult == Result.Playing) {
                playerToMove = (Board.isWhitesTurn) ? whitePlayer : blackPlayer;
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
            Debug.Log(text);
        }

        private void CreatePlayer(ref Player player)
        {
            if (player != null)
            {
                player.onMoveEvent -= OnMoveChosen;
            }

            player = new HumanPlayer(Board);
            player.onMoveEvent += OnMoveChosen;
        }
    }
}