using Core;
using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<Greeter>();
builder.Services.AddSingleton<IJobQueue, JobQueue>();
builder.Services.AddHostedService<MainWorker>();

var host = builder.Build();
host.Run();
