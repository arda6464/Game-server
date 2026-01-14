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
      Arena arena = ArenaManager.GetArena(session.PlayerData.ArenaId);
        Bullet bullet = new Bullet
        {
            BulletId = arena.GetBulletId(),
            Position = session.PlayerData.Position,
            Direction = Vector2.Normalize(new Vector2(X, Y)),
            Speed = 10f,
            OwnerId = session.AccountId,
            startPos = session.PlayerData.Position,
            Damage = 50,
            menzil = 7f

        };
        arena.AddBullet(bullet);

        Vector3 dir = new Vector3(X, Y, 0f);
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.Shoot);

        buffer.WriteString(session.AccountId); // owner id
        buffer.WriteInt(bullet.BulletId);
        buffer.WriteFloat(bullet.Speed);
        buffer.WriteFloat(session.PlayerData.Position.X);
        buffer.WriteFloat(session.PlayerData.Position.Y);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
       //  Console.WriteLine($"[PlayerShotRequestHandler] Oyuncu {session.AccountId} atış yaptı. Bullet ID: {bullet.BulletId}, Pozisyon: ({bullet.Position.X}, {bullet.Position.Y}), Yön: ({X}, {Y})  speed: {bullet.Speed}");
        
        byte[] data = buffer.ToArray();
        buffer.Dispose();
        
        var arenaplayers = arena.GetPlayers();
        foreach(var acc in arenaplayers)
        {
              acc.session.Send(data);
        }
    }
}