# GymManagement

C# WPF gym management application using .NET 8 and SQL Server.

## Local database configuration

The SQL Server connection string is read from the local `appsettings.local.json` file, which is ignored by Git and is intentionally not stored in this repository.

Create `appsettings.local.json` beside the `.csproj` file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=YOUR_SERVER;database=GymManagementDB;uid=YOUR_USER;pwd=YOUR_PASSWORD;TrustServerCertificate=true"
  }
}
```

You can copy `appsettings.example.json` and rename it to `appsettings.local.json`, then replace the placeholders. The environment variable `GYM_DB_CONNECTION` remains supported as a fallback.

The database schema is documented in `docs/06_Technical.md`. Apply the `Members.UserId` changes described there before using member self-service registration.
