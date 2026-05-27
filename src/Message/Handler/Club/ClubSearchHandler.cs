public static class ClubSearchHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = ByteBufferPool.Get();
        read.WriteBytes(message, true);

        string clubname = read.ReadString().ToLower();
        read.Dispose();
            
    }
}
