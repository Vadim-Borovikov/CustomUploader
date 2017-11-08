using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace CustomUploader.Logic
{
    internal static class RestSharpProvider
    {
        public static async Task<T> ExecuteGetTaskAsync<T>(string baseUrl, string resource,
                                                           Dictionary<string, object> parameters)
            where T : new()
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest(resource);
            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    request.AddParameter(key, parameters[key]);
                }
            }
            IRestResponse<T> response = await client.ExecuteGetTaskAsync<T>(request);
            return response.Data;
        }
    }
}
