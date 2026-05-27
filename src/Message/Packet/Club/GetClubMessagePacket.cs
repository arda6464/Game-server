public class GetClubMessagePacket : IPacket
{
    public ClubMessage Message { get; set; }
    public ClubRole Role { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetClubMessage);
       
        buffer.WriteVarInt((int)Message.messageFlags);
        Console.WriteLine("flag:" + Message.messageFlags);

        if (Message.messageFlags == ClubMessageFlags.HasSystem)
        {
            buffer.WriteVarInt((int)Message.eventType);
            buffer.WriteVarString(Message.ActorName);
            buffer.WriteVarInt(Message.SenderId); 
        }
        else if (Message.messageFlags == ClubMessageFlags.None)
        {
            buffer.WriteVarInt(Message.MessageId);
            buffer.WriteVarInt(Message.SenderId); 
            buffer.WriteVarString(Message.SenderName);
            buffer.WriteVarInt(Message.SenderAvatarID);
            buffer.WriteVarInt((int)Role);
            buffer.WriteVarString(Message.Content);
        }
        else if (Message.messageFlags == ClubMessageFlags.Request)
        {
            buffer.WriteVarInt(Message.MessageId);
            buffer.WriteVarInt(Message.ActorID);
            buffer.WriteVarString(Message.ActorName);
            buffer.WriteVarString(Message.Content);
            buffer.WriteVarInt(Message.SenderAvatarID);
            buffer.WriteVarInt((int)Message.RequestState);
        }
        else
        {
            buffer.WriteVarInt((int)Message.eventType);
            buffer.WriteVarString(Message.ActorName); // atan kişi
            buffer.WriteVarString(Message.TargetName);
            buffer.WriteVarInt(Message.TargetID);
        
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
