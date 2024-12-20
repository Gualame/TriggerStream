using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TriggerStream
{
    class Program
    {
        static async Task Main(string[] args)
        {
            /*
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            */

            Console.WriteLine("Starting application...");

            IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

            string authenticationApiUrl = configuration["Endpoint.Credentials"];
            string hostname = configuration["Endpoint.HostName"];
            string apis = configuration["Endpoint.APIs"];
            string username = configuration["Credentials.Username"];
            string password = configuration["Credentials.Password"];


            try
            {
                await TriggerStream.Process(authenticationApiUrl, username, password, hostname, apis, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("Application completed.");
        }
    }
}
