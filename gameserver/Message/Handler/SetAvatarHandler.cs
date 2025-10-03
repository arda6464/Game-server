using System;

public static class SetAvatar
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("Set Avatar");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);
        int _ = BUFFER.ReadInt();

        int Id = BUFFER.ReadInt();
        BUFFER.Dispose();
        // todo number control(gönderdiği profil id truemu?)
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account == null)
        {
            // todo....
            return;
        }
        account.Avatarid = Id;
        Console.WriteLine("Avatar değiştirildi: " + Id);

    }
}