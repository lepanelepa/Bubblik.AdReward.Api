var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var rewardsDb = postgres.AddDatabase("addb");
var entitlementsDb = postgres.AddDatabase("entitlementsdb");

builder.AddProject<Projects.Puzyrik_ApiService>("rewards-api")
    .WithReference(rewardsDb)
    .WaitFor(rewardsDb);

builder.AddProject<Projects.Puzyrik_Entitlements>("entitlements-api")
    .WithReference(entitlementsDb)
    .WaitFor(entitlementsDb);

builder.Build().Run();