namespace BizFirst.Ai.ProcessEngine.Service.DependencyInjection;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BizFirst.Ai.ProcessEngine.Service.Definition;
using BizFirst.Ai.ProcessEngine.Service.Caching;
using BizFirstFi.Go.Essentials.AppCache.Extensions;

/// <summary>
/// Extension methods for registering ProcessEngine services with caching support.
/// </summary>
public static class ProcessEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers ProcessEngine services with tiered caching (L1 Memory + L2 Redis).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddProcessEngineServicesWithCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Get Redis connection string from configuration
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? "localhost:6379";

        // Register tiered cache (L1 + L2)
        services.AddTieredAppCache(
            redisConnectionString: redisConnectionString,
            memoryOptions: opts =>
            {
                opts.SizeLimit = 104857600; // 100MB L1 cache
            }
        );

        // Register cache invalidation service
        services.AddScoped<ProcessDefinitionCacheInvalidationService>();

        // Register the inner loader first (unwrapped)
        services.AddScoped<ProcessThreadLoader>();

        // Register the cached definition loader
        services.AddScoped<IProcessThreadLoader>(sp =>
        {
            // Get the inner loader - this should be the real database loader
            var innerLoader = sp.GetRequiredService<ProcessThreadLoader>();
            var cache = sp.GetRequiredService<BizFirstFi.Go.Essentials.AppCache.Abstractions.IAppCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedProcessThreadDefinitionLoader>>();

            return new CachedProcessThreadDefinitionLoader(cache, innerLoader, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers ProcessEngine services with memory-only caching (for testing).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddProcessEngineServicesWithMemoryCache(
        this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register memory cache only
        services.AddMemoryAppCache(opts =>
        {
            opts.SizeLimit = 52428800; // 50MB L1 cache
        });

        // Register cache invalidation service
        services.AddScoped<ProcessDefinitionCacheInvalidationService>();

        // Register the inner loader first (unwrapped)
        services.AddScoped<ProcessThreadLoader>();

        // Register the cached definition loader
        services.AddScoped<IProcessThreadLoader>(sp =>
        {
            var innerLoader = sp.GetRequiredService<ProcessThreadLoader>();
            var cache = sp.GetRequiredService<BizFirstFi.Go.Essentials.AppCache.Abstractions.IAppCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedProcessThreadDefinitionLoader>>();

            return new CachedProcessThreadDefinitionLoader(cache, innerLoader, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers ProcessEngine services without caching (for development/debugging).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddProcessEngineServicesWithoutCache(
        this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register dummy cache (no-op)
        services.AddDummyAppCache();

        // Register cache invalidation service (still registered but won't do anything)
        services.AddScoped<ProcessDefinitionCacheInvalidationService>();

        // Register the inner loader first (unwrapped)
        services.AddScoped<ProcessThreadLoader>();

        // Register the cached definition loader (will always load from database)
        services.AddScoped<IProcessThreadLoader>(sp =>
        {
            var innerLoader = sp.GetRequiredService<ProcessThreadLoader>();
            var cache = sp.GetRequiredService<BizFirstFi.Go.Essentials.AppCache.Abstractions.IAppCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedProcessThreadDefinitionLoader>>();

            return new CachedProcessThreadDefinitionLoader(cache, innerLoader, logger);
        });

        return services;
    }
}
