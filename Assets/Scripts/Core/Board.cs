namespace Chess.Core
{
    // TODO
    public class Board
    {
        public const int WhiteIndex = 0;
        public const int BlackIndex = 1;

        // storing pieces : colour | type
        public int[] squares;

        public bool isWhitesTurn;
        public int colourToMove;
        public int colourToMoveIndex;
        public int opponentColour;

        public int plyCounter;
        public int fiftyMoveCounter;
        
        public int[] kingSquareIndexes;

        public PieceList[] rooks;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] knights;
        public PieceList[] pawns;

        private PieceList[] pieceLists;

        private PieceList GetPieceList(int pieceType, int colourIndex)
        {
            return pieceLists[colourIndex * 8 + pieceType];
        }

        public void MakeMove(Move move)
        {
            
        }

        public void UnmakeMove(Move move)
        {
            
        }

        private void InitializeBoard()
        {
            squares = new int[64];
            kingSquareIndexes = new int[2];

            plyCounter = 0;
            fiftyMoveCounter = 0;

            knights = new PieceList[] { new(10), new(10) };
            pawns = new PieceList[] { new(8), new(8) };
            rooks = new PieceList[] { new(10), new(10) };
            bishops = new PieceList[] { new(10), new(10) };
            queens = new PieceList[] { new(9), new(9) };
            var emptyList = new PieceList(0);
            pieceLists = new[] {
                emptyList,
                emptyList,
                pawns[WhiteIndex],
                knights[WhiteIndex],
                emptyList,
                bishops[WhiteIndex],
                rooks[WhiteIndex],
                queens[WhiteIndex],
                emptyList,
                emptyList,
                pawns[BlackIndex],
                knights[BlackIndex],
                emptyList,
                bishops[BlackIndex],
                rooks[BlackIndex],
                queens[BlackIndex],
            };
        }

        public void LoadStartPosition()
        {
            LoadPosition(FenUtility.StartFen);
        }

        public void LoadPosition(string fen)
        {
            InitializeBoard();
            // ...
        }

    }
}