using System;

public class NewAccountCreateResponsePacket : IPacket
{
    public string Token { get; set; }
    public int ID { get; set; }
    public int ConnectionToken { get; set; }    

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.NewAccountCreateResponse);
        buffer.WriteVarString(Token);
        buffer.WriteVarInt(ID);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
