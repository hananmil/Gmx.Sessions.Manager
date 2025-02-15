﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Session.Manager.Test.E2E
{
    internal class ServiceProxy
    {
        private readonly HttpClient client = new HttpClient()
        {
            DefaultRequestHeaders = { { "Accept", "application/octet-stream" },
                { "Connection", "keep-alive" },
                { "Keep-Alive", "3600" },
            }
        };
        internal async Task Set( string server, string sessionName, byte[] data, int? expirySeconds = null )
        {
            var sw = Stopwatch.StartNew();
            var content = new ByteArrayContent(data);
            var response = await
                ( expirySeconds.HasValue
                    ? client.PutAsync("http://"+server+"/session/"+sessionName+"/"+expirySeconds.Value, content)
                    : client.PutAsync("http://"+server+"/session/"+sessionName, content)
                );
            response.EnsureSuccessStatusCode();
        }

        internal async Task<byte[]> Get( string server, string sessionName )
        {
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync("http://"+server+"/session/"+sessionName);
            if ( response.StatusCode == HttpStatusCode.NotFound )
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            return data;
        }
    }
}
