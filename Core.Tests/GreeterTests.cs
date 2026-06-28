using Core;
using Xunit;

namespace Core.Tests;

public class GreeterTests
{
    private readonly Greeter _greeter = new();

    [Fact]
    public void Greet_WithName_ReturnsPersonalizedGreeting()
    {
        var result = _greeter.Greet("Alberto");

        Assert.Equal("Hello, Alberto!", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Greet_WithoutName_FallsBackToWorld(string? name)
    {
        var result = _greeter.Greet(name);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Greet_TrimsSurroundingWhitespace()
    {
        var result = _greeter.Greet("  Rider  ");

        Assert.Equal("Hello, Rider!", result);
    }
}
