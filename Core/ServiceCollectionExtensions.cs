using Microsoft.Extensions.DependencyInjection;

namespace Core;

/// <summary>
/// Single source of truth for wiring up the job infrastructure, so every host
/// (Api, Worker, tests) registers the exact same singletons and can never drift.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared job queue and the job store as singletons.
    /// Both must be singletons so producers (Api) and the consumer (Worker)
    /// running in the same process share one queue and one store.
    /// </summary>
    public static IServiceCollection AddJobInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IJobQueue, JobQueue>();
        services.AddSingleton<IJobStore, InMemoryJobStore>();
        return services;
    }
}
