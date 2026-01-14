public static class GetChatMessage
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(message);
        int id = buffer.ReadInt();
        byte ticketno = buffer.ReadByte();
        string content = buffer.ReadString();
        buffer.Dispose();
        var acccount = AccountCache.Load(session.AccountId);
        if (acccount == null) return;
        SupportTicketData ticketData = TicketManager.GetTicketDataByNo(session.AccountId, ticketno);
        if (ticketData == null)
        {
            // todoo
            return;
        }

        ticketData.ticketMessages.Add(new TicketMessage
        {
            Name = acccount.Username,
            Message = content,
            time = DateTime.Now
        });
        
       BotManager.istance.TicketSystem.SendTicketMessage(session.AccountId, content,ticketData.ID);
    }
}