public class LootSpawnedPacket : IPacket
{
    public int LootId { get; set; }
    public int Type { get; set; }
    public int DataId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.LootSpawned);
        buffer.WriteVarInt(LootId);
        buffer.WriteVarInt(Type);
        buffer.WriteVarInt(DataId);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
    }
    public void Deserialize(ByteBuffer buffer) { }
}