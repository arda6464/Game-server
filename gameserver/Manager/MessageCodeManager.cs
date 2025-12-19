public static class MessageCodeManager
{
    public enum Message : short
    {

        RegisterSucceses,
        EmailAlreadyUsed,
        NotAClub,
        ClubFull,
        AlreadyİnClub,
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
        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.MessageCode);
        buffer.WriteShort((short)message);
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);        
    }
}