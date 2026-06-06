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
        MemberNotInClub,
        NoAuthorityClub,
        JustClubOwnerChange,
        ClubRoleUpdateCoOwner,
        ClubRoleDoOwner,
        ClubRoleLowerCoOwner,
        MemberAlreadyLowest,
        CannotLowerOwner,
        InvalidAvatar,
        ThisYou,
        InvalidTransaction,
        GeneralError,
        InvalidName,
        AlreadyInTeam,
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