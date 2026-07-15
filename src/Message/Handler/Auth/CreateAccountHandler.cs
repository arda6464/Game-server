[PacketHandler(MessageType.SignAccount)]
public class CreateAccountHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<CreateAccountPacket>();
        
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
        bool sendmail = EmailServer.SendVerificationCode(email, code);
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
