using System.Collections.Generic;
using System.Linq;

public static class OnlinePlayerManager
{
    /// <summary>
    /// Viewer'a gösterilebilecek online oyuncuları snapshot olarak üretir.
    /// Kaynak veri SessionManager ve AccountCache'tir.
    /// </summary>
    public static List<OnlinePlayerData> BuildSnapshotForViewer(AccountManager.AccountData viewer)
    {
        List<int> addedIds = new List<int>();
        var snapshot = new List<OnlinePlayerData>();

        foreach (var friend in viewer.Friends)
        {
            if (SessionManager.IsOnline(friend.ID))
            {
                var friendAccount = AccountCache.Load(friend.ID);
                if (friendAccount != null)
                {
                    snapshot.Add(CreateOnlinePlayerData(viewer, friendAccount));
                    addedIds.Add(friend.ID);
                }
            }
        }
        if (viewer.Clubid > 0)
        {
            var club = ClubCache.Load(viewer.Clubid);
            foreach (var member in club.Members)
            {
                if (!addedIds.Contains(member.ID) && SessionManager.IsOnline(member.ID))
                {
                    AccountManager.AccountData? memberAccount = AccountCache.Load(member.ID);
                    if((memberAccount == null) || (memberAccount.ID == viewer.ID)) continue; // Hesap yüklenemezse veya kendisi ise atla
                    snapshot.Add(CreateOnlinePlayerData(viewer, memberAccount));
                    //  addedIds.Add(member.ID); //  arkadaşlar zaten eklendiği için bu satır şu an gereksiz görünüyor.
                }
            }
        }
        return snapshot;





    }


    /// <summary>
    /// Tek bir online oyuncu kaydını viewer için view model'e çevirir.
    /// </summary>
   

    /// <summary>
    /// İstenirse tüm online oyuncuların ham snapshot'ını üretir.
    /// </summary>


    private static bool IsVisibleToViewer(AccountManager.AccountData viewer, AccountManager.AccountData target)
    {
        bool isFriend = viewer.Friends != null && viewer.Friends.Any(f => f.ID == target.ID);
        bool isClubMember = viewer.Clubid > 0 && target.Clubid > 0 && viewer.Clubid == target.Clubid;

        return isFriend || isClubMember;
    }

    private static OnlinePlayerData CreateOnlinePlayerData(AccountManager.AccountData viewer, AccountManager.AccountData target)
    {
        bool isFriend = viewer.Friends != null && viewer.Friends.Any(f => f.ID == target.ID);
        bool isClubMember = viewer.Clubid > 0 && target.Clubid > 0 && viewer.Clubid == target.Clubid;

        return new OnlinePlayerData
        {
            ID = target.ID,
            Username = target.Username,
            AvatarId = target.Avatarid,
            NameColorID = target.Namecolorid,
            Trophy = target.Trophy,
            LookingForTeam = target.LookingForTeam,
            DisturbMode = target.DoNotDisturb,
            IsFriend = isFriend,
            IsClubMember = isClubMember,
        };
    }

    private static List<OnlinePlayerData> SortSnapshot(List<OnlinePlayerData> snapshot)
    {
        return snapshot
            .OrderByDescending(p => p.LookingForTeam)
            .ThenByDescending(p => p.IsFriend)
            .ThenByDescending(p => p.IsClubMember)
            .ThenByDescending(p => p.Trophy)
            .ThenBy(p => p.Username)
            .ToList();
    }
}
