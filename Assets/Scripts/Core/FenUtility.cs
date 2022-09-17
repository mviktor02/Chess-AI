namespace Chess.Core
{
    /// <summary>
    /// The easiest way to save and load gamestates in chess is to use the <a href="https://www.chessprogramming.org/Forsyth-Edwards_Notation">Forsyth-Edwards Notation</a>
    /// Format:
    /// piece_placement side_to_move castling_ability en_passant_target_square halfmove_clock fullmove_counter
    /// </summary>
    public class FenUtility
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        
        // TODO load and store position
    }
}