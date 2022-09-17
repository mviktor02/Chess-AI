using System;
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

        private Coord currentHover = INVALID_COORD;
        private Camera camera;
        private MeshRenderer[,] squareRenderers;
        private SpriteRenderer[,] pieceRenderers;

        public const float pieceDepth = -0.1f;
        public const float pieceDragDepth = -0.2f;
        private static readonly Coord INVALID_COORD = new(-1, -1);
        
        private void Awake()
        {
            CreateBoardUI();
            camera = Camera.main;
        }

        private void Update()
        {
            if (!camera) return;

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var info, 100, LayerMask.GetMask("Tile")))
            {
                var hitPosition = LookupTileIndex(info.transform.gameObject);
                currentHover = hitPosition;
            }
        }

        private static Coord LookupTileIndex(Object hitInfo)
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

            return INVALID_COORD;
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
                    pieceRenderer.transform.localScale = Vector3.one * 100 / (2000 / 6f);
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

        public bool TryGetSquareUnderMouse(out Coord selectedCoord)
        {
            selectedCoord = currentHover;
            return selectedCoord.fileIndex is >= 0 and < 8 && selectedCoord.rankIndex is >= 0 and < 8;
        }

        public void HighlightLegalMoves(Board board, Coord fromSquare)
        {
            // TODO
        }
    }
}
