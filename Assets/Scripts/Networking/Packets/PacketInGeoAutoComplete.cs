namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.GEOAUTOCOMPLETE)]
    public class PacketInGeoAutoComplete : Packet {
        public string Response { get; private set; }

        public PacketInGeoAutoComplete() : base(PacketNature.In, PacketType.GEOAUTOCOMPLETE) {
        }

        public override void Read(PacketDataStream stream) {
            Response = stream.ReadString();
        }
    }
}