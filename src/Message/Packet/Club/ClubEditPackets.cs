
[PacketHandler(MessageType.ClubEditRequest)]
public class ClubEditRequestPacket : IPacket
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

public class ClubEditResponsePacket : IPacket
{
    public string? ClubName { get; set; }
    public string? ClubDescription { get; set; }
    public int ClubAvatarId { get; set; }
    public int State { get; set; }
    public string? Region { get; set; }
    public int AccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubEditResponse);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        buffer.WriteVarInt(ClubAvatarId);
        buffer.WriteVarInt(State);
        buffer.WriteVarString(Region);
        buffer.WriteVarInt(AccountId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
