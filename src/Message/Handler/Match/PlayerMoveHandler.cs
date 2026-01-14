using System.Numerics;

public static class PlayerMoveHandler
{
    public static void Handle(Session session,byte[] data)
    {
        if (session.PlayerData == null)
        {
            Logger.errorslog("[PlayerMoveHandler] PlayerData null!");
            return;
        }

        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int type = read.ReadInt();
        float X = read.ReadFloat();
        float Y = read.ReadFloat();

        read.Dispose();
       Arena arena = ArenaManager.GetArena(session.PlayerData.ArenaId);
        arena.UpdatePlayerPosition(session.AccountId, new Vector2(X, Y));
        var arenaplayers = arena.GetPlayers();
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.PlayerMoved);
        buffer.WriteString(session.AccountId);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        byte[] msg = buffer.ToArray();
        buffer.Dispose();
    //    Console.WriteLine($"[PlayerMoveHandler] Oyuncu {session.AccountId} pozisyonunu güncelledi: ({X}, {Y})");
        foreach(var p in arenaplayers)
        {
           
            if (p.AccountId != session.AccountId)
                p.session.Send(msg);
            
            
        }
    }
}