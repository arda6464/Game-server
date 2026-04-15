[PacketHandler(MessageType.SignAccount)]
public class CreateAccountPacket : IPacket
{
    public string Email { get; set; }
    public string Password { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Email = buffer.ReadVarString();
        Password = buffer.ReadVarString();
    }
}
