using System;
using Core;
using Xunit;

namespace Core.Tests;

public class BackgroundJobTests
{
    [Fact]
    public void NewJob_HasNonEmptyId()
    {
        var job = new BackgroundJob();

        Assert.NotEqual(Guid.Empty, job.Id);
    }

    [Fact]
    public void NewJob_GeneratesUniqueIds()
    {
        var first = new BackgroundJob();
        var second = new BackgroundJob();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void NewJob_DefaultsToPending()
    {
        var job = new BackgroundJob();

        Assert.Equal(JobStatus.Pending, job.Status);
    }

    [Fact]
    public void NewJob_DefaultsTitleAndPayloadToEmpty()
    {
        var job = new BackgroundJob();

        Assert.Equal(string.Empty, job.Title);
        Assert.Equal(string.Empty, job.Payload);
    }

    [Fact]
    public void NewJob_CreatedAtIsRecentUtc()
    {
        var before = DateTime.UtcNow;
        var job = new BackgroundJob();
        var after = DateTime.UtcNow;

        Assert.InRange(job.CreatedAt, before, after);
        Assert.Equal(DateTimeKind.Utc, job.CreatedAt.Kind);
    }

    [Fact]
    public void Properties_CanBeAssigned()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var job = new BackgroundJob
        {
            Id = id,
            Title = "Send email",
            Payload = "{\"to\":\"user@example.com\"}",
            Status = JobStatus.Completed,
            CreatedAt = createdAt
        };

        Assert.Equal(id, job.Id);
        Assert.Equal("Send email", job.Title);
        Assert.Equal("{\"to\":\"user@example.com\"}", job.Payload);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(createdAt, job.CreatedAt);
    }

    [Theory]
    [InlineData(JobStatus.Pending)]
    [InlineData(JobStatus.Processing)]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Failed)]
    public void Status_AcceptsEachJobStatusValue(JobStatus status)
    {
        var job = new BackgroundJob { Status = status };

        Assert.Equal(status, job.Status);
    }
}
