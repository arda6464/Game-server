[PacketHandler(MessageType.SignAccount)]
public static class CreateAccountHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message);
        
        var request = new CreateAccountPacket();
        request.Deserialize(read);
        
        string email = request.Email;
        string password = request.Password;

        Console.WriteLine(email);
        var acccount = session.Account;
        if (acccount == null)
        {
            Console.WriteLine("account null konum: createaccount");
            return;
        } 

        bool isfree = AccountManager.CheckMail(email);
        if (isfree)
        {
            Console.WriteLine("test");
            MessageCodeManager.Send(session, MessageCodeManager.Message.EmailAlreadyUsed);
            return; // todo send message

        } 

        string code = VerificationCodeManager.GenerateCode();
        VerificationCodeManager.SaveCode(email, code);
        bool sendmail = EmailServiceSync.SendVerificationCode(email, code);
       if(!sendmail)
        {
            Console.WriteLine("mail gönderilmemiş?");
            return;
        }
        VerifyManager.CreateData(session.ID, new VerifyManager.VerificationData
        {
            Type = VerificationType.Create,
            Email = email,
            Password = password
        });
        
        session.Send(new SendVerifyCodePacket());
        
     }
}
