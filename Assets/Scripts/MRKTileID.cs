using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK {
    public class MRKTileID {
        public int Z { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public MRKTileID(int z, int x, int y) {
            Z = z;
            X = x;
            Y = y;
        }

        public override string ToString() {
            return $"{Z} / {X} / {Y}";
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(obj, null) || !(obj is MRKTileID))
                return false;

            MRKTileID id = (MRKTileID)obj;
            return id.X == X && id.Y == Y && id.Z == Z;
        }

        public static bool operator ==(MRKTileID left, MRKTileID right) {
            bool lnull = ReferenceEquals(left, null);
            bool rnull = ReferenceEquals(right, null);

            if (rnull && lnull)
                return true;

            if (lnull || rnull)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(MRKTileID left, MRKTileID right) {
            bool lnull = ReferenceEquals(left, null);
            bool rnull = ReferenceEquals(right, null);

            if (rnull && lnull)
                return false;

            if (lnull || rnull)
                return true;

            return !left.Equals(right);
        }

        public override int GetHashCode() {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();

            return hash;
        }
    }
}
