namespace Sessions.Manager.Interfaces
{
    public interface WriteInterface
    {
        Task UpdateSession(string sessionId, Stream streamReader,TimeSpan? expiry = null);
    }
}
