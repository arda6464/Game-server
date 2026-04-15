public class TeamInviteNotificationPacket : IPacket
{
    public string SenderName { get; set; }
    public int SenderId { get; set; }
    public int SenderAvatarId { get; set; }
    public int SenderTrophy { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.InviteToTeamRequest);
        buffer.WriteVarString(SenderName);
        buffer.WriteVarInt(SenderId);
        buffer.WriteVarInt(SenderAvatarId);
        buffer.WriteVarInt(SenderTrophy);
        buffer.WriteByte((byte)CurrentPlayers);
        buffer.WriteByte((byte)MaxPlayers);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
