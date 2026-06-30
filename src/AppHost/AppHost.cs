// Puzyrik.AdReward.AppHost — the orchestration root.
// This file REPLACES the AppHost.cs (or Program.cs) that the Aspire template generates.
//
// What it does: spins up a real PostgreSQL container for local dev, declares a database
// resource, and starts the API with the connection info injected automatically. No manual
// connection strings, no docker-compose. The Aspire dashboard then shows logs/traces/metrics.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL server resource. Aspire pulls docker.io/library/postgres and generates
// credentials for you. WithDataVolume() persists data across `dotnet run` restarts so your
// reward_grants table doesn't vanish every time. WithPgAdmin() is an optional web UI.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Database resource. The name "addb" is the contract: the API references it by this exact
// name (see Api/Program.cs -> AddNpgsqlDataSource("addb")).
var addb = postgres.AddDatabase("addb");

// The API project. WithReference injects the connection info as env vars; WaitFor holds the
// API until Postgres reports healthy, so startup migration doesn't race the DB coming up.
builder.AddProject<Projects.Puzyrik_AdReward_Api>("api")
    .WithReference(addb)
    .WaitFor(addb);

builder.Build().Run();
