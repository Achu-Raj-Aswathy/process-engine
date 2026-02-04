namespace BizFirst.Ai.ProcessEngine.Service.Caching;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirstFi.Go.Essentials.AppCache.Abstractions;

/// <summary>
/// Service for managing cache invalidation of process definitions.
/// Handles invalidation events when definitions are updated, created, or deleted.
/// </summary>
public class ProcessDefinitionCacheInvalidationService
{
    private readonly IAppCache _cache;
    private readonly ILogger<ProcessDefinitionCacheInvalidationService> _logger;

    // Event handlers for cache invalidation
    public event EventHandler<ProcessDefinitionCacheInvalidationEventArgs>? OnCacheInvalidated;

    /// <summary>Initializes a new instance.</summary>
    public ProcessDefinitionCacheInvalidationService(
        IAppCache cache,
        ILogger<ProcessDefinitionCacheInvalidationService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Invalidates cache for a single process thread definition.</summary>
    /// <param name="processThreadVersionID">Version ID to invalidate.</param>
    /// <param name="reason">Reason for invalidation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateDefinitionAsync(
        int processThreadVersionID,
        string reason = "Manual invalidation",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(processThreadVersionID);
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation(
                "Cache invalidated for process thread version {VersionID}: {Reason}",
                processThreadVersionID, reason);

            // Raise event for subscribers
            OnCacheInvalidated?.Invoke(this, new ProcessDefinitionCacheInvalidationEventArgs
            {
                ProcessThreadVersionID = processThreadVersionID,
                Reason = reason,
                InvalidatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for version {VersionID}", processThreadVersionID);
        }
    }

    /// <summary>Invalidates cache for multiple process thread definitions.</summary>
    /// <param name="processThreadVersionIDs">Version IDs to invalidate.</param>
    /// <param name="reason">Reason for invalidation (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateDefinitionsAsync(
        IEnumerable<int> processThreadVersionIDs,
        string reason = "Batch invalidation",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new List<Task>();

            foreach (var versionId in processThreadVersionIDs)
            {
                tasks.Add(InvalidateDefinitionAsync(versionId, reason, cancellationToken));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Cache invalidated for {Count} process thread versions: {Reason}",
                tasks.Count, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for batch");
        }
    }

    /// <summary>Clears all cached definitions.</summary>
    /// <param name="reason">Reason for clearing all cache (for logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateAllAsync(
        string reason = "Full cache clear",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.ClearAsync(cancellationToken);

            _logger.LogInformation("All process definition cache cleared: {Reason}", reason);

            // Raise event for subscribers
            OnCacheInvalidated?.Invoke(this, new ProcessDefinitionCacheInvalidationEventArgs
            {
                ProcessThreadVersionID = -1, // -1 indicates all
                Reason = reason,
                InvalidatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
        }
    }

    /// <summary>Invalidates cache when definition is created.</summary>
    /// <param name="processThreadVersionID">Version ID that was created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnDefinitionCreatedAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default)
    {
        // New definitions might not be in cache, but invalidate any aggregate caches if applicable
        await InvalidateDefinitionAsync(processThreadVersionID, "Definition created", cancellationToken);
    }

    /// <summary>Invalidates cache when definition is updated.</summary>
    /// <param name="processThreadVersionID">Version ID that was updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnDefinitionUpdatedAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default)
    {
        await InvalidateDefinitionAsync(processThreadVersionID, "Definition updated", cancellationToken);
    }

    /// <summary>Invalidates cache when definition is deleted.</summary>
    /// <param name="processThreadVersionID">Version ID that was deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnDefinitionDeletedAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default)
    {
        await InvalidateDefinitionAsync(processThreadVersionID, "Definition deleted", cancellationToken);
    }

    /// <summary>Invalidates cache when process thread is deployed/published.</summary>
    /// <param name="processThreadID">Process thread ID.</param>
    /// <param name="newVersionID">New version that was deployed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnProcessThreadDeployedAsync(
        int processThreadID,
        int newVersionID,
        CancellationToken cancellationToken = default)
    {
        await InvalidateDefinitionAsync(newVersionID, "Process thread deployed", cancellationToken);
    }

    /// <summary>Gets cache key for a process thread version.</summary>
    private static string GetCacheKey(int processThreadVersionID)
    {
        return $"process_thread_definition_{processThreadVersionID}";
    }
}

/// <summary>Event arguments for cache invalidation events.</summary>
public class ProcessDefinitionCacheInvalidationEventArgs : EventArgs
{
    /// <summary>Process thread version ID that was invalidated (-1 if all).</summary>
    public int ProcessThreadVersionID { get; set; }

    /// <summary>Reason for invalidation.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>When the invalidation occurred.</summary>
    public DateTime InvalidatedAt { get; set; }
}
