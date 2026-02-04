namespace BizFirst.Ai.ProcessEngine.Service.Definition;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirstFi.Go.Essentials.AppCache.Abstractions;

/// <summary>
/// Decorator for IProcessThreadLoader that adds L1/L2 caching support.
/// Caches workflow definitions to avoid repeated database/loader calls.
/// </summary>
public class CachedProcessThreadDefinitionLoader : IProcessThreadLoader
{
    private readonly IAppCache _cache;
    private readonly IProcessThreadLoader _innerLoader;
    private readonly ILogger<CachedProcessThreadDefinitionLoader> _logger;

    // Cache configuration
    private const string CACHE_KEY_PREFIX = "process_thread_definition_";
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromHours(24);

    /// <summary>Initializes a new instance with caching.</summary>
    /// <param name="cache">Application cache instance.</param>
    /// <param name="innerLoader">Inner loader to delegate to if cache misses.</param>
    /// <param name="logger">Logger instance.</param>
    public CachedProcessThreadDefinitionLoader(
        IAppCache cache,
        IProcessThreadLoader innerLoader,
        ILogger<CachedProcessThreadDefinitionLoader> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _innerLoader = innerLoader ?? throw new ArgumentNullException(nameof(innerLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProcessThreadDefinition> LoadProcessThreadAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(processThreadVersionID);

        try
        {
            _logger.LogDebug("Loading process thread definition from cache or database: {VersionID}", processThreadVersionID);

            // Use GetOrLoadAsync for automatic cache population
            var definition = await _cache.GetOrLoadAsync<ProcessThreadDefinition>(
                key: cacheKey,
                provider: async ct =>
                {
                    _logger.LogDebug("Cache miss for process thread {VersionID}, loading from database", processThreadVersionID);
                    return await _innerLoader.LoadProcessThreadAsync(processThreadVersionID, ct);
                },
                absoluteExpiration: DateTime.UtcNow.Add(_defaultCacheDuration),
                cancellationToken: cancellationToken
            );

            if (definition == null)
            {
                _logger.LogWarning("Failed to load process thread definition: {VersionID}", processThreadVersionID);
                throw new InvalidOperationException($"Could not load process thread definition with version {processThreadVersionID}");
            }

            _logger.LogInformation("Process thread definition loaded successfully: {VersionID}", processThreadVersionID);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading process thread definition: {VersionID}", processThreadVersionID);
            throw;
        }
    }

    /// <summary>Gets a cache key for a process thread version.</summary>
    private static string GetCacheKey(int processThreadVersionID)
    {
        return $"{CACHE_KEY_PREFIX}{processThreadVersionID}";
    }

    /// <summary>Clears cached definition for a specific version.</summary>
    /// <param name="processThreadVersionID">Version ID to clear from cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateCacheAsync(int processThreadVersionID, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(processThreadVersionID);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogInformation("Cache invalidated for process thread version: {VersionID}", processThreadVersionID);
    }

    /// <summary>Clears all cached definitions.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        await _cache.ClearAsync(cancellationToken);
        _logger.LogInformation("All process thread definition cache cleared");
    }
}
