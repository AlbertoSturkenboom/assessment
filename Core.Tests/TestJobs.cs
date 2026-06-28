using Core;

namespace Core.Tests;

/// <summary>Builders for BackgroundJob test data, to keep tests free of noise.</summary>
internal static class TestJobs
{
    public static BackgroundJob Create(string title = "test") => new() { Title = title };
}
