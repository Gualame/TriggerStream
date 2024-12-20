using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TriggerStream
{
    public class TriggerStream
    {
        [FunctionName("TriggerStream")]
        public void Run([TimerTrigger("0 0 17 * * 1-5")] TimerInfo myTimer, ILogger log)
        //public void Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function starting at: {DateTime.Now}");

            Process(log).Wait();

            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
        }

        public static async Task<string> GetAuthTokenAsync(string apiUrl, string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                var credentials = new { Username = username, Password = password };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(credentials);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PostAsync(apiUrl, content))
                {
                    response.EnsureSuccessStatusCode();
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(responseContent)?.Token;
                }
            }
        }

        public static async Task Process(ILogger log)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .Build();

            string authenticationApiUrl = Environment.GetEnvironmentVariable("Endpoint.Credentials");
            string hostname = Environment.GetEnvironmentVariable("Endpoint.HostName");
            string apis = Environment.GetEnvironmentVariable("Endpoint.APIs");
            string username = Environment.GetEnvironmentVariable("Credentials.Username");
            string password = Environment.GetEnvironmentVariable("Credentials.Password");

            await Process(authenticationApiUrl, username, password, hostname, apis, log);
        }

        public static async Task Process(string authenticationApiUrl, string userName, string password, string hostname, string apis, ILogger log)
        {
            try
            {
                List<string> apisToCall = new List<string>();
                string endpoint;

                log.LogInformation($"URI Authentication: {authenticationApiUrl}");

                log.LogInformation($"Calling credentials method: {DateTime.Now}");

                string authToken = await GetAuthTokenAsync(authenticationApiUrl, userName, password);
                if (!string.IsNullOrEmpty(authToken))
                {
                    log.LogInformation($"Authentication Successful. Token: " + authToken);

                    if(apis.IndexOf("|") > -1)
                    {
                       apisToCall = apis.Split('|').ToList();
                    }
                    else
                    {
                        apisToCall.Add(apis);
                    }

                    foreach(var api in apisToCall)
                    {
                        endpoint = hostname + api;

                        log.LogInformation($"Calling API: {endpoint}");
                        
                        await CallTargetApiAsync(endpoint, authToken, log);
                    }

                    log.LogInformation($"Target API call successful.");
                }
                else
                {
                    log.LogInformation($"Authentication failed. Unable to get the token.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public static async Task CallTargetApiAsync(string apiUrl, string authToken, ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                using (HttpResponseMessage response = await client.GetAsync(apiUrl))
                {
                    response.EnsureSuccessStatusCode();

                    log.LogInformation($"API called: {apiUrl} => Status code: {response.StatusCode.ToString()}");  
                }
            }
        }

        // Assuming a class like this to represent the token response
        private class TokenResponse
        {
            public string Token { get; set; }
        }
    }
}
