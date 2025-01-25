using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Session.Manager.Test.E2E
{
    public class DockerManager : IDisposable
    {
        private DockerClient _client;

        public DockerManager()
        {
            _client = new DockerClientConfiguration().CreateClient();

            // Perform your E2E tests here

        }

        public async Task<IList<ContainerListResponse>> ListServers()
        {
            var listParams = new ContainersListParameters() {
            All = true
            };
            var containers = await _client.Containers.ListContainersAsync(listParams);

            containers = containers.Where(c => c.Image == "sessions").ToArray();

            return containers;
        }

        public async Task<ContainerListResponse?> FindContainer(string address)
        {
            var containerPort = int.Parse(address.Split(":").Last());
            var containers = await ListServers();
            return containers.FirstOrDefault(c => c.Ports.Any(p=>p.PublicPort == containerPort));
        }

        public void Dispose()
        {
            _client.Dispose();
            // Stop the application using docker-compose
        }

        internal async Task<string> Stop(string server)
        {
            var container = await FindContainer(server);
            if (container == null)
            {
                throw new Exception("Container not found "+server);
            }
            await _client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
            return container.ID;
        }

        internal async Task Start(string containerId)
        {
            await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
        }
    }
}
