using System.Collections.Generic;


public class JoinTeamRequestPacket : IPacket
{
    public int TeamId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TeamId = buffer.ReadVarInt();
    }
}


public class JoinTeamResponsePacket : IPacket
{
    public int TeamId { get; set; }
    public List<TeamMessage> Messages { get; set; } = new List<TeamMessage>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.JoinTeamResponse);
        buffer.WriteVarInt(TeamId);
        
        buffer.WriteVarInt(Messages.Count);
        foreach(var teammessage in Messages)
        {
            buffer.WriteVarInt(teammessage.SenderId);
            buffer.WriteVarString(teammessage.SenderName);
            buffer.WriteVarInt(teammessage.SenderAvatarID);
            buffer.WriteVarString(""); // ?!
            buffer.WriteVarString(teammessage.Content);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
