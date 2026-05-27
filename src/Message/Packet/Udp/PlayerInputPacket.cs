using System.Numerics;

public struct PlayerInputPacket : IPacket
{
    public PlayerInputPacket()
    {
    }

    public int SequenceNumber { get; set; }
    public uint Tick { get; set; } // Olayın gerçekleştiği zaman damgası
    public float InputX { get; set; } = 0;
    public float InputY { get; set; }=0;

    public int ConnectionToken { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        
    }

    public void Deserialize(ByteBuffer buffer)
    {
       byte inputbyte = buffer.ReadByte();

       if((inputbyte & 1) != 0) InputX = 1;
       else if ((inputbyte & 2) != 0) InputX = -1;

       if ((inputbyte & 4) != 0) InputY = 1;
       else if ((inputbyte & 8) != 0) InputY = -1;
       
       Tick = (uint)buffer.ReadVarInt();
    }
}
