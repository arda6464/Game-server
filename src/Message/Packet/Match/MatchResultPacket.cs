public class MatchResultPacket : IPacket
{

    public int Placement { get; set; }
    public int Kills { get; set; }
    public int DamageDealt { get; set; }
    public int HitDealt{get;set;}
    public int CurrentTrophies {get;set;}
    public int TrophiesDelta { get; set; }
  //  public int RewardCoins { get; set; }
    public int RewardXp { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }
    public string ElapsedTime {get; set;}
    

    public void Serialize(ByteBuffer buffer)
    {

        buffer.WriteVarInt((int)MessageType.MatchResult);
        buffer.WriteVarInt(Placement);
        buffer.WriteVarInt(Kills);
        buffer.WriteVarInt(DamageDealt);
        buffer.WriteVarInt(HitDealt);
        buffer.WriteVarInt(CurrentTrophies);
        buffer.WriteVarInt(TrophiesDelta);
       // buffer.WriteVarInt(RewardCoins);
        buffer.WriteVarInt(RewardXp);
        buffer.WriteVarInt(Level);
        buffer.WriteVarInt(Experience);
        buffer.WriteVarInt(ExperienceToNextLevel);
        buffer.WriteVarString(ElapsedTime);
    }

    public void Deserialize(ByteBuffer buffer)
    {
    }
}
