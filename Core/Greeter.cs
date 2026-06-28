namespace Core;

/// <summary>
/// Shared greeting logic used by both the Api and the Worker.
/// </summary>
public class Greeter
{
    public string Greet(string? name)
    {
        var target = string.IsNullOrWhiteSpace(name) ? "World" : name.Trim();
        return $"Hello, {target}!";
    }
}
