public static class CreateAccountHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message);
        int packetid = read.ReadInt();
        string email = read.ReadString();
        string password = read.ReadString();

        Console.WriteLine(email);
        var acccount = AccountManager.LoadAccount(session.AccountId);
        if (acccount == null)
        {
            Console.WriteLine("account null konum: createaccount");
            return;
        } 

        bool isfree = AccountManager.CheckMail(email);
        if (isfree)
        {
            Console.WriteLine("test");
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
        VerifyManager.CreateData(session.AccountId, new VerifyManager.VerificationData
        {
            Type = VerificationType.Create,
            Email = email,
            Password = password
        });
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.SendVerifyCode);
        session.Send(buffer.ToArray());
        buffer.Dispose();
        
     }
}