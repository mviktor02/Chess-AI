namespace Chess.Core
{
    /// <summary>
    /// Bitboards are a general purpose, set-wise data-structure fitting in one 64-bit register.
    /// In this project, we'll use bitboards to validate moves
    /// </summary>
    public static class BitboardUtil
    {
        public static bool ContainsSquare (ulong bitboard, int square) {
            return ((bitboard >> square) & 1) != 0;
        }
    }
}