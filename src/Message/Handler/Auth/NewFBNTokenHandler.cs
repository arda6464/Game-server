[PacketHandler(MessageType.NewFBNTokenRequest)]
public class NewFBNTokenHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        using (ByteBuffer read = ByteBufferPool.Get())
        {
            read.WriteBytes(data);
            string newtoken = read.ReadString();

            if (session.Account == null)
            {
                Console.WriteLine("HATA: FBN Token geldi ama session.Account NULL!");
                session.FBNToken = newtoken;
            }
            else
            {
                session.Account.FBNToken = newtoken;
                Console.WriteLine($"FBN Token kaydedildi: {newtoken} (AccountID: {session.Account.ID})");
            }
        }
    }
}
