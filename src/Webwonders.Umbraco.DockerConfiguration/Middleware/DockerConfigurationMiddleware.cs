using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Webwonders.Umbraco.DockerConfiguration;

/// <summary>
/// Middleware to configure a Docker-based SQL Server setup for Umbraco projects
/// using environment variables.
/// </summary>
public static class DockerConfigurationMiddleware
{
    public static void ConfigureDockerSqlDb(IConfigurationBuilder builder)
    {
        // Directly read from environment variables
        var useLocalSql = Environment.GetEnvironmentVariable("Use_Local_Docker_SQL") ?? "false";
        var dbName      = Environment.GetEnvironmentVariable("Local_Docker_DB_NAME");
        var dbPassword  = Environment.GetEnvironmentVariable("Local_Docker_PASSWORD");
        var dbPort      = Environment.GetEnvironmentVariable("Local_Docker_PORT");

        if (useLocalSql.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(dbPassword) || string.IsNullOrEmpty(dbPort))
            {
                throw new Exception(
                    "Error: SQL Server configuration is incomplete. " +
                    "Ensure 'Local_Docker_DB_NAME', 'Local_Docker_PASSWORD', and " +
                    "'Local_Docker_PORT' are set, or disable 'Use_Local_Docker_SQL'."
                );
            }

            Console.WriteLine("Starting Docker configuration check...");

            if (!IsDockerInstalled())
            {
                OpenBrowser("https://www.docker.com/products/docker-desktop");
                throw new Exception("Docker is not installed. Please install Docker Desktop and try again.");
            }

            if (!IsDockerRunning())
            {
                throw new Exception("Docker is installed but not running. Please start Docker Desktop and try again.");
            }

            Console.WriteLine("Docker is installed and running.");
            Console.WriteLine("Local SQL configuration detected. Generating Docker Compose file...");

            var dockerComposeContent = $@"
            version: '3.9'
            services:
              sqlserver:
                image: mcr.microsoft.com/mssql/server:2019-latest
                container_name: umbraco_sqlserver_{dbName}
                environment:
                  - ACCEPT_EULA=Y
                  - SA_PASSWORD={dbPassword}
                ports:
                  - ""{dbPort}:1433""
                volumes:
                  - sqlserverdata_{dbName}:/var/opt/mssql
                restart: unless-stopped
            volumes:
              sqlserverdata_{dbName}:
            ";

            var dockerComposePath = Path.Combine(AppContext.BaseDirectory, "docker-compose.yml");
            File.WriteAllText(dockerComposePath, dockerComposeContent);
            Console.WriteLine("docker-compose.yml generated successfully.");

            Console.WriteLine("Attempting to start Docker Compose...");
            if (!StartDockerCompose(dockerComposePath))
            {
                throw new Exception($"Failed to start Docker Compose. Please follow these steps:\n" +
                                    $"1. Navigate to: {AppContext.BaseDirectory}\n" +
                                    $"2. Run: docker-compose -f '{dockerComposePath}' up -d\n" +
                                    "3. Ensure Docker is running properly and retry if necessary.");
            }

            var connectionString = $"Server=localhost,{dbPort};Database={dbName};User Id=sa;Password={dbPassword};TrustServerCertificate=True;";
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:umbracoDbDSN", connectionString },
                { "ConnectionStrings:umbracoDbDSN_ProviderName", Constants.ProviderNames.SQLServer }
            }!);

            Console.WriteLine("Database connection string successfully set.");
        }
        else
        {
            Console.WriteLine("Use_Local_Docker_SQL is disabled. Skipping docker configuration.");
        }
    }

    private static bool StartDockerCompose(string dockerComposePath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f \"{dockerComposePath}\" up -d",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDockerInstalled()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDockerRunning()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.WriteLine($"Please open your browser and visit: {url}");
        }
    }
}
