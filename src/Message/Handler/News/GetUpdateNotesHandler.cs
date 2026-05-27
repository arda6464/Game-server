[PacketHandler(MessageType.GetUpdateNotesRequest)]
public static class GetUpdateNotesHandler
{
    public static void Handle(Session session)
    {
        var published = UpdateNotesManager.GetPublished();

        var response = new GetUpdateNotesResponsePacket();
        response.Updates.AddRange(published);

        session.Send(response);
        Logger.genellog($"[GetUpdateNotesHandler] {session.ID} → {published.Count} güncelleme notu gönderildi.");
    }
}
