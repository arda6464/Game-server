[PacketHandler(MessageType.SupportMessageSend)]
public static class GetChatMessage
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(message);
        int _ = buffer.ReadShort();
        
        var request = new SupportSendMessageRequestPacket();
        request.Deserialize(buffer);
        
        byte ticketno = request.TicketNo;
        string content = request.Content;
        buffer.Dispose();
        if (session.Account == null) return;
        var acccount = session.Account;
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