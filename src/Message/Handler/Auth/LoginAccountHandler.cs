[PacketHandler(MessageType.AccountLogin)]
public static class LoginAccountHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        
        var request = new LoginAccountPacket();
        request.Deserialize(read);
        
        string email = request.Email;
        string password = request.Password;
        read.Dispose();
        
        var account = AccountManager.FindAccountByEmail(email);
        if(account == null)
        {
            Console.WriteLine("böyle bir hesap bulunamadı");
            return;
        }
        if (account.Password == password)
        {
            
            string code = VerificationCodeManager.GenerateCode();
            VerificationCodeManager.SaveCode(email, code);
            EmailServiceSync.SendVerificationCode(email, code);
            VerifyManager.CreateData(session.ID, new VerifyManager.VerificationData
            {
                Email = email,
                 Type = VerificationType.Login
            });
            Console.WriteLine($"{email} adresine doğrulama kodu gönderildi: {code}");
            
            session.Send(new SendVerifyCodePacket());
        }
        else
        {
                  // todo
        }
        
        

    }
}
