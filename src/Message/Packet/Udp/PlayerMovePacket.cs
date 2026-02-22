using System.Numerics;

public class PlayerMovePacket : IPacket
{
    public ushort SequenceNumber { get; set; }
    public string AccountId { get; set; } // Server broadcasting to others
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }


    // Client -> Server için Constructor
    public PlayerMovePacket() {}

    public void Serialize(ByteBuffer buffer)
    {
        // 1. UDP Header (Flags + Sequence)
        buffer.WriteByte((byte)Network.UdpPacketFlags.None); // Unreliable
        buffer.WriteUShort(SequenceNumber);


        // 2. Payload
        buffer.WriteShort((short)MessageType.UdpMove);
        buffer.WriteString(AccountId);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
        buffer.WriteFloat(Rotation);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        // Header (Flags + SequenceNumber) was already read by UdpServer
        AccountId = buffer.ReadString();

        X = buffer.ReadFloat();
        Y = buffer.ReadFloat();
        Z = buffer.ReadFloat();
        Rotation = buffer.ReadFloat();
    }


}
