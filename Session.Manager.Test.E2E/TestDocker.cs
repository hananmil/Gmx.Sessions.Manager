namespace Session.Manager.Test.E2E
{
    [Collection("E2E")]

    public class TestDocker : IClassFixture<DockerManager>
    {
        private readonly string TestServer1 = "127.0.0.1:5001";
        private readonly string TestServer2 = "127.0.0.1:5002";
        private readonly string TestServer3 = "127.0.0.1:5003";

        private readonly DockerManager _dockerManager;

        public TestDocker(DockerManager dockerManager)
        {
            _dockerManager = dockerManager;   
        }

        [Fact]
        public async Task EnsureDockerServers()
        {
            var servers = await _dockerManager.ListServers();
            Assert.Equal(3, servers.Count);
            Assert.All(servers, s => Assert.Equal("running", s.State));
            Assert.All(servers, s => Assert.Contains(s.Ports, p => p.PublicPort == 5001 || p.PublicPort == 5002 || p.PublicPort == 5003));
        }

        [Fact]
        public async Task EnsureDockerServer1()
        {
            var server = await _dockerManager.FindContainer(TestServer1);
            Assert.NotNull(server);
            Assert.Equal("sessions", server.Image);
            Assert.Equal("running", server.State);
            Assert.Contains(server.Ports, p => p.PublicPort == 5001);
        }

    }
}