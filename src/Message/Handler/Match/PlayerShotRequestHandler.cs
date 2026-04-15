using System.Numerics;

[PacketHandler(MessageType.ShootRequest)]
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

        var request = new ShootRequestPacket();
        request.Deserialize(read);
        
        float X = request.DirectionX;
        float Y = request.DirectionY;
        read.Dispose();
        
      Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

        Bullet bullet = new Bullet
        {
            BulletId = battle.GetNextBulletId(),
            Position = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Y),
            Direction = Vector2.Normalize(new Vector2(X, Y)),
            Speed = 10f,
            OwnerID = session.ID,
            startPos = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Y),
            Damage = 50,
            menzil = 7f

        };
        battle.AddBullet(bullet);

        Vector3 dir = new Vector3(X, Y, 0f);
        
        var response = new ShootResponsePacket
        {
            OwnerID = session.ID,
            BulletId = bullet.BulletId,
            Speed = bullet.Speed,
            PositionX = session.PlayerData.Position.X,
            PositionY = session.PlayerData.Position.Y,
            PositionZ = session.PlayerData.Position.Z, 
            DirectionX = X,
            DirectionY = Y
        };

        
        var battleplayers = battle.GetPlayers();
        foreach(var acc in battleplayers)
        {
              acc.session.Send(response);
        }
    }
}
