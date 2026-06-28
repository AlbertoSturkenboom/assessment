using System.Text.Json.Serialization;
using Api;
using Core;
using Worker;

var builder = WebApplication.CreateBuilder(args);

// Shared infrastructure: queue + store as singletons (single source of truth
// in Core), plus the worker hosted in this same process so it always shares
// the same IJobQueue and IJobStore instances as the endpoints.
builder.Services.AddJobInfrastructure();
builder.Services.AddHostedService<MainWorker>();

// Serialize JobStatus as "Pending"/"Processing"/... instead of numbers.
builder.Services.ConfigureHttpJsonOptions(
    options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapJobEndpoints();

app.Run();
