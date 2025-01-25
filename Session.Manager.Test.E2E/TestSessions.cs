namespace Session.Manager.Test.E2E
{
    [Collection("E2E")]
    public class TestSessions:IClassFixture<DockerManager>
    {
        private readonly string TestServer1 = "127.0.0.1:5001";
        private readonly string TestServer2 = "127.0.0.1:5002";
        private readonly string TestServer3 = "127.0.0.1:5003";

        private readonly ServiceProxy _serviceProxy = new ServiceProxy();
        private readonly SessionManager _sessionManager = new SessionManager();
        private readonly DockerManager _dockerManager;

        public TestSessions(DockerManager dockerManager)
        {
            _dockerManager = dockerManager;
        }

        [Fact]
        public async Task EnsureMissingSession()
        {
            var sessionId = Guid.NewGuid().ToString("n");
            var data = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Null(data);
        }

        [Fact]
        public async Task EnsureSessionWrite()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
        }

        [Fact]
        public async Task EnsureSessionReadSameServer()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(sessionData, readData);
        }

        [Fact]
        public async Task EnsureSessionReadDiffrentServers()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var readData = await _serviceProxy.Get(TestServer2, sessionId);
            Assert.Equal(sessionData, readData);
        }

        [Fact]
        public async Task EnsureSessionUpdateSameServer1()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var updatedData = _sessionManager.RandomBuffer(100);
            await _serviceProxy.Set(TestServer1, sessionId, updatedData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(updatedData, readData);
        }

        [Fact]
        public async Task EnsureSessionUpdateSameServer2()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var updatedData = _sessionManager.RandomBuffer(10);
            await _serviceProxy.Set(TestServer1, sessionId, updatedData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(updatedData, readData);
        }

        [Fact]
        public async Task EnsureSessionUpdateSameServer3()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var updatedData = _sessionManager.RandomBuffer(200);
            await _serviceProxy.Set(TestServer1, sessionId, updatedData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(updatedData, readData);
        }
    }
}