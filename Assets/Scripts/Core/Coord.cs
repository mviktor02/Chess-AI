using System;
using System.Runtime.CompilerServices;

namespace Chess.Core
{
    public struct Coord : IComparable<Coord> {
        public readonly int fileIndex;
        public readonly int rankIndex;

        public Coord (int fileIndex, int rankIndex) {
            this.fileIndex = fileIndex;
            this.rankIndex = rankIndex;
        }

        public bool IsLightSquare () {
            return (fileIndex + rankIndex) % 2 != 0;
        }

        public int CompareTo (Coord other) {
            return (fileIndex == other.fileIndex && rankIndex == other.rankIndex) ? 0 : 1;
        }

        public override bool Equals(object obj)
        {
            return obj is Coord other && other == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(fileIndex, rankIndex);
        }

        [MethodImpl((MethodImplOptions) 256)]
        public static bool operator == (Coord left, Coord right) =>
            left.fileIndex == right.fileIndex && left.rankIndex == right.rankIndex;

        [MethodImpl((MethodImplOptions) 256)]
        public static bool operator !=(Coord left, Coord right) => !(left == right);
    }
}