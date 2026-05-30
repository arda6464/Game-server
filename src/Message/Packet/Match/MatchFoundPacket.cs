using System.Collections.Generic;

public class  MatchFoundPacket : IPacket
{


    public List<Player> Players { get; set; } = new List<Player>();
    public List<LootItem> Loots { get; set; } = new List<LootItem>();
    public uint Tick { get; set; } // Başlangıç Tick'i (Client senkronizasyonu için)

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.MatchFound);
        buffer.WriteVarInt((int)Players.Count);
          buffer.WriteUInt(Tick); // Client bu değeri alıp kendi sayacını başlatacak

        foreach (var p in Players)
        {
            buffer.WriteVarInt(p.ID); // Yeni eklenen Internal ID
            buffer.WriteVarString(p.Username ?? "Unknown");
            buffer.WriteVarInt(p.Health);
            buffer.WriteFloat(p.Position.x);
            buffer.WriteFloat(p.Position.y);
            buffer.WriteFloat(p.Position.z);
        }

        buffer.WriteVarInt(Loots.Count);
        foreach (var loot in Loots)
        {
            buffer.WriteVarInt(loot.LootId);
            buffer.WriteVarInt((int)loot.Type);
            buffer.WriteVarInt(loot.DataId);
            buffer.WriteFloat(loot.Position.x);
            buffer.WriteFloat(loot.Position.y);
            buffer.WriteFloat(loot.Position.z);
        }
    }




    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
