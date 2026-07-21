# GymManagement

C# WPF gym management application using .NET 8 and SQL Server.

## Local database configuration

The SQL Server connection string is read from the `GYM_DB_CONNECTION` environment variable and is intentionally not stored in this repository.

PowerShell example:

```powershell
$env:GYM_DB_CONNECTION = "server=YOUR_SERVER;database=GymManagementDB;uid=YOUR_USER;pwd=YOUR_PASSWORD;TrustServerCertificate=true"
dotnet run
```

The database schema is documented in `docs/06_Technical.md`. Apply the `Members.UserId` changes described there before using member self-service registration.
