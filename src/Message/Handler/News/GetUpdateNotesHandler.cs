[PacketHandler(MessageType.GetUpdateNotesRequest)]
public class GetUpdateNotesHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var published = UpdateNotesManager.GetPublished();

        var response = new GetUpdateNotesResponsePacket();
        response.Updates.AddRange(published);

        session.Send(response);
        Logger.genellog(
            $"[GetUpdateNotesHandler] {session.ID} → {published.Count} güncelleme notu gönderildi."
        );
    }
}
