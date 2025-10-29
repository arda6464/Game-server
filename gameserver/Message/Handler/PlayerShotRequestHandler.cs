using System.Numerics;

public static class PlayerShotRequestHandler
{
    public static void Handle(Session session,byte[] message)
    {
        if (session.PlayerData == null)
        {
            Logger.errorslog("[PlayerShotRequestHandler] PlayerData null!");
            return;
        }

        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();

        float X = read.ReadFloat();
        float Y = read.ReadFloat();
        read.Dispose();
        
        // Shot validation: Mesafe ve pozisyon kontrolü
      /*  float distance = MathF.Sqrt(X * X + Y * Y);
        if (distance > 1000f || float.IsNaN(X) || float.IsNaN(Y))
        {
            Logger.errorslog($"[PlayerShotRequestHandler] Invalid shot data from {session.AccountId}");
            return;
        }*/

        Vector3 dir = new Vector3(X, Y, 0f);
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.Shoot);

        buffer.WriteString(session.AccountId);
        buffer.WriteFloat(session.PlayerData.PositionX);
        buffer.WriteFloat(session.PlayerData.PositionY);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        
        byte[] data = buffer.ToArray();
        buffer.Dispose();
        var arenaplayers = ArenaManager.GetPlayers(session.PlayerData.ArenaId);
        foreach(var acc in arenaplayers)
        {
            if (acc.session != null && acc.AccountId != session.AccountId)  acc.session.Send(data);
        }
    }
}