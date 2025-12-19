public class Notfication
{
    public int Id { get; set; }        // 11 = toast, 10 = banner, 12 = inbox
    public string Title { get; set; }
    public string Message { get; set; }
    public string ButtonText;
    public bool IsViewed = false;
    public string Url;
    public int iconid = 2;
    public string Sender;
    public DateTime Timespam = DateTime.Now;
    public RewardItemType.RewardItemTypes rewardItemType;
    public int DonationCount;
    public bool İsclamed;



    public Notfication()
    {
    }
}
public class RewardItemType
{
    public enum RewardItemTypes

    {
        Chacter,
        Skin,
        Gem,
        Coin

    }
}

