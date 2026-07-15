[PacketHandler(MessageType.ChangeNameRequest)]
public class ChangeNameHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<ChangeNameRequestPacket>();

        string newname = request.NewName;
       

        session.Logic.ChangeName(newname);
    }
}
