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
       
        var arenaplayers = ArenaManager.GetPlayers(session.PlayerData.ArenaId);
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.PlayerMoved);
        buffer.WriteString(session.AccountId);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        byte[] msg = buffer.ToArray();
        buffer.Dispose();

        foreach(var p in arenaplayers)
        {
           
            if (p.AccountId != session.AccountId)
                p.session.Send(msg);
            
            
        }
    }
}