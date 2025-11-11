
public enum MessageType : int
{
    // Connection
    FirstConnectionRequest,
    FirstConnectionResponse,
    Disconnect,
    Ping,
    Pong,

    // Authentication
    AuthLoginRequest,
    AuthLoginResponse,
    NewAccountCreateResponse,

    // Account
    AccountData,
    LoginFailed,

    // Profile
    ChangeNameRequest,
    ChangeNameResponse,
    ChangeNameColorRequest,
    ChangeNameColorResponse,
    SetAvatarRequest,
    SetAvatarResponse,
    ShowProfileRequest,
    ShowProfileResponse,


    // Friends
    SendFriendRequest,
    SendFriendResponse,
    AcceptFriendRequest,
    AcceptFriendResponse,
    DeclineFriendRequest,
    DeclineFriendResponse,
    DeleteFriendRequest,
    DeleteFriendResponse,
    NewFriendsList,
    NewRequestList,



    // Team
    CreateTeamRequest,
    CreateTeamResponse,
    JoinTeamRequest,
    JoinTeamResponse,
    LeaveTeamRequest,
    LeaveTeamResponse,
    SendTeamMessageRequest,
    SendTeamMessageResponse,






    // notfications
    Notification,
    AddFriendsRequest,
    AddFriendsResponse,
    AllNotficationViewed,


    // Club
    GetRandomClubRequest,
    GetRandomClubResponse,
    ClubShowRequest,
    ClubShowResponse,
    SendClubMessage,
    GetClubMessage,
    JoinClubRequest,
    JoinClubResponse,
    LeaveClubRequest,
    LeaveClubResponse,
    ClubEditRequest,
    ClubEditResponse,
    ClubSearchRequest,
    ClubSearchResponse,
    KickMemberinClubRequest,
    KickMemberinClubResponse,
    MemberToUpperRequest,
    MemberToUpperResponse,
    MemberToLowerRequest,
    MemberToLowerResponse,



    // Battle
    MatchMakingRequest,
    MatchMakingCancelRequest,
    MatchFound,
    MatchMakingAddPlayer,
    StartMatch,
    Move,
    Dead,
    PlayerMoved,
    PlayerDead,
    ShootRequest,
    Shoot,
    HitRequest,
    PlayerHealthUpdate,

}
