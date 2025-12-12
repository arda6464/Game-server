public static class GetChatMessage
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(message);
        int id = buffer.ReadInt();
        string content = buffer.ReadString();
        buffer.Dispose();

        BotManager.istance.TicketSystem.SendTicketMessage(session.AccountId,content);
    }
}