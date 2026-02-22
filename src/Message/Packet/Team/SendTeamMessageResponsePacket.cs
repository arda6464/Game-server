public class SendTeamMessageResponsePacket : IPacket
{
    public TeamMessageFlags Flags { get; set; }
    
    // System Message Fields
    public TeamEventType EventType { get; set; }
    // Shared: SenderName, SenderId

    // User Message Fields
    public int MessageId { get; set; }
    public string SenderAccountId { get; set; }
    public string SenderName { get; set; }
    public int SenderAvatarId { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.SendTeamMessageResponse);
        buffer.WriteByte((byte)Flags);

        if (Flags == TeamMessageFlags.HasSystem)
        {
            buffer.WriteInt((int)EventType);
            buffer.WriteString(SenderName);
            buffer.WriteString(SenderAccountId ?? "");
        }
        else
        {
            buffer.WriteInt(MessageId);
            buffer.WriteString(SenderAccountId);
            buffer.WriteString(SenderName);
            buffer.WriteInt(SenderAvatarId);
            buffer.WriteString(Role ?? ""); 
            buffer.WriteString(Content);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
