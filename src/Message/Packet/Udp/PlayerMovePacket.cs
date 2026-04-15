using System.Numerics;

public class PlayerMovePacket : IPacket
{
    public int SequenceNumber { get; set; }
    public int ID { get; set; } // Server broadcasting to others
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    public uint Tick { get; set; }

    public PlayerMovePacket() { }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.Move);
        buffer.WriteUInt(Tick);
        buffer.WriteVarInt(ID);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
        buffer.WriteFloat(Rotation);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Tick = buffer.ReadUInt();
        ID = buffer.ReadVarInt();
        X = buffer.ReadFloat();
        Y = buffer.ReadFloat();
        Z = buffer.ReadFloat();
        Rotation = buffer.ReadFloat();
    }
}
