public static class MessageCodeManager
{
    public enum Message : short
    {

        RegisterSucceses,
        EmailAlreadyUsed,
        NotAClub,
        ClubFull,
        SendClubJoinRequest,
        AlreadyRequestClub,
        AlreadyInClub,
        ClubUnusableName,
        ClubUnusableDescription,
        ClubKicked,
        MemberNotİnClub,
        NoAuthorityClub,
        JustClubOwnerChange,
        ClubRoleUpdateCoOwner,
        ClubRoleDoOwner,
        ClubRoleLowerCoOwner,
        MemberAlreadyLowest,
        CannotLowerOwner,
        İnvalidAvatar,
        ThisYou,
        İnvalidTransaction,
        GeneralError,
        İnvalidName,
        AlreadyİnTeam,
        NotATeam,









    }
    public static void Send(Session session, Message message)
    {
        ByteBuffer buffer = ByteBufferPool.Get();

        buffer.WriteVarInt((int)MessageType.MessageCode);
        buffer.WriteVarInt((int)message);
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);        
    }
}