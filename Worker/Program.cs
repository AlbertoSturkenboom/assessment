using Core;
using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddJobInfrastructure();
builder.Services.AddHostedService<MainWorker>();

var host = builder.Build();
host.Run();
