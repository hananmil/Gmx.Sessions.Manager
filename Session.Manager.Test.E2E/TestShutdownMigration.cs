namespace Session.Manager.Test.E2E
{
    [Collection("E2E")]
    public class TestShutdownMigration: IClassFixture<DockerManager>
    {
        private readonly string TestServer1 = "127.0.0.1:5001";
        private readonly string TestServer2 = "127.0.0.1:5002";
        private readonly string TestServer3 = "127.0.0.1:5003";

        private readonly ServiceProxy _serviceProxy = new ServiceProxy();
        private readonly SessionManager _sessionManager = new SessionManager();
        private readonly DockerManager _dockerManager;

        public TestShutdownMigration(DockerManager dockerManager)
        {
            _dockerManager = dockerManager;
        }

        [Fact]
        public async Task EnsureShutdownMigration()
        {
            var sessionData = _sessionManager.RandomBuffer(1024);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var id = await _dockerManager.Stop(TestServer1);
            var recieved = await _serviceProxy.Get(TestServer2, sessionId);
            await _dockerManager.Start(id);
            Assert.Equal(sessionData, recieved);
        }
    }
}