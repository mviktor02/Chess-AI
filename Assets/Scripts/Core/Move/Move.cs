namespace Chess.Core
{
    /// <summary>
    /// Moves are stored as 16-bit numbers.
    /// bits 0-5: from square (0-63)
    /// bits 6-11: to square (0-63)
    /// bits 12-15: flag
    /// </summary>
    public readonly struct Move
    {
        public readonly struct Flag
        {
            public const int None = 0;
            public const int EnPassant = 1;
            public const int Castling = 2;
            public const int PromoteToQueen = 3;
            public const int PromoteToKnight = 4;
            public const int PromoteToRook = 5;
            public const int PromoteToBishop = 6;
            public const int PawnTwoForward = 7;
        }
        
        private readonly ushort moveValue;
        
        private const ushort startSquareMask = 0b0000000000111111;
        private const ushort targetSquareMask = 0b0000111111000000;
        private const ushort flagMask = 0b1111000000000000;
        
        public Move (ushort moveValue) {
            this.moveValue = moveValue;
        }

        public Move (int startSquare, int targetSquare) {
            moveValue = (ushort) (startSquare | targetSquare << 6);
        }

        public Move (int startSquare, int targetSquare, int flag) {
            moveValue = (ushort) (startSquare | targetSquare << 6 | flag << 12);
        }

        public int StartSquare => moveValue & startSquareMask;

        public int TargetSquare => (moveValue & targetSquareMask) >> 6;

        public bool IsPromotion => MoveFlag is Flag.PromoteToQueen or Flag.PromoteToRook or Flag.PromoteToKnight or Flag.PromoteToBishop;

        public int MoveFlag => moveValue >> 12;

        public int PromotionPieceType {
            get
            {
                return MoveFlag switch
                {
                    Flag.PromoteToRook => Piece.Rook,
                    Flag.PromoteToKnight => Piece.Knight,
                    Flag.PromoteToBishop => Piece.Bishop,
                    Flag.PromoteToQueen => Piece.Queen,
                    _ => Piece.None
                };
            }
        }

        public static Move InvalidMove => new Move (0);

        public static bool IsSameMove(Move a, Move b) {
            return a.moveValue == b.moveValue;
        }

        public ushort Value => moveValue;

        public bool IsInvalid => moveValue == 0;

        public string Name => 
            $"{BoardRepresentation.SquareNameFromIndex(StartSquare)}-{BoardRepresentation.SquareNameFromIndex(TargetSquare)}";
    }
}