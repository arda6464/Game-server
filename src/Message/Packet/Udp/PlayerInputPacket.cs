using System.Numerics;

public class PlayerInputPacket : IPacket
{
    public int SequenceNumber { get; set; }
    public int Tick { get; set; } // Olayın gerçekleştiği zaman damgası
    public float InputX { get; set; } = 0;
    public float InputY { get; set; }=0;

    public int ConnectionToken { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        
    }

    public void Deserialize(ByteBuffer buffer)
    {
       Tick = buffer.ReadInt(); // Client'ın bu input'u ürettiği tick (Lag compensation ve Prediction için)
       byte inputbyte = buffer.ReadByte();

       if((inputbyte &1) != 0) InputX = 1;
       else if ((inputbyte & 2) != 0) InputX = -1;

       // YUKARI (Değeri 4 olan lamba yanıyor mu?)
       if ((inputbyte & 4) != 0) InputY = 1;
       // AŞAĞI (Değeri 8 olan lamba yanıyor mu?)
       else if ((inputbyte & 8) != 0) InputY = -1;
    }
}
