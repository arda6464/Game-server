using System;

[PacketHandler(MessageType.SupportMessageSend)]
public static class GetChatMessage
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(message);
        
        var request = new SupportSendMessageRequestPacket();
        request.Deserialize(buffer);
        
        int ticketno = request.TicketNo;
        string content = request.Content;
        buffer.Dispose();

        if (session.Account == null) return;
        var account = session.Account;

        SupportTicketData ticketData = TicketManager.GetTicketDataByNo(session.ID, ticketno);
        if (ticketData == null) return;

        ticketData.ticketMessages.Add(new TicketMessage
        {
            Name = account.Username,
            Message = content,
            time = DateTime.Now
        });
        
        BotManager.istance.TicketSystem.SendTicketMessage(session.ID, content, ticketData.ID);
    }
}
