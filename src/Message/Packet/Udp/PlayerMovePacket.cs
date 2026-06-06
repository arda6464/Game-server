
public struct PlayerMovePacket : IPacket
{
    public int SequenceNumber { get; set; }
    public uint ServerTick { get; set; }
    public uint LastProcessedInputTick { get; set; }
    public int ID { get; set; } // Server broadcasting to others
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }


    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.Move);
        buffer.WriteUInt(ServerTick);
        buffer.WriteUInt(LastProcessedInputTick);
        buffer.WriteVarInt(ID);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
     //   buffer.WriteFloat(Rotation);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ServerTick = buffer.ReadUInt();
        LastProcessedInputTick = buffer.ReadUInt();
        ID = buffer.ReadVarInt();
        X = buffer.ReadFloat();
        Y = buffer.ReadFloat();
        Z = buffer.ReadFloat();
        Rotation = buffer.ReadFloat();
    }
}
