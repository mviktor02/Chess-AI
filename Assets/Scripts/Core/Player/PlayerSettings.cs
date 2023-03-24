using System;
using UnityEngine;

namespace Chess.Core
{
    [Serializable]
    public class PlayerSettings
    {
        public GameManager.PlayerType whitePlayer;
        public GameManager.PlayerType blackPlayer;
    }
}