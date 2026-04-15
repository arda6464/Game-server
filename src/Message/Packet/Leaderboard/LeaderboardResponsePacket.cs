using System.Collections.Generic;

public class LeaderboardResponsePacket : IPacket
{
    public class PlayerInfo
    {
        public string Name { get; set; }
    
        public int ID { get; set; } // Yeni sayısal ID
        public string ClubName { get; set; }
        public int Trophy { get; set; }
        public int AvatarId { get; set; }
        public int NameColorId { get; set; }
        public int Premium { get; set; }
    }

    public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();
    public int PlayerRankIndex { get; set; }
    public int PlayerTrophy { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.LeaderboardResponse);
        buffer.WriteVarInt(Players.Count);
        foreach (var player in Players)
        {
            buffer.WriteVarString(player.Name);
            buffer.WriteVarInt(player.ID); // ID önce gönderilebilir veya sona eklenebilir
            buffer.WriteVarString(player.ClubName ?? " ");
            buffer.WriteVarInt(player.Trophy);
            buffer.WriteVarInt(player.AvatarId);
            buffer.WriteVarInt(player.NameColorId);
            buffer.WriteVarInt(player.Premium);
        }
        buffer.WriteVarInt(PlayerRankIndex);
        buffer.WriteVarInt(PlayerTrophy);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
