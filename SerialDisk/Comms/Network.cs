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

        public async static Task<string> GetReleases()
        {
            NetworkClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await GetHttpResponse("https://api.github.com/repos/z80andrew/serialdisk/releases");
        }

        private async static Task<string> GetHttpResponse(string uri)
        {
            return await NetworkClient.GetStringAsync(uri);
        }
    }
}
