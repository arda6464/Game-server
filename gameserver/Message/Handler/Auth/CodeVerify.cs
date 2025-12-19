public static class CodeVerify
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message);

        int packetid = read.ReadInt();

        int code = read.ReadInt();


        var data = VerifyManager.GetData(session.AccountId);
        bool isverify = VerificationCodeManager.VerifyCode(data.Email, code.ToString());

        if (!isverify) return; // todo
        switch (data.Type)
        {
            case VerificationType.Create:
                CrateAccount(session,session.AccountId,data.Email,data.Password);
                break;
            case VerificationType.Login:
                LoginAccount(session, data.Email);
                break;
            case VerificationType.ForgotPassword:
                break;
        }

    }
    private static void CrateAccount(Session session, string acccountId, string email, string password)
    {
        var acccount = AccountCache.Load(acccountId);
        if (acccount == null) return;

        acccount.Email = email;
        acccount.Password = password;
        Console.WriteLine($"epostaya kayıt olundu!: eposta: {acccount.Email} password: {acccount.Password}");
        Notfication notfication = new Notfication
        {
            Id = 12,
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

        LoginOK.Handle(session, account.Token, account.AccountId);
    }

}