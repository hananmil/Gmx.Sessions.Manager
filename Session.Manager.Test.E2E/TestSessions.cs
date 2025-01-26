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
        public async Task EnsureSessionReadDiffrentServersMultipleUpdates()
        {
            var sessionData1 = _sessionManager.RandomBuffer(100);
            var sessionData2 = _sessionManager.RandomBuffer(100);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData1);
            var readData1 = await _serviceProxy.Get(TestServer3, sessionId);

            await _serviceProxy.Set(TestServer2, sessionId, sessionData2);
            var readData2 = await _serviceProxy.Get(TestServer3, sessionId);
            Assert.Equal(sessionData1, readData1);
            Assert.Equal(sessionData2, readData2);
        }

        [Fact]
        public async Task EnsureSessionReadDiffrentServers100MB()
        {
            var sessionData = _sessionManager.RandomBuffer(100*1024*1024);
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

        [Fact]
        public async Task EnsureExpiry()
        {
            var sessionData = _sessionManager.RandomBuffer(10);
            var sessionId = Guid.NewGuid().ToString("n");
            await _serviceProxy.Set(TestServer2, sessionId, sessionData,1);
            await Task.Delay(2000);
            var readData = await _serviceProxy.Get(TestServer3, sessionId);
            Assert.Null(readData);

        }

        [Fact]
        public async Task EnsureFilesSessionSpecialCharacters1()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = System.Web.HttpUtility.UrlEncode("/test1/test2"); 
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(sessionData, readData);
        }
        [Fact]
        public async Task EnsureFilesSessionSpecialCharacters2()
        {
            var sessionData = _sessionManager.RandomBuffer(100);
            var sessionId = System.Web.HttpUtility.UrlEncode("\\test\\test\\");
            await _serviceProxy.Set(TestServer1, sessionId, sessionData);
            var readData = await _serviceProxy.Get(TestServer1, sessionId);
            Assert.Equal(sessionData, readData);
        }
    }
}