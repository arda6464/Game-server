public class Notfication
{
    public int IndexID { get; set; }
    public NotficationTypes.NotficationType type { get; set; }       
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
public class NotficationTypes
{
    public enum NotficationType : byte
    {
        toast,
        banner,
        Inbox,
        Push,
    }

}