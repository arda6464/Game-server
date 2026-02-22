using System.Numerics;

public class PlayerInputPacket : IPacket
{
    public ushort SequenceNumber { get; set; }
    public float InputX { get; set; }
    public float InputY { get; set; }
    public float Rotation { get; set; } // Karakterin baktığı yön (Joystick'ten bağımsız olabilir)
    public bool IsMoving { get; set; } // Hareket ediyor mu?

    public int ConnectionToken { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        // 1. UDP Header (Flags + Sequence + Token)
        buffer.WriteByte((byte)Network.UdpPacketFlags.None); // Unreliable
        buffer.WriteUShort(SequenceNumber);
        buffer.WriteInt(ConnectionToken);

        // 2. Payload
        buffer.WriteShort((short)MessageType.UdpInput);
        buffer.WriteFloat(InputX);
        buffer.WriteFloat(InputY);
        buffer.WriteFloat(Rotation);
        buffer.WriteBool(IsMoving);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Header (7 byte) UdpServer tarafından ayıklandı. Payload'dan devam ediyoruz.
        InputX = buffer.ReadFloat();
        InputY = buffer.ReadFloat();
        Rotation = buffer.ReadFloat();
        IsMoving = buffer.ReadBool();
    }

}
