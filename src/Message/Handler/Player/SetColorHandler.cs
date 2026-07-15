using System;

[PacketHandler(MessageType.ChangeNameColorRequest)]
public class SetColorHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        Console.WriteLine("SetCOLOR");
        var request = data.DeserializePacket<SetNameColorRequestPacket>();

        int Id = request.ColorId;

        // Color ID validasyonu (1-15 arası)
        if (Id < 1 || Id > 15)
        {
            Logger.errorslog($"[SetColor] Geçersiz color ID: {Id} from {session.ID}");
            return;
        }

        session.Logic.SetColorHandler(Id);
    }
}
