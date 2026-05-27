using System.Collections.Generic;

[PacketHandler(MessageType.ClubCreateRequest)]
public class ClubCreateRequestPacket : IPacket
{
    public string? ClubName { get; set; }
    public string? ClubDescription { get; set; }
    public int AvatarId { get; set; }
    public int State { get; set; }
    public string? Region { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ClubName = buffer.ReadVarString();
        ClubDescription = buffer.ReadVarString();
        AvatarId = buffer.ReadVarInt();
        State = buffer.ReadVarInt();
        Region = buffer.ReadVarString();
    }
}


public class ClubCreateResponsePacket : IPacket
{
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? ClubDescription { get; set; }
    public int AvatarId { get; set; }
    public int State { get; set; }
    public string? Region { get; set; }
    public int TotalTrophies { get; set; }
    public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>();
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubCreateResponse);
        buffer.WriteVarInt(ClubId);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        buffer.WriteVarInt(TotalTrophies);
        buffer.WriteVarInt(AvatarId);
        buffer.WriteVarInt(State);
        buffer.WriteVarString(Region);

        buffer.WriteVarInt(Messages.Count);
        foreach (var message in Messages)
        {
            buffer.WriteVarInt(message.SenderId);
            buffer.WriteVarString(message.SenderName);
            buffer.WriteVarInt(message.SenderAvatarID);
            buffer.WriteVarInt((int)message.Role); 
            buffer.WriteVarString(message.Content);
        }

        buffer.WriteVarInt(Members.Count);
        foreach (var member in Members)
        {
            buffer.WriteVarInt(member.ID);
            buffer.WriteVarString(member.AccountName);
            buffer.WriteVarInt((int)member.Role);
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
