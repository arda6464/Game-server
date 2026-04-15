[PacketHandler(MessageType.ShowProfileRequest)]
public class ShowProfileRequestPacket : IPacket
{
   
    public int ID { get; set; }
   
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {  
            ID = buffer.ReadVarInt();
    }
}
