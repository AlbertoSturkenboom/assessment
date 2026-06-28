using Core;

namespace Api;

/// <summary>Maps and handles the /api/jobs endpoints.</summary>
public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs", CreateJob);
        app.MapGet("/api/jobs/{id:guid}", GetJob);
        return app;
    }

    private static async Task<IResult> CreateJob(
        CreateJobRequest request,
        IJobQueue queue,
        IJobStore store)
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
    }

    private static IResult GetJob(Guid id, IJobStore store)
        => store.TryGet(id, out var job)
            ? Results.Ok(job)
            : Results.NotFound();
}

public record CreateJobRequest(string Title, string? Payload);
