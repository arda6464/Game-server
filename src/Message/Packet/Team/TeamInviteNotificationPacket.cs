public class TeamInviteNotificationPacket : IPacket
{
    public string SenderName { get; set; }
    public string SenderId { get; set; }
    public int SenderAvatarId { get; set; }
    public int SenderTrophy { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.InviteToTeamRequest);
        buffer.WriteString(SenderName);
        buffer.WriteString(SenderId);
        buffer.WriteInt(SenderAvatarId);
        buffer.WriteInt(SenderTrophy);
        buffer.WriteByte((byte)CurrentPlayers);
        buffer.WriteByte((byte)MaxPlayers);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
