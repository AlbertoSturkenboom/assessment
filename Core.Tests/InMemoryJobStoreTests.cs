using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Xunit;

namespace Core.Tests;

public class InMemoryJobStoreTests
{
    [Fact]
    public async Task ConcurrentReadsAndWrites_StayConsistent()
    {
        // Mimics the Api (readers) and the Worker (writers mutating status)
        // hitting the same store and the same job instances simultaneously.
        var store = new InMemoryJobStore();
        var jobs = Enumerable.Range(0, 50).Select(_ => new BackgroundJob()).ToArray();
        foreach (var job in jobs)
        {
            store.Save(job);
        }

        var writers = jobs.Select(job => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                job.Status = JobStatus.Processing;
                store.Save(job);
                job.Status = JobStatus.Completed;
                store.Save(job);
            }
        }));

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                foreach (var job in jobs)
                {
                    if (store.TryGet(job.Id, out var found))
                    {
                        // Reading status concurrently with writers must not throw
                        // and must always be a defined enum value.
                        Assert.True(Enum.IsDefined(found.Status));
                    }
                }
            }
        }));

        await Task.WhenAll(writers.Concat(readers));

        foreach (var job in jobs)
        {
            Assert.True(store.TryGet(job.Id, out var found));
            Assert.Equal(job.Id, found.Id);
            Assert.Equal(JobStatus.Completed, found.Status);
        }
    }

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
