[PacketHandler(MessageType.ChangeNameRequest)]
public static class ChangeNameHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        var request = new ChangeNameRequestPacket();
        request.Deserialize(read);
        
        string newname = request.NewName;
        read.Dispose();

        session.Logic.ChangeName(newname);
    }
}
