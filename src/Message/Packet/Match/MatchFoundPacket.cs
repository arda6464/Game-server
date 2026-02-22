using System.Collections.Generic;
using System.Numerics;

public class MatchFoundPacket : IPacket
{
    

    public List<Player> Players { get; set; } = new List<Player>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.MatchFound);
        buffer.WriteByte((byte)Players.Count);

        foreach(var p in Players)
        {
            buffer.WriteString(p.AccountId ?? "");
            buffer.WriteString(p.Username ?? "Unknown");
            buffer.WriteByte((byte)p.Health);
            buffer.WriteInt(p.SpawnIndex);
            
            // Send the connection token (useful for UDP setup on the client side)
            buffer.WriteInt(p.session?.ConnectionToken ?? 0);

            Console.WriteLine($" index: {p.SpawnIndex}");
        }
    }




    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
