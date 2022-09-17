using Chess.Core;
using UnityEngine;

namespace Chess.UI
{
    [CreateAssetMenu (menuName = "Theme/Pieces")]
    public class PieceTheme : ScriptableObject
    {
        public PieceSprites whitePieces;
        public PieceSprites blackPieces;

        public Sprite GetPieceSprite(int piece)
        {
            var sprites = Piece.IsColour(piece, Piece.White) ? whitePieces : blackPieces;
            var type = Piece.GetPieceType(piece);
            switch (type)
            {
                case Piece.Pawn:
                    return sprites.pawn;
                case Piece.Rook:
                    return sprites.rook;
                case Piece.Knight:
                    return sprites.knight;
                case Piece.Bishop:
                    return sprites.bishop;
                case Piece.Queen:
                    return sprites.queen;
                case Piece.King:
                    return sprites.king;
                default:
                    if (piece != 0)
                    {
                        Debug.Log(piece);
                    }
                    return null;
            }
        }
        
        [System.Serializable]
        public class PieceSprites
        {
            public Sprite pawn, rook, knight, bishop, queen, king;

            public Sprite this[int i]
            {
                get
                {
                    return new[] { pawn, rook, knight, bishop, queen, king }[i];
                }
            }
        }
    }
}
