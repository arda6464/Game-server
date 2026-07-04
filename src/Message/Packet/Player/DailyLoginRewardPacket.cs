public class DailyLoginRewardPacket : IPacket
{
    public int Day { get; set; }          // Current streak day (DailyRewardStreak)
    public  DailyStreakData[] RewardItems { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.DailyLoginReward);
        buffer.WriteVarInt(Day);
        foreach (var reward in RewardItems)
        {
            buffer.WriteVarInt(reward.Day);
            buffer.WriteBool(reward.IsAvaiable);
            buffer.WriteBool(reward.IsClaimed);
            buffer.WriteVarInt((int)reward.Reward.Type);
            buffer.WriteVarInt(reward.Reward.DataId);
            buffer.WriteVarInt(reward.Reward.Count);
        }
    }
    public void Deserialize(ByteBuffer buffer) { }
}
