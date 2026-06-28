using System.Collections.Concurrent;

namespace Core.Tests;

public class JobQueueTests
{
    private readonly JobQueue _queue = new();

    [Fact]
    public async Task EnqueueThenDequeue_ReturnsSameJob()
    {
        var job = TestJobs.Create("first");

        await _queue.EnqueueAsync(job);
        var dequeued = await _queue.DequeueAsync();

        Assert.Same(job, dequeued);
    }

    [Fact]
    public async Task Dequeue_ReturnsJobsInFifoOrder()
    {
        await _queue.EnqueueAsync(TestJobs.Create("a"));
        await _queue.EnqueueAsync(TestJobs.Create("b"));
        await _queue.EnqueueAsync(TestJobs.Create("c"));

        var first = await _queue.DequeueAsync();
        var second = await _queue.DequeueAsync();
        var third = await _queue.DequeueAsync();

        Assert.Equal("a", first.Title);
        Assert.Equal("b", second.Title);
        Assert.Equal("c", third.Title);
    }

    [Fact]
    public async Task EnqueueAsync_NullJob_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _queue.EnqueueAsync(null!));
    }

    [Fact]
    public async Task Dequeue_WaitsUntilJobIsEnqueued()
    {
        // Start waiting before anything is enqueued.
        var dequeueTask = _queue.DequeueAsync().AsTask();
        Assert.False(dequeueTask.IsCompleted);

        await _queue.EnqueueAsync(TestJobs.Create("late"));
        var job = await dequeueTask;

        Assert.Equal("late", job.Title);
    }

    [Fact]
    public async Task Dequeue_WithCancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _queue.DequeueAsync(cts.Token));
    }

    [Fact]
    public async Task Dequeue_IsCancelledWhileWaiting()
    {
        using var cts = new CancellationTokenSource();

        var dequeueTask = _queue.DequeueAsync(cts.Token).AsTask();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await dequeueTask);
    }

    [Fact]
    public async Task ConcurrentProducersAndConsumers_AllJobsAreProcessedExactlyOnce()
    {
        const int producers = 4;
        const int consumers = 3;
        const int perProducer = 250;
        const int total = producers * perProducer;

        var produce = Enumerable.Range(0, producers).Select(p => Task.Run(async () =>
        {
            for (var i = 0; i < perProducer; i++)
            {
                await _queue.EnqueueAsync(TestJobs.Create($"{p}-{i}"));
            }
        }));

        var consumed = new ConcurrentBag<string>();

        // Claim exactly `total` dequeues across all consumers. Each call that
        // decrements to >= 0 owns one of the enqueued jobs, so no consumer is
        // left blocking on DequeueAsync after the queue is drained.
        var remaining = total;
        var consume = Enumerable.Range(0, consumers).Select(_ => Task.Run(async () =>
        {
            while (Interlocked.Decrement(ref remaining) >= 0)
            {
                var job = await _queue.DequeueAsync();
                consumed.Add(job.Title);
            }
        }));

        await Task.WhenAll(produce.Concat(consume));

        Assert.Equal(total, consumed.Count);
        Assert.Equal(total, consumed.Distinct().Count());
    }
}
