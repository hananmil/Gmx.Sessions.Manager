namespace Sessions.Manager.Controllers.DTO
{
    public class ServerStatus
    {
        public required string Ip { get; set; }
        public TimeSpan LastUpdate { get; set; }
        public bool Alive { get; set; }
    }
}
