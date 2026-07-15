public interface IGameMessage
{
    void Handle(Session session, byte[]? data);
}
