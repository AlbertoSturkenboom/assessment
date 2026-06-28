using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Xunit;

namespace Core.Tests;

public class JobQueueTests
{
    private static BackgroundJob JobWithTitle(string title) => new() { Title = title };

    [Fact]
    public async Task EnqueueThenDequeue_ReturnsSameJob()
    {
        var queue = new JobQueue();
        var job = JobWithTitle("first");

        await queue.EnqueueAsync(job);
        var dequeued = await queue.DequeueAsync();

        Assert.Same(job, dequeued);
    }

    [Fact]
    public async Task Dequeue_ReturnsJobsInFifoOrder()
    {
        var queue = new JobQueue();
        await queue.EnqueueAsync(JobWithTitle("a"));
        await queue.EnqueueAsync(JobWithTitle("b"));
        await queue.EnqueueAsync(JobWithTitle("c"));

        var first = await queue.DequeueAsync();
        var second = await queue.DequeueAsync();
        var third = await queue.DequeueAsync();

        Assert.Equal(new[] { "a", "b", "c" },
            new[] { first.Title, second.Title, third.Title });
    }

    [Fact]
    public async Task EnqueueAsync_NullJob_Throws()
    {
        var queue = new JobQueue();

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await queue.EnqueueAsync(null!));
    }

    [Fact]
    public async Task Dequeue_WaitsUntilJobIsEnqueued()
    {
        var queue = new JobQueue();

        // Start waiting before anything is enqueued.
        var dequeueTask = queue.DequeueAsync().AsTask();
        Assert.False(dequeueTask.IsCompleted);

        await queue.EnqueueAsync(JobWithTitle("late"));
        var job = await dequeueTask;

        Assert.Equal("late", job.Title);
    }

    [Fact]
    public async Task Dequeue_WithCancelledToken_ThrowsOperationCanceled()
    {
        var queue = new JobQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(cts.Token));
    }

    [Fact]
    public async Task Dequeue_IsCancelledWhileWaiting()
    {
        var queue = new JobQueue();
        using var cts = new CancellationTokenSource();

        var dequeueTask = queue.DequeueAsync(cts.Token).AsTask();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await dequeueTask);
    }

    [Fact]
    public async Task ConcurrentProducersAndConsumers_AllJobsAreProcessedExactlyOnce()
    {
        var queue = new JobQueue();
        const int producers = 4;
        const int perProducer = 250;
        const int total = producers * perProducer;

        var produce = Enumerable.Range(0, producers).Select(p => Task.Run(async () =>
        {
            for (var i = 0; i < perProducer; i++)
            {
                await queue.EnqueueAsync(JobWithTitle($"{p}-{i}"));
            }
        }));

        var consumed = new ConcurrentBag<string>();

        // Claim exactly `total` dequeues across all consumers. Each call that
        // decrements to >= 0 owns one of the enqueued jobs, so no consumer is
        // left blocking on DequeueAsync after the queue is drained.
        var remaining = total;
        var consume = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
        {
            while (Interlocked.Decrement(ref remaining) >= 0)
            {
                var job = await queue.DequeueAsync();
                consumed.Add(job.Title);
            }
        }));

        await Task.WhenAll(produce.Concat(consume));

        Assert.Equal(total, consumed.Count);
        Assert.Equal(total, consumed.Distinct().Count());
    }
}
