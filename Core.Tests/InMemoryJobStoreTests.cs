namespace Core.Tests;

public class InMemoryJobStoreTests
{
    private readonly InMemoryJobStore _store = new();

    [Fact]
    public void Save_ThenTryGet_ReturnsSameJob()
    {
        var job = TestJobs.Create("build");

        _store.Save(job);

        Assert.True(_store.TryGet(job.Id, out var retrieved));
        Assert.Same(job, retrieved);
    }

    [Fact]
    public void TryGet_UnknownId_ReturnsFalseAndNull()
    {
        var found = _store.TryGet(Guid.NewGuid(), out var retrieved);

        Assert.False(found);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Save_SameId_OverwritesPreviousState()
    {
        var job = TestJobs.Create("build");
        _store.Save(job);

        _store.Save(job.MarkCompleted("done"));

        Assert.True(_store.TryGet(job.Id, out var retrieved));
        Assert.Equal(JobStatus.Completed, retrieved.Status);
    }

    [Fact]
    public void Save_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _store.Save(null!));
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_StayConsistent()
    {
        // Mimics the Api (readers) and the Worker (writers saving new snapshots)
        // hitting the same store and the same job ids simultaneously.
        const int jobCount = 50;
        const int writesPerJob = 500;
        const int readerCount = 4;
        const int readPasses = 2000;

        var jobs = Enumerable.Range(0, jobCount).Select(_ => TestJobs.Create()).ToArray();
        foreach (var job in jobs)
        {
            _store.Save(job);
        }

        var writers = jobs.Select(job => Task.Run(() =>
        {
            for (var i = 0; i < writesPerJob; i++)
            {
                _store.Save(job.MarkProcessing());
                _store.Save(job.MarkCompleted("ok"));
            }
        }));

        var readers = Enumerable.Range(0, readerCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < readPasses; i++)
            {
                foreach (var job in jobs)
                {
                    AssertConsistentSnapshot(job.Id);
                }
            }
        }));

        await Task.WhenAll(writers.Concat(readers));

        foreach (var job in jobs)
        {
            Assert.True(_store.TryGet(job.Id, out var found));
            Assert.Equal(JobStatus.Completed, found.Status);
            Assert.NotNull(found.CompletedAt);
        }
    }

    // A reader must never observe a half-applied update: a Completed snapshot
    // always carries its matching CompletedAt/Result.
    private void AssertConsistentSnapshot(Guid id)
    {
        if (!_store.TryGet(id, out var found))
        {
            return;
        }

        Assert.True(Enum.IsDefined(found.Status));
        if (found.Status == JobStatus.Completed)
        {
            Assert.NotNull(found.CompletedAt);
            Assert.Equal("ok", found.Result);
        }
    }
}
