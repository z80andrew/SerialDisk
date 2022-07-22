using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.Comms
{
    public static class Network
    {
        private static HttpClient _networkClient;

        private static HttpClient NetworkClient
        {
            get
            {
                if (_networkClient == null)
                {
                    _networkClient = new HttpClient();
                    NetworkClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("z80andrew.SerialDisk", ConfigurationHelper.ApplicationVersion));
                }

                return _networkClient;
            }
        }

        private static HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            NetworkClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("z80andrew.SerialDisk", ConfigurationHelper.ApplicationVersion));
            return httpClient;
        }

        public async static Task<string> GetReleaseTags()
        {
            NetworkClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await GetHttpResponse("https://api.github.com/repos/z80andrew/serialdisk/tags");
        }

        public async static Task<string> GetLatestVersionInfo()
        {
            NetworkClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await GetHttpResponse("https://api.github.com/repos/z80andrew/serialdisk/releases/latest");
        }

        private async static Task<string> GetHttpResponse(string uri)
        {
            return await NetworkClient.GetStringAsync(uri);
        }
    }
}
