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
                CrateAccount(session.AccountId,data.Email,data.Password);
                break;
            case VerificationType.Login:
                LoginAccount(session, data.Email);
                break;
            case VerificationType.ForgotPassword:
                break;
        }

    }
    private static void CrateAccount(string acccountId, string email, string password)
    {
        var acccount = AccountManager.LoadAccount(acccountId);
        if (acccount == null) return;

        acccount.Email = email;
        acccount.Password = password;
        Console.WriteLine($"epostaya kayıt olundu!: eposta: {acccount.Email} password: {acccount.Password}");
    }    
    private static void LoginAccount(Session session,string email)
    {
        var account = AccountManager.FindAccountByEmail(email);
        if (account == null) return; // todo

        LoginOK.Handle(session, account.Token, account.AccountId);
    }

}