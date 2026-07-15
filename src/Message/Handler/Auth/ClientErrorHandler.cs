[PacketHandler(MessageType.ClientErrorRequest)]
public class ClientErrorHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var packet = data.DeserializePacket<ClientErrorPacket>();

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