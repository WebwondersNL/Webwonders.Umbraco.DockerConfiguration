# Webwonders.Umbraco.DockerConfiguration

`Webwonders.Umbraco.DockerConfiguration` is a lightweight NuGet package that automatically configures a Docker-based SQL Server for **Umbraco 13+**. This package requires **no changes** to your `Program.cs`. This package is only intended for **local development** scenarios, where you need a cross-platform way to spin up a SQL Server instance inside Docker.

## Features
- **Zero Touch**: No modifications needed in `Program.cs` or any Umbraco composer.
- **Docker Validation**: Checks if Docker is installed and running.
- **Docker Compose Generation**: Creates a `docker-compose.yml` for SQL Server on demand.
- **Automatic Connection String**: Dynamically configures Umbraco’s database connection for local development.

## Installation

Install the package via NuGet:

```bash
Install-Package Webwonders.Umbraco.DockerConfiguration
```

Or the .NET CLI:

```bash
dotnet add package Webwonders.Umbraco.DockerConfiguration
```

That’s it! This package automatically configures everything for local Docker usage.

## Configuration

To **enable** a local Docker-based SQL Server, set the following environment variables in your development environment. Typically, you can place these in `launchSettings.json` (used only in local dev). Example:

```json
{
  "profiles": {
    "UmbracoProject": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "Use_Local_Docker_SQL": "true",
        "Local_Docker_DB_NAME": "YourDatabaseName",
        "Local_Docker_PASSWORD": "YourStrongPassword123",
        "Local_Docker_PORT": "1433"
      }
    }
  }
}
```

### Environment Variables
- **`Use_Local_Docker_SQL`**  
  Set to "true" to enable local Docker-based SQL; "false" (or unset) to disable.
- **`Local_Docker_DB_NAME`**  
  Name of the SQL Server database to create in Docker.
- **`Local_Docker_PASSWORD`**  
  Password for the `sa` user.
- **`Local_Docker_PORT`**  
  Host port mapping for the SQL Server container.

> **Note**: This package is intended for local development scenarios.

## How It Works
1. **IHostingStartup Discovery**  
   ASP.NET Core automatically discovers this package’s startup logic, so **no code changes** are required in `Program.cs`.
2. **Docker Validation**  
   Checks if Docker is installed and running locally.
3. **Docker Compose**  
   If enabled (`Use_Local_Docker_SQL=true`), creates and runs a SQL Server container via Docker Compose.
4. **Connection String Setup**  
   Injects the correct connection string into Umbraco’s configuration, ready for local development.

## Contributing
Contributions are welcome! Submit a pull request or open an issue on GitHub.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
