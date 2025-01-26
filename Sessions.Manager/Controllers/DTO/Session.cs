namespace Sessions.Manager.Controllers.DTO
{
    public class Session
    {
        public required string Id { get; set; }
        public required string Server { get; set; }
        public TimeSpan? Expiry { get; set; }
    }
}
