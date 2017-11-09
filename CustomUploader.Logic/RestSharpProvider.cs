using System.Collections.Generic;
using RestSharp;

namespace CustomUploader.Logic
{
    internal static class RestSharpProvider
    {
        public static T Execute<T>(string baseUrl, string resource, DataFormat format,
                                   Dictionary<string, object> parameters)
            where T : new()
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest(resource)
            {
                RequestFormat = format
            };
            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    request.AddParameter(key, parameters[key]);
                }
            }

            IRestResponse<T> response = client.Execute<T>(request);
            return response.Data;
        }
    }
}
