using System;

[PacketHandler(MessageType.SetAvatarRequest)]
public class SetAvatarHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        Console.WriteLine("Set Avatar");
        var request = data.DeserializePacket<SetAvatarRequestPacket>();

        int Id = request.AvatarId;

        // Avatar ID validasyonu (1-10 arası)
        if (Id < 1 || Id > 10)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.InvalidAvatar);
            return;
        }

        session.Logic.SetAvatarHandler(Id);
    }
}
