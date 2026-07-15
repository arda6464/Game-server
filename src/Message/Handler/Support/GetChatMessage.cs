using System;

[PacketHandler(MessageType.SupportMessageSend)]
public class GetChatMessage : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<SupportSendMessageRequestPacket>();

        int ticketno = request.TicketNo;
        string content = request.Content;

        if (session.Account == null)
            return;
        var account = session.Account;

        SupportTicketData ticketData = TicketManager.GetTicketDataByNo(session.ID, ticketno);
        if (ticketData == null)
            return;

        ticketData.ticketMessages.Add(
            new TicketMessage
            {
                Name = account.Username,
                Message = content,
                time = DateTime.Now,
            }
        );

        BotManager.istance.TicketSystem.SendTicketMessage(session.ID, content, ticketData.ID);
    }
}
