using System;

[PacketHandler(MessageType.ChangeNameColorRequest)]
public static class SetNameColor
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("SetCOLOR");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);

        var request = new SetNameColorRequestPacket();
        request.Deserialize(BUFFER);

        int Id = request.ColorId;
        BUFFER.Dispose();
        
        // Color ID validasyonu (1-15 arası)
        if (Id < 1 || Id > 15)
        {
            Logger.errorslog($"[SetColor] Geçersiz color ID: {Id} from {session.ID}");
            return;
        }
        
        session.Logic.SetNameColor(Id);

    }
}
