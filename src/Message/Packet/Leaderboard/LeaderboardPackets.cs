using System.Collections.Generic;

[PacketHandler(MessageType.LeaderboardRequest)]
public class LeaderboardRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Empty
    }
}


public class LeaderboardResponsePacket : IPacket
{
    

    public List<AccountData> Players { get; set; } = new List<AccountData>();
    public int PlayerRankIndex { get; set; }
    public int PlayerTrophy { get; set; }
    public string PlayerCountry {get;set;}

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.LeaderboardResponse);

        // 
        buffer.WriteVarString(SeasonManager.Config.SeasonName);
        buffer.WriteVarLong(DateTimeHelper.ToUnixSeconds(SeasonManager.Config.EndTimeUtc));

        buffer.WriteVarInt(Players.Count);
        foreach (var player in Players)
        {
            buffer.WriteVarString(player.Username);
            buffer.WriteVarInt(player.ID); 
            buffer.WriteVarString(player.CountryCode);
            buffer.WriteVarString(player.ClubName ?? " ");
            buffer.WriteVarInt(player.Trophy);
            buffer.WriteVarInt(player.Avatarid);
            buffer.WriteVarInt(player.Namecolorid);
            buffer.WriteVarInt(player.Premium);
        }
        buffer.WriteVarInt(PlayerRankIndex);
        buffer.WriteVarInt(PlayerTrophy);
        buffer.WriteVarString(PlayerCountry);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
