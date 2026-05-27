
public struct PlayerMovePacket : IPacket
{
    public int SequenceNumber { get; set; }
    public int ID { get; set; } // Server broadcasting to others
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }

    public uint ClientTick { get; set; } // Acknowledgement


    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.Move);
        buffer.WriteUInt(ClientTick);
        buffer.WriteVarInt(ID);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
     //   buffer.WriteFloat(Rotation);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
        ClientTick = buffer.ReadUInt();
        ID = buffer.ReadVarInt();
        X = buffer.ReadFloat();
        Y = buffer.ReadFloat();
        Z = buffer.ReadFloat();
        Rotation = buffer.ReadFloat();
    }
}
