[PacketHandler(MessageType.AccountLogin)]
public class LoginAccountHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<LoginAccountPacket>();
        
        string email = request.Email;
        string password = request.Password;
        
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
            EmailServer.SendVerificationCode(email, code);
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
