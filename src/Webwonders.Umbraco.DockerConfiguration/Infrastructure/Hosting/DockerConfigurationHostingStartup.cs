using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: HostingStartup(typeof(Webwonders.Umbraco.DockerConfiguration.DockerConfigurationHostingStartup))]

namespace Webwonders.Umbraco.DockerConfiguration
{
    public class DockerConfigurationHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            // Hook into the app configuration phase
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                DockerConfigurationMiddleware.ConfigureDockerSqlDb(configBuilder);
            });
        }
    }
}