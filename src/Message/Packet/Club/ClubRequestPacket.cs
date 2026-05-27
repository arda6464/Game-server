
public class ClubRequestPacket : IPacket
{
    public int MessageID;
    public bool Isjoined;


    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        MessageID = buffer.ReadVarInt();
        Isjoined = buffer.ReadBool();
    }
}

/// <summary>
/// Server → Client: Belirli bir request mesajının durumunu güncelle
/// Client bu MessageId'yi chat listesinde bulup UI'ı günceller
/// </summary>
public class ClubRequestStateUpdatePacket : IPacket
{
    public int MessageId { get; set; }
    public int NewState { get; set; } // ClubRequestState (Accepted/Rejected)
    public string? ResponderName { get; set; } // Kabul/Red eden kişinin adı

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubRequestStateUpdate);
        buffer.WriteVarInt(MessageId);
        buffer.WriteVarInt(NewState);
        //buffer.WriteVarString(ResponderName);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
