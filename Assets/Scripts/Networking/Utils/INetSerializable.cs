namespace MRK.Networking.Utils
{
    public interface INetSerializable
    {
        void Serialize(NetDataWriter writer);
        void Deserialize(NetDataReader reader);
    }
}
