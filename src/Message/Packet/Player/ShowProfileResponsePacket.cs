using Logic;

public class ShowProfileResponsePacket : IPacket
{
   public AccountManager.AccountData account {get;set;}

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ShowProfileResponse);
        buffer.WriteVarInt(account.ID);
        buffer.WriteVarString(account.Username);
        buffer.WriteBool(account.Premium > 0);
        buffer.WriteVarInt(account.Namecolorid);
        buffer.WriteVarInt(account.Avatarid);
        buffer.WriteVarInt(1); // language

        // Last Seen Calculation
        int lastSeenSeconds;
        if (SessionManager.IsOnline(account.ID))
        {
            lastSeenSeconds = 0; // Online
        }
        else
        {
            TimeSpan ts = DateTime.Now - account.LastLogin;
            lastSeenSeconds = (int)Math.Max(0, ts.TotalSeconds);
        }
        buffer.WriteVarInt(lastSeenSeconds); // LastSeen 

        buffer.WriteVarInt(account.CreatedAt.Year);
        buffer.WriteVarInt(account.Trophy);
        buffer.WriteVarInt(account.WinStreak);
        Random random = new Random();
        buffer.WriteVarInt(random.Next(0,1000)); // solo win
        buffer.WriteVarInt(random.Next(0,1000)); // duo
        buffer.WriteVarInt(random.Next(0,1000)); // squad
        buffer.WriteVarString(account.ClubName);
        buffer.WriteVarInt(1); // club role

        
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
