public class DailyLoginClaimPacket : IPacket
{
    public bool IsSucessfull { get; set; }
    public RewardItem Drop = new();
    public int Day { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClaimDailyRewardResponse);
        buffer.WriteBool(IsSucessfull);
        buffer.WriteVarInt(Day);
        if (IsSucessfull)
        {

            buffer.WriteVarInt((int)Drop.Type);
            buffer.WriteVarInt(Drop.DataId);
            buffer.WriteVarInt(Drop.Count);

        }

    }
    public void Deserialize(ByteBuffer buffer) { }
}
