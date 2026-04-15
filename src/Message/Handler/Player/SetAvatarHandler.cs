using System;

[PacketHandler(MessageType.SetAvatarRequest)]
public static class SetAvatar
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("Set Avatar");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);

        var request = new SetAvatarRequestPacket();
        request.Deserialize(BUFFER);
        
        int Id = request.AvatarId;
        BUFFER.Dispose();
        
        // Avatar ID validasyonu (1-10 arası)
        if (Id < 1 || Id > 10)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidAvatar);
            return;
        }
        
        session.Logic.SetAvatar(Id);

    }
}
