using System.Collections.Generic;

public class LeaderboardResponsePacket : IPacket
{
    public class PlayerInfo
    {
        public string Name { get; set; }
        public string AccountId { get; set; }
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
        buffer.WriteShort((short)MessageType.LeaderboardResponse);
        buffer.WriteInt(Players.Count);
        foreach (var player in Players)
        {
            buffer.WriteString(player.Name);
            buffer.WriteString(player.AccountId);
            buffer.WriteString(player.ClubName ?? " ");
            buffer.WriteInt(player.Trophy);
            buffer.WriteInt(player.AvatarId);
            buffer.WriteInt(player.NameColorId);
            buffer.WriteInt(player.Premium);
        }
        buffer.WriteInt(PlayerRankIndex);
        buffer.WriteInt(PlayerTrophy);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
