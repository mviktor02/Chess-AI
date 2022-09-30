using System;
using System.Collections;
using Chess.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chess.UI
{
    public class BoardUI : MonoBehaviour
    {
        public PieceTheme pieceTheme;
        public BoardTheme boardTheme;
        
        public bool whiteIsBottom = true;

        private Camera _camera;
        private MeshRenderer[,] squareRenderers;
        private SpriteRenderer[,] pieceRenderers;

        private Move lastMadeMove;
        private MoveGenerator moveGenerator;

        public const float pieceDepth = -0.1f;
        public const float pieceDragDepth = -0.2f;

        private void Awake()
        {
            moveGenerator = new MoveGenerator();
            CreateBoardUI();
            _camera = Camera.main;
        }

        public void UpdatePositions(Board board)
        {
            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var piece = board.squares[BoardRepresentation.IndexFromCoord(file, rank)];
                    pieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
                    pieceRenderers[file, rank].transform.position = PositionFromCoord(file, rank, pieceDepth);
                }
            }
        }

        private void CreateBoardUI()
        {
            var squareShader = Shader.Find("Unlit/Color");
            squareRenderers = new MeshRenderer[8, 8];
            pieceRenderers = new SpriteRenderer[8, 8];

            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    square.transform.parent = transform;
                    square.transform.name = BoardRepresentation.SquareNameFromCoordinate(file, rank);
                    square.transform.position = PositionFromCoord(file, rank);
                    square.layer = LayerMask.NameToLayer("Tile");
                    var squareMaterial = new Material(squareShader);

                    squareRenderers[file, rank] = square.transform.gameObject.GetComponent<MeshRenderer>();
                    squareRenderers[file, rank].material = squareMaterial;

                    var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
                    pieceRenderer.transform.parent = square.transform;
                    pieceRenderer.transform.position = PositionFromCoord(file, rank, pieceDepth);
                    pieceRenderer.transform.localScale = Vector3.one * 0.25f;
                    pieceRenderers[file, rank] = pieceRenderer;
                }
            }

            ResetSquareColours();
        }

        public void ResetSquareColours()
        {
            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    SetSquareColour(new Coord(file, rank), boardTheme.lightSquares.normal,
                        boardTheme.darkSquares.normal);
                }
            }
            
            if (!lastMadeMove.IsInvalid)
                HighlightMove(lastMadeMove);
        }

        private void HighlightMove(Move move)
        {
            SetSquareColour(BoardRepresentation.CoordFromIndex(move.StartSquare), boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);
            SetSquareColour(BoardRepresentation.CoordFromIndex(move.TargetSquare), boardTheme.lightSquares.moveToHighlight, boardTheme.darkSquares.moveToHighlight);
        }

        private void SetSquareColour(Coord square, Color lightColour, Color darkColour)
        {
            squareRenderers[square.fileIndex, square.rankIndex].material.color =
                square.IsLightSquare() ? lightColour : darkColour;
        }

        public void SelectSquare(Coord coord)
        {
            SetSquareColour(coord, boardTheme.lightSquares.selected, boardTheme.darkSquares.selected);
        }

        public void DeselectSquare(Coord coord)
        {
            ResetSquareColours();
        }

        public void DragPiece(Coord coord, Vector2 mousePos)
        {
            pieceRenderers[coord.fileIndex, coord.rankIndex].transform.position = new Vector3(mousePos.x, mousePos.y, pieceDragDepth);
        }

        public void ResetPiecePosition(Coord coord)
        {
            var pos = PositionFromCoord(coord, pieceDepth);
            pieceRenderers[coord.fileIndex, coord.rankIndex].transform.position = pos;
        }

        public Vector3 PositionFromCoord(int file, int rank, float depth = 0)
        {
            return whiteIsBottom ? 
                new Vector3(-3.5f + file, -3.5f + rank, depth) : 
                new Vector3(-3.5f + 7 - file, 7 - rank - 3.5f, depth);
        }

        public Vector3 PositionFromCoord(Coord coord, float depth = 0)
        {
            return PositionFromCoord(coord.fileIndex, coord.rankIndex, depth);
        }

        public void HighlightLegalMoves(Board board, Coord fromSquare)
        {
            var moves = moveGenerator.GenerateMoves(board);

            foreach (var move in moves)
            {
                if (move.StartSquare != BoardRepresentation.IndexFromCoord(fromSquare)) continue;
                
                var coord = BoardRepresentation.CoordFromIndex(move.TargetSquare);
                SetSquareColour(coord, boardTheme.lightSquares.legal, boardTheme.darkSquares.legal);
            }
        }

        public void OnMoveMade(Board board, Move move, bool animate = false)
        {
            lastMadeMove = move;
            if (animate)
            {
                StartCoroutine(AnimateMove(move, board));
            }
            else
            {
                UpdatePositions(board);
                ResetSquareColours();
            }
        }

        private IEnumerator AnimateMove(Move move, Board board)
        {
            float t = 0;
            const float moveAnimDuration = 0.05f;
            var startCoord = BoardRepresentation.CoordFromIndex(move.StartSquare);
            var targetCoord = BoardRepresentation.CoordFromIndex(move.TargetSquare);
            var pieceTransform = pieceRenderers[startCoord.fileIndex, startCoord.rankIndex].transform;
            var startPos = PositionFromCoord(startCoord);
            var targetPos = PositionFromCoord(targetCoord);
            SetSquareColour(startCoord, boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);

            while (t <= 1)
            {
                yield return null;
                t += Time.deltaTime * 1 / moveAnimDuration;
                pieceTransform.position = Vector3.Lerp(startPos, targetPos, t);
            }
            UpdatePositions(board);
            ResetSquareColours();
            pieceTransform.position = startPos;
        }

        public void SetPerspective(bool isPlayerWhite)
        {
            whiteIsBottom = isPlayerWhite;
            ResetSquarePositions();
        }

        private void ResetSquarePositions()
        {
            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    squareRenderers[file, rank].transform.position = PositionFromCoord(file, rank);
                    pieceRenderers[file, rank].transform.position = PositionFromCoord(file, rank, pieceDepth);
                }
            }
            
            if (!lastMadeMove.IsInvalid)
                HighlightMove(lastMadeMove);
        }
    }
}
