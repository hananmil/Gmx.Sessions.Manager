// See https://aka.ms/new-console-template for more information
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

Console.WriteLine("Hello, World!");
HttpClient client = new HttpClient();

var servers = new string[] { "127.0.0.1:5001", "127.0.0.1:5002", "127.0.0.1:5003", };

await LoadBalancerTest(1024*1024,TimeSpan.FromMinutes(1));

async Task LoadBalancerTest(int size,TimeSpan duration)
{
    var sw = Stopwatch.StartNew();
    var tasks = new List<Task>();
    var random = new Random();
    while(sw.Elapsed<duration)
    {
        var server = servers[random.Next(servers.Length)];
        tasks.Add(CreateRandomSession(server,size));
        await Task.Delay(500);
    }
    await Task.WhenAll(tasks);
    Console.WriteLine("Load balancer test completed in "+sw.Elapsed.TotalMilliseconds+"ms");
}

async Task CreateRandomSession(string serverName, int size)
{
    var sw = Stopwatch.StartNew();
    var sessionName = Guid.NewGuid().ToString("n");
    Console.WriteLine("Uploading "+size+" to server "+serverName);
    var content = new ByteArrayContent(RandomBuffer(size));
    var response = await client.PutAsync("http://"+serverName+"/session/"+sessionName, content);
    response.EnsureSuccessStatusCode();
    Console.WriteLine("Session "+sessionName+" uploaded to server "+serverName+" in "+sw.Elapsed.TotalMilliseconds+"ms");
}



byte[] RandomBuffer(int size)
{
    var buffer = new byte[size];
    new Random().NextBytes(buffer);
    return buffer;
}

