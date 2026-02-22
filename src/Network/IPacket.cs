public interface IPacket
{
    void Serialize(ByteBuffer buffer);
    void Deserialize(ByteBuffer buffer);
}
