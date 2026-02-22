public class GetClubMessagePacket : IPacket
{
    public ClubMessage Message { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.GetClubMessage);
        buffer.WriteInt(Message.MessageId); 
        buffer.WriteByte((byte)Message.messageFlags);
        
        if (Message.messageFlags == ClubMessageFlags.HasSystem)
        {
            buffer.WriteInt((int)Message.eventType);
            buffer.WriteString(Message.ActorName);
            buffer.WriteString(Message.ActorID);
        }
        else if (Message.messageFlags == ClubMessageFlags.None)
        {
            // Normal message logic if needed, but existing usage seems to focus on System/Event messages in Join/Leave
             buffer.WriteString(Message.SenderId);
             buffer.WriteString(Message.SenderName);
             buffer.WriteInt(Message.SenderAvatarID);
             buffer.WriteString("Üye");
             buffer.WriteString(Message.Content);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
