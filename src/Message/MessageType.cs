
public enum MessageType : int
{
    // Connection
    FirstConnectionRequest,
    FirstConnectionResponse,
    Disconnect,
    Ping,
    Pong,
    Maintance,
    MessageCode,
    Alive,
    Presence,


    // Authentication
    AuthLoginRequest,
    AuthLoginResponse,
    NewAccountCreateResponse,

    // Account
    AccountData,
    LoginFailed,
    LoginOKRequest,
    LoginOKResponse,
    SendVerifyCode,
    VerifyCodeResponse,
    AccountLogin,
    SignAccount,



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
    InviteToTeamRequest,
    InviteToTeamResponse,






    // notfications
    Notification,
    AllNotficationViewed,

    // Leaderboards
    LeaderboardRequest,
    LeaderboardResponse,




    // Club

    ClubCreateRequest,
    ClubCreateResponse,
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

    PlayerMoved,
    PlayerDead,
    ShootRequest,
    Shoot,
    HitRequest,
    PlayerHealthUpdate,

    // Shop
    GetAllMarketItemsRequest,
    GetAllMarketItemsResponse,



    // Support
    SupportMessageSend,
    SupportMessageResponse,
    SupporCreateTicketRequest,
    SupporCreateTicketResponse,
    ReportPlayerRequest,
    SupportGetAllTicketRequest,
    SupportGetAllTicketResponse,
    SupportTicketClosed,
    SupportTicketOpened



}
