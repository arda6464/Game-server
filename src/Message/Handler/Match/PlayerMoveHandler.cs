using System.Numerics;

[PacketHandler(MessageType.Move)]
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
        int _ = read.ReadShort();
        
        var request = new PlayerMoveRequestPacket();
        request.Deserialize(read);
        
        float X = request.X;
        float Y = request.Y;
        float Z = request.Z;

        read.Dispose();
       Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

        battle.UpdatePlayerPosition(session.AccountId, new Vector3(X, Y, Z));
        var battleplayers = battle.GetPlayers();
        
        var response = new PlayerMovedPacket
        {
             AccountId = session.AccountId,
             X = X,
             Y = Y,
             Z = Z
        };

        
        foreach(var p in battleplayers)
        {
            if (p.AccountId != session.AccountId)
                p.session?.Send(response);
        }
    }
}
