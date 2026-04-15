[PacketHandler(MessageType.ClientErrorRequest)]
public static class ClientErrorHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(data);
        ClientErrorPacket packet = new ClientErrorPacket();
        packet.Deserialize(buffer);
        buffer.Dispose();

        // Konsola yazmaya devam et (geliştirme için kolaylık)
        Console.WriteLine($"=============Client Error [{packet.AccountId}]===================");
        Console.WriteLine($"message: {packet.LogMessage}");
        Console.WriteLine($"type: {packet.LogType}");
        Console.WriteLine($"trace: {packet.StackTrace}");
        Console.WriteLine($"scene: {packet.SceneName}");
        Console.WriteLine($"================================");

        // Sisteme Kaydet (Structured & Aggregated)
        ClientErrorManager.StoreLog(packet);
    }
}