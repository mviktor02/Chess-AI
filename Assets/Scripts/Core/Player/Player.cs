using System;

namespace Chess.Core
{
    public abstract class Player
    {
        public event Action<Move> onMoveEvent;

        public abstract void Update();

        public abstract void NotifyTurnToMove();

        protected void MakeMove(Move move)
        {
            onMoveEvent?.Invoke(move);
        }
    }
}