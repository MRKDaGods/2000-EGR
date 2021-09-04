using MRK.Networking.Packets;
using UnityEngine;

namespace MRK {
    public class MRKTileID : IMRKNetworkSerializable<MRKTileID> {
        public int Z { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Magnitude { get; private set; }
        public bool Stationary { get; private set; }

        public MRKTileID() {
            Z = X = Y = 0;
        }

        public MRKTileID(int z, int x, int y, bool stationary = false) {
            Z = z;
            X = x;
            Y = y;

            Magnitude = x * x + y * y;
            Stationary = stationary;
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

        public Vector3Int ToVector() {
            return new Vector3Int(X, Y, Z);
        }

        public void Write(PacketDataStream stream) {
            stream.WriteInt32(Z);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
        }

        public void Read(PacketDataStream stream) {
            Z = stream.ReadInt32();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();

            Magnitude = X * X + Y * Y;
        }
    }
}
