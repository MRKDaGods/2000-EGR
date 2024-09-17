using UnityEngine;

namespace MRK.Maps
{
    public class TileID
    {
        private static TileID _topMost;

        public int Z
        {
            get; private set;
        }

        public int X
        {
            get; private set;
        }

        public int Y
        {
            get; private set;
        }

        public int Magnitude
        {
            get; private set;
        }

        public bool Stationary
        {
            get; private set;
        }

        public static TileID TopMost
        {
            get
            {
                return _topMost ??= new TileID(0, 0, 0);
            }
        }

        public TileID(int z, int x, int y, bool stationary = false)
        {
            Z = z;
            X = x;
            Y = y;

            Magnitude = x * x + y * y;
            Stationary = stationary;
        }

        public override string ToString()
        {
            return $"{Z} / {X} / {Y}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null) || !(obj is TileID))
            {
                return false;
            }

            TileID id = (TileID)obj;
            return id.X == X && id.Y == Y && id.Z == Z;
        }

        public static bool operator ==(TileID left, TileID right)
        {
            bool lnull = left is null;
            bool rnull = right is null;

            return (rnull && lnull) || (!lnull && !rnull && left.Equals(right));
        }

        public static bool operator !=(TileID left, TileID right)
        {
            bool lnull = left is null;
            bool rnull = right is null;

            return (!rnull || !lnull) && (lnull || rnull || !left.Equals(right));
        }

        public override int GetHashCode()
        {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();

            return hash;
        }

        public Vector3Int ToVector()
        {
            return new Vector3Int(X, Y, Z);
        }
    }
}
