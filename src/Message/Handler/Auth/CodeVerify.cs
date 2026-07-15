[PacketHandler(MessageType.VerifyCodeResponse)]
public class CodeVerify : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<VerifyCodeRequestPacket>();

        int code = request.Code;


        var verifyData = VerifyManager.GetData(session.ID);
        bool isverify = VerificationCodeManager.VerifyCode(verifyData.Email, code.ToString());

        if (!isverify) return; // todo
        switch (verifyData.Type)
        {
            case VerificationType.Create:
                CrateAccount(session,session.ID,verifyData.Email,verifyData.Password);
                break;
            case VerificationType.Login:
                LoginAccount(session, verifyData.Email);
                break;
            case VerificationType.ForgotPassword:
                break;
        }

    }
    private static void CrateAccount(Session session, int acccountId, string email, string password)
    {
        var acccount = AccountCache.Load(acccountId);
        if (acccount == null) return;

        acccount.Email = email;
        acccount.Password = password;
        Console.WriteLine($"epostaya kayıt olundu!: eposta: {acccount.Email} password: {acccount.Password}");
        Notification Notification = new Notification
        {
             type =  NotificationTypes.NotificationType.Inbox,
            Sender = "Sistem",
            Message = "Kayıt olduğun için teşekkürler!\n bu ödül senin için <3",
            Rewards = new List<RewardItem>
            {
                new RewardItem { Type =  ItemType.Gems, Count = 300 }
            }
        };
        NotificationSender.Send(session, Notification);
        acccount.inboxesNotifications.Add(Notification);

    }    
    private static void LoginAccount(Session session,string email)
    {
        var account = AccountManager.FindAccountByEmail(email);
        if (account == null) return; // todo

        LoginOK.Handle(session, account.Token, account.ID);
    }

}
