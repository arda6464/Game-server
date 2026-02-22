[PacketHandler(MessageType.NewFBNTokenRequest)]
public static class NewFBNTokenHandler
{
    public static void Handle(Session session, byte[] data)
    {
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadShort();
            string newtoken = read.ReadString();

            if (session.Account == null)
            {
                Console.WriteLine("HATA: FBN Token geldi ama session.Account NULL!");
                session.FBNToken = newtoken;
            }
            else
            {
                session.Account.FBNToken = newtoken;
                Console.WriteLine($"FBN Token kaydedildi: {newtoken} (AccountID: {session.Account.AccountId})");
            }
        }
    }
}