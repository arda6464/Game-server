using System;

[PacketHandler(MessageType.ShowProfileRequest)]
public class ShowProfileHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<ShowProfileRequestPacket>();

        AccountData? account = null;

        account = AccountCache.Load(request.ID);

        if (account == null)
            return;

        var response = new ShowProfileResponsePacket { account = account };
        session.Send(response);
    }
}
