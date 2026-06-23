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
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteVarInt((int)MessageType.MessageCode);
            buffer.WriteVarInt((int)message);
            session.Send(buffer.GetBufferSegment());
        }
    }
}