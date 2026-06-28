using System;
using Core;
using Xunit;

namespace Core.Tests;

public class InMemoryJobStoreTests
{
    [Fact]
    public void Save_ThenTryGet_ReturnsSameJob()
    {
        var store = new InMemoryJobStore();
        var job = new BackgroundJob { Title = "build" };

        store.Save(job);
        var found = store.TryGet(job.Id, out var retrieved);

        Assert.True(found);
        Assert.Same(job, retrieved);
    }

    [Fact]
    public void TryGet_UnknownId_ReturnsFalseAndNull()
    {
        var store = new InMemoryJobStore();

        var found = store.TryGet(Guid.NewGuid(), out var retrieved);

        Assert.False(found);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Save_SameId_OverwritesPreviousState()
    {
        var store = new InMemoryJobStore();
        var job = new BackgroundJob { Title = "build" };
        store.Save(job);

        job.Status = JobStatus.Completed;
        store.Save(job);

        Assert.True(store.TryGet(job.Id, out var retrieved));
        Assert.Equal(JobStatus.Completed, retrieved!.Status);
    }

    [Fact]
    public void Save_Null_Throws()
    {
        var store = new InMemoryJobStore();

        Assert.Throws<ArgumentNullException>(() => store.Save(null!));
    }
}
