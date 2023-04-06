using System;
using Chess.Core.AI;

namespace Chess.Core
{
    [Serializable]
    public class PlayerSettings
    {
        public GameManager.PlayerType whitePlayer;
        public AISettings whiteAiSettings;
        public GameManager.PlayerType blackPlayer;
        public AISettings blackAiSettings;
    }
}