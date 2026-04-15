using System.Collections.Generic;
using System.Numerics;

public class MatchFoundPacket : IPacket
{


    public List<Player> Players { get; set; } = new List<Player>();
    public uint Tick { get; set; } // Başlangıç Tick'i (Client senkronizasyonu için)

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.MatchFound);
        buffer.WriteVarInt((int)Players.Count);
          buffer.WriteUInt(Tick); // Client bu değeri alıp kendi sayacını başlatacak

        foreach (var p in Players)
        {
            buffer.WriteVarInt(p.ID); // Yeni eklenen Internal ID
            buffer.WriteVarString(p.AccountId ?? "");
            buffer.WriteVarString(p.Username ?? "Unknown");
            buffer.WriteVarInt(p.Health);
            buffer.WriteFloat(p.Position.X);
            buffer.WriteFloat(p.Position.Y);
            buffer.WriteFloat(p.Position.Z);
        }
    }




    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
