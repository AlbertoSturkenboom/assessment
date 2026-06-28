using System.Text.Json.Serialization;
using Core;
using Worker;

var builder = WebApplication.CreateBuilder(args);

// Shared infrastructure: queue + store as singletons (single source of truth
// in Core), plus the worker hosted in this same process so it always shares
// the same IJobQueue and IJobStore instances as the endpoints.
builder.Services.AddJobInfrastructure();
builder.Services.AddHostedService<MainWorker>();

// Serialize JobStatus as "Pending"/"Processing"/... instead of numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Enqueue a new job and store it so its status can be queried later.
app.MapPost("/api/jobs", async (
    CreateJobRequest request,
    IJobQueue queue,
    IJobStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    var job = new BackgroundJob
    {
        Title = request.Title,
        Payload = request.Payload ?? string.Empty
    };

    store.Save(job);
    await queue.EnqueueAsync(job);

    // 202 Accepted: the job is queued, processing happens asynchronously.
    return Results.Accepted($"/api/jobs/{job.Id}", job);
});

// Return the current state of a previously submitted job.
app.MapGet("/api/jobs/{id:guid}", (Guid id, IJobStore store) =>
    store.TryGet(id, out var job)
        ? Results.Ok(job)
        : Results.NotFound());

app.Run();

public record CreateJobRequest(string Title, string? Payload);
