[PacketHandler(MessageType.GetEvents)]
public static class GetEvents
{
    public static void Handle(Session session, byte[] data)
    {
        Console.WriteLine("events handler ");
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteVarInt((int)MessageType.EventsResponse);
        buffer.WriteVarInt(DynamicConfigManager.Config.ActiveEvents.Count);
        foreach (var events in DynamicConfigManager.Config.ActiveEvents)
        {

            buffer.WriteVarInt((int)events.Type);
            buffer.WriteBool(events.IsStarted);
            int remainingSeconds = 0;
            var now = DateTime.UtcNow;

            if (events.IsStarted)
            {
                remainingSeconds = (int)Math.Max(0, (events.EndTime - now).TotalSeconds);
            }
            else
            {
                remainingSeconds = (int)Math.Max(0, (events.StartTime - now).TotalSeconds);
            }
            buffer.WriteVarInt(remainingSeconds);

            buffer.WriteVarInt(events.Value);
            buffer.WriteVarInt(events.Value2);


        }
        byte[] response = buffer.ToArray();
        buffer.Dispose();

        session.Send(response);
    }
}