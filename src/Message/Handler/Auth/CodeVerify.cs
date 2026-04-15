[PacketHandler(MessageType.VerifyCodeResponse)]
public static class CodeVerify
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message);

        var request = new VerifyCodeRequestPacket();
        request.Deserialize(read);

        int code = request.Code;


        var data = VerifyManager.GetData(session.ID);
        bool isverify = VerificationCodeManager.VerifyCode(data.Email, code.ToString());

        if (!isverify) return; // todo
        switch (data.Type)
        {
            case VerificationType.Create:
                CrateAccount(session,session.ID,data.Email,data.Password);
                break;
            case VerificationType.Login:
                LoginAccount(session, data.Email);
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
        Notfication notfication = new Notfication
        {
             type =  NotficationTypes.NotficationType.Inbox,
            Sender = "Sistem",
            Message = "Kayıt olduğun için teşekkürler!\n bu ödül senin için <3",
            rewardItemType = RewardItemType.RewardItemTypes.Gem,
            DonationCount = 300
        };
        NotficationSender.Send(session, notfication);
        acccount.inboxesNotfications.Add(notfication);

    }    
    private static void LoginAccount(Session session,string email)
    {
        var account = AccountManager.FindAccountByEmail(email);
        if (account == null) return; // todo

        LoginOK.Handle(session, account.Token, account.ID);
    }

}
