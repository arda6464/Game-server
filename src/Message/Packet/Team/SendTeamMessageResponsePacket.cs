public class SendTeamMessageResponsePacket : IPacket
{
    public TeamMessageFlags Flags { get; set; }
    
    // System Message Fields
    public TeamEventType EventType { get; set; }
    // Shared: SenderName, SenderId

    // User Message Fields
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; }
    public int SenderAvatarId { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.SendTeamMessageResponse);
        buffer.WriteByte((byte)Flags);

        if (Flags == TeamMessageFlags.HasSystem)
        {
            buffer.WriteVarInt((int)EventType);
            buffer.WriteVarString(SenderName);
            buffer.WriteVarInt(SenderId);
        }
        else
        {
            buffer.WriteVarInt(MessageId);
            buffer.WriteVarInt(SenderId);
            buffer.WriteVarString(SenderName);
            buffer.WriteVarInt(SenderAvatarId);
            buffer.WriteVarString(Role ?? ""); 
            buffer.WriteVarString(Content);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
