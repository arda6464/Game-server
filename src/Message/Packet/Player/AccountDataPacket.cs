using Logic;

public class AccountDataPacket : IPacket
{
    public AccountManager.AccountData Account { get; set; }

    public AccountDataPacket(AccountManager.AccountData account)
    {
        Account = account;
    }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.AccountData);
        buffer.WriteVarInt(Account.ID);
        buffer.WriteVarInt(Account.Gems);
        buffer.WriteVarInt(Account.Coins);
        buffer.WriteVarInt(Account.Trophy);
        buffer.WriteVarInt(Account.Level);
        buffer.WriteVarInt(Account.Namecolorid);
        buffer.WriteVarInt(Account.Avatarid);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new System.NotImplementedException();
    }
}
