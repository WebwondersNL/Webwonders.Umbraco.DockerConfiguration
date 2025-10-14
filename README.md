# Webwonders.Umbraco.DockerConfiguration

`Webwonders.Umbraco.DockerConfiguration` is a lightweight NuGet package to simplify Docker-based SQL Server configuration for Umbraco 13+ projects. It automates Docker setup, connection string generation, and ensures compatibility with Umbraco.

## Features
- Detects and validates Docker installation and status.
- Generates and starts a Docker Compose file for SQL Server.
- Automatically configures the Umbraco connection string.
- Supports seamless integration with `launchSettings.json`.

## Installation
To install the package, run:

```bash
Install-Package Webwonders.Umbraco.DockerConfiguration
```

Or, using the .NET CLI:

```bash
dotnet add package Webwonders.Umbraco.DockerConfiguration
```

## Usage
Add the middleware to your `IConfigurationBuilder` in `Program.cs`:

```csharp
using Webwonders.Umbraco.DockerConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Enable Docker-based SQL Server configuration
DockerConfigurationMiddleware.ConfigureDockerSqlDb(builder.Configuration);

// Continue building the app...
```

### Prerequisites
1. Ensure Docker Desktop is installed and running.
2. Add the following environment variables to `launchSettings.json` under the `UmbracoProject` profile:

```json
"environmentVariables": {
  "Use_Local_Docker_SQL": "true",
  "Local_Docker_PROJECT_NAME": "MyProject",
  "Local_Docker_CONTAINER_DIRECTORY_OVERRIDE": "C:\\a\\path\\to\\goto",
  "Local_Docker_DB_NAME": "MyDatabase",
  "Local_Docker_PASSWORD": "YourStrongPassword123",
  "Local_Docker_PORT": "1433"
}
```

## How It Works
1. **Docker Validation**: Checks if Docker is installed and running.
2. **Docker Compose Generation**: Creates a `docker-compose.yml` file for SQL Server.
3. **Connection String Configuration**: Adds the SQL Server connection string dynamically to the Umbraco configuration.

## Contributing
Contributions are welcome! Please submit a pull request or open an issue on GitHub.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
