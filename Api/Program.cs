using Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Greeter>();

var app = builder.Build();

app.MapGet("/", (Greeter greeter) => greeter.Greet(null));
app.MapGet("/hello/{name}", (string name, Greeter greeter) => greeter.Greet(name));

app.Run();
