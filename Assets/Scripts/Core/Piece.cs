namespace Chess.Core
{
    public class Piece
    {
        public const int None = 0;   // 00000
        public const int King = 1;   // 00001
        public const int Pawn = 2;   // 00010
        public const int Knight = 3; // 00011
        public const int Bishop = 5; // 00101
        public const int Rook = 6;   // 00110
        public const int Queen = 7;  // 00111

        public const int White = 8;  // 01000
        public const int Black = 16; // 10000

        private const int typeMask = 0b00111;
        private const int blackMask = 0b10000;
        private const int whiteMask = 0b01000;
        private const int colourMask = whiteMask | blackMask;

        public static bool IsColour (int piece, int colour) {
            return (piece & colourMask) == colour;
        }

        public static int GetColour (int piece) {
            return piece & colourMask;
        }

        public static int GetPieceType (int piece) {
            return piece & typeMask;
        }

        public static bool IsRookOrQueen (int piece) {
            return (piece & 0b110) == 0b110;
        }

        public static bool IsBishopOrQueen (int piece) {
            return (piece & 0b101) == 0b101;
        }

        public static bool IsSlidingPiece (int piece) {
            return (piece & 0b100) != 0;
        }
    }
}
