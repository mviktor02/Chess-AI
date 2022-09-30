using System;
using Chess.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chess.Core
{
    public class HumanPlayer : Player
    {
        public enum InputState
        {
            None,
            Selected,
            Dragging
        }

        private InputState inputState;
        
        private BoardUI boardUI;
        private Board board;
        private Camera camera;
        private Coord selectedSquare;
        private Coord hoveringSquare;
        private static readonly Coord INVALID_SQUARE = new(-1, -1);
        
        public HumanPlayer(Board board)
        {
            this.board = board;
            boardUI = Object.FindObjectOfType<BoardUI>();
            camera = Camera.main;
        }
        
        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            
        }

        private void HandleInput()
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out var info, 100, LayerMask.GetMask("Tile")))
            {
                var hitPosition = LookupTileIndex(info.transform.gameObject);
                hoveringSquare = hitPosition;
            }

            switch (inputState)
            {
                case InputState.None:
                    HandleSelection();
                    break;
                case InputState.Selected:
                    HandleClick();
                    break;
                case InputState.Dragging:
                    HandleDrag(ray);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelSelection();
            }
        }


        private Coord LookupTileIndex(Object hitInfo)
        {
            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    if (hitInfo.name == BoardRepresentation.SquareNameFromCoordinate(file, rank))
                    {
                        return new Coord(file, rank);
                    }
                }
            }

            return INVALID_SQUARE;
        }

        private void HandleSelection()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            if (TryGetSquareUnderMouse(out selectedSquare))
            {
                var index = BoardRepresentation.IndexFromCoord(selectedSquare);
                if (Piece.IsColour(board.squares[index], board.colourToMove))
                {
                    boardUI.HighlightLegalMoves(board, selectedSquare);
                    boardUI.SelectSquare(selectedSquare);
                    inputState = InputState.Dragging;
                }
            }
        }

        private void HandleDrag(Ray ray)
        {
            var horizontalPlane = new Plane(Vector3.forward, Vector3.zero);
            if (horizontalPlane.Raycast(ray, out var distance))
            {
                boardUI.DragPiece(selectedSquare, ray.GetPoint(distance) + Vector3.forward * BoardUI.pieceDragDepth);
            }
            if (Input.GetMouseButtonUp(0))
            {
                HandlePlacement();
            }
        }

        private void HandleClick()
        {
            if (Input.GetMouseButton(0))
                HandlePlacement();
        }

        private void HandlePlacement()
        {
            if (TryGetSquareUnderMouse(out var targetSquare))
            {
                if (targetSquare.Equals(selectedSquare))
                {
                    boardUI.ResetPiecePosition(selectedSquare);
                    if (inputState == InputState.Dragging)
                        inputState = InputState.Selected;
                    else
                    {
                        inputState = InputState.None;
                        boardUI.DeselectSquare(selectedSquare);
                    }
                }
                else
                {
                    var targetIndex =
                        BoardRepresentation.IndexFromCoord(targetSquare.fileIndex, targetSquare.rankIndex);
                    if (Piece.IsColour(board.squares[targetIndex], board.colourToMove) &&
                        board.squares[targetIndex] != 0)
                    {
                        CancelSelection();
                        HandleSelection();
                    }
                    else
                    {
                        TryMakeMove(selectedSquare, targetSquare);
                    }
                }
            }
            else
            {
                CancelSelection();
            }
        }

        private void TryMakeMove(Coord startSquare, Coord targetSquare)
        {
            var startIndex = BoardRepresentation.IndexFromCoord(startSquare);
            var targetIndex = BoardRepresentation.IndexFromCoord(targetSquare);
            var isMoveLegal = false;
            var chosenMove = new Move();
            var moveGenerator = new MoveGenerator();

            var legalMoves = moveGenerator.GenerateMoves(board);
            foreach (var move in legalMoves)
            {
                if (move.StartSquare != startIndex || move.TargetSquare != targetIndex)
                    continue;
                
                isMoveLegal = true;
                chosenMove = move;
                break;
            }

            if (isMoveLegal)
            {
                MakeMove(chosenMove);
                inputState = InputState.None;
            }
            else
            {
                CancelSelection();
            }
        }

        private void CancelSelection()
        {
            if (inputState == InputState.None) return;
            
            inputState = InputState.None;
            boardUI.DeselectSquare(selectedSquare);
            boardUI.ResetPiecePosition(selectedSquare);
        }
        
        private bool TryGetSquareUnderMouse(out Coord selectedCoord)
        {
            selectedCoord = hoveringSquare;
            return selectedCoord.fileIndex is >= 0 and < 8 && selectedCoord.rankIndex is >= 0 and < 8;
        }
    }
}