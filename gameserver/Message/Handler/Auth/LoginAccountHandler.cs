public static class LoginAccountHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int packetid = read.ReadInt();
        string eposta = read.ReadString();
        string password = read.ReadString();
        read.Dispose();
        
        ByteBuffer buffer = new ByteBuffer();
        var account = AccountManager.FindAccountByEmail(eposta);
        if(account == null)
        {
            Console.WriteLine("böyle bir hesap bulunamadı");
            return;
        }
        if (account.Password == password)
        {
            
            string code = VerificationCodeManager.GenerateCode();
            VerificationCodeManager.SaveCode(eposta, code);
            EmailServiceSync.SendVerificationCode(eposta, code);
            VerifyManager.CreateData(session.AccountId, new VerifyManager.VerificationData
            {
                Email = eposta,
                 Type = VerificationType.Login
            });
            Console.WriteLine($"{eposta} adresine doğrulama kodu gönderildi: {code}");
            buffer.WriteInt((int)MessageType.SendVerifyCode);
        }
        else
        {
                  // todo
        }
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);
        
        

    }
}