namespace MRK.Networking.Packets {
    public interface IMRKNetworkSerializable<T> where T : new() {
        public void Write(PacketDataStream stream);
        public void Read(PacketDataStream stream);
    }
}
