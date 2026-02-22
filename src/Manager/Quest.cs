public  class Quest
{
    public MissionType Type { get; set; }
    public int ID { get; set; }
    public int Target { get; set; }
    public ItemType RewardType { get; set; }
    public int Goal { get; set; }
    public int CurrentGoal { get; set; }
    public bool IsDailyQuest { get; set; }
    public bool IsPremium { get; set; }
    public bool IsCompleted { get; set; }
   


    public enum MissionType

    {
        SendChatMessage,
        CreateTeam,
        JoinTeam,
        AddFriend,
        
    }
}