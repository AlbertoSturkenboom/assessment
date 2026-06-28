using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Core;

var builder = WebApplication.CreateBuilder(args);

// Shared infrastructure.
builder.Services.AddSingleton<Greeter>();
builder.Services.AddSingleton<IJobQueue, JobQueue>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, BackgroundJob>>();

// Serialize JobStatus as "Pending"/"Processing"/... instead of numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.MapGet("/", (Greeter greeter) => greeter.Greet(null));
app.MapGet("/hello/{name}", (string name, Greeter greeter) => greeter.Greet(name));

// Enqueue a new job and store it so its status can be queried later.
app.MapPost("/api/jobs", async (
    CreateJobRequest request,
    IJobQueue queue,
    ConcurrentDictionary<Guid, BackgroundJob> store) =>
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

    store[job.Id] = job;
    await queue.EnqueueAsync(job);

    // 202 Accepted: the job is queued, processing happens asynchronously.
    return Results.Accepted($"/api/jobs/{job.Id}", job);
});

// Return the current state of a previously submitted job.
app.MapGet("/api/jobs/{id:guid}", (
    Guid id,
    ConcurrentDictionary<Guid, BackgroundJob> store) =>
        store.TryGetValue(id, out var job)
            ? Results.Ok(job)
            : Results.NotFound());

app.Run();

public record CreateJobRequest(string Title, string? Payload);
