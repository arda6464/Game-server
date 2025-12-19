public static class CreateTicket
{
    public static void Handle(Session session)
    {
        var acccount = AccountCache.Load(session.AccountId);
        BotManager.istance.TicketSystem.CreateTicket(session.AccountId, new TicketData
        {
            Accountid = acccount.AccountId,
            session = session,
             UserName = acccount.Username
        });
    }
}