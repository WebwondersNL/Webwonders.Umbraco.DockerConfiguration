using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
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
        var dbName = Environment.GetEnvironmentVariable("Local_Docker_DB_NAME");
        var dbPassword  = Environment.GetEnvironmentVariable("Local_Docker_PASSWORD");
        var dbPort      = Environment.GetEnvironmentVariable("Local_Docker_PORT");
        
        //Override to handle breaking change introduced in 16.1
        var projectRootOverride = Environment.GetEnvironmentVariable("Local_Docker_CONTAINER_DIRECTORY_OVERRIDE");
        var projectRoot = !string.IsNullOrEmpty(projectRootOverride)
            ? projectRootOverride
            : ResolveProjectRoot(AppContext.BaseDirectory) 
              ?? Directory.GetCurrentDirectory();
        
        var defaultProjectName = Path.GetFileName(projectRoot);
        var rawProjectName = Environment.GetEnvironmentVariable("Local_Docker_PROJECT_NAME") ?? defaultProjectName;
        var projectName = SanitizeComposeProjectName(rawProjectName, "umbraco");

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

            
            var composeDir = Path.Combine(projectRoot, ".docker");
            Directory.CreateDirectory(composeDir);

            var dockerComposePath = Path.Combine(composeDir, "docker-compose.yml");
            File.WriteAllText(dockerComposePath, dockerComposeContent);
            Console.WriteLine("docker-compose.yml generated successfully.");

            Console.WriteLine("Attempting to start Docker Compose...");

            StartDockerComposeOrThrow(dockerComposePath, projectName, projectRoot);
            
            
            var connectionString = $"Server=127.0.0.1,{dbPort};Database={dbName};User Id=sa;Password={dbPassword};TrustServerCertificate=True;Encrypt=false;";
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
    
    private static (bool ok, int exitCode, string stdout, string stderr, string command) TryRun(string fileName, string arguments, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = new Process { StartInfo = psi };
            p.Start();

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();

            p.WaitForExit();

            return (p.ExitCode == 0, p.ExitCode, stdout, stderr, $"{fileName} {arguments}");
        }
        catch (Exception ex)
        {
            return (false, -1, "", ex.ToString(), $"{fileName} {arguments}");
        }
    }

    private static void StartDockerComposeOrThrow(string dockerComposePath, string projectName, string workingDir)
    {
        // Prefer v2: `docker compose ...`
        var v2 = TryRun("docker", $"compose -p \"{projectName}\" -f \"{dockerComposePath}\" up -d", workingDir);
        if (v2.ok) return;

        // Fallback to v1: `docker-compose ...`
        var v1 = TryRun("docker-compose", $"-p \"{projectName}\" -f \"{dockerComposePath}\" up -d", workingDir);
        if (v1.ok) return;

        // Throw with REAL diagnostics
        throw new Exception(
            "Failed to start Docker Compose.\n\n" +
            $"Tried:\n1) {v2.command}\n2) {v1.command}\n\n" +
            $"docker compose exit={v2.exitCode}\nSTDERR:\n{v2.stderr}\nSTDOUT:\n{v2.stdout}\n\n" +
            $"docker-compose exit={v1.exitCode}\nSTDERR:\n{v1.stderr}\nSTDOUT:\n{v1.stdout}\n"
        );
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
    
    private static string? ResolveProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir != null)
        {
            // Heuristics: any of these indicate a repo/solution/project root
            var hasCsproj = dir.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any();
            var hasSln    = dir.EnumerateFiles("*.sln",   SearchOption.TopDirectoryOnly).Any();
            var hasGit    = Directory.Exists(Path.Combine(dir.FullName, ".git"));

            if (hasCsproj || hasSln || hasGit)
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
    
    
    private static string SanitizeComposeProjectName(string? value, string fallback)
    {
        var v = string.IsNullOrWhiteSpace(value) ? fallback : value;

        // lower-case + replace invalid chars with underscores
        v = v.ToLowerInvariant();
        v = Regex.Replace(v, @"[^a-z0-9_-]", "_");

        // must start with a letter/number
        v = Regex.Replace(v, @"^[^a-z0-9]+", "");

        // avoid empty result
        if (string.IsNullOrWhiteSpace(v))
            v = fallback.ToLowerInvariant();

        return v;
    }
}
