namespace Sessions.Manager.Interfaces
{
    public interface ReadInterface
    {
        Task<Stream?> GetSession(string sessionId);
    }
}
