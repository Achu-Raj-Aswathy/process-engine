namespace BizFirst.Ai.ProcessEngine.Service.Definition;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Exceptions;
using BizFirst.Ai.Process.Domain.Interfaces.Services;
using BizFirst.Ai.Process.Domain.Entities;
using BizFirst.Ai.Process.Domain.WebRequests.ProcessElement;
using BizFirst.Ai.Process.Domain.WebRequests.Connection;
using BizFirstFi.Go.Essentials.Domain.Requests;

/// <summary>
/// Implementation of IProcessThreadLoader.
/// Loads workflow definitions from database using Process module services with caching.
/// </summary>
public class ProcessThreadLoader : IProcessThreadLoader
{
    private readonly IProcessElementService _processElementService;
    private readonly IConnectionService _connectionService;
    private readonly IProcessElementTypeService _processElementTypeService;
    private readonly ILogger<ProcessThreadLoader> _logger;
    private readonly Dictionary<int, ProcessThreadDefinition> _definitionCache = new();

    /// <summary>Initializes a new instance.</summary>
    public ProcessThreadLoader(
        IProcessElementService processElementService,
        IConnectionService connectionService,
        IProcessElementTypeService processElementTypeService,
        ILogger<ProcessThreadLoader> logger)
    {
        _processElementService = processElementService ?? throw new ArgumentNullException(nameof(processElementService));
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _processElementTypeService = processElementTypeService ?? throw new ArgumentNullException(nameof(processElementTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProcessThreadDefinition> LoadProcessThreadAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading ProcessThread for version {VersionID}", processThreadVersionID);

        // Check cache first
        if (_definitionCache.TryGetValue(processThreadVersionID, out var cachedDefinition))
        {
            _logger.LogDebug("Using cached definition for version {VersionID}", processThreadVersionID);
            return cachedDefinition;
        }

        try
        {
            var elements = new List<ProcessElementDefinition>();
            var connections = new List<ConnectionDefinition>();

            _logger.LogDebug("Querying ProcessElementService for ProcessThreadVersionID {VersionID}", processThreadVersionID);

            // Load ProcessElements for this version using the service
            var elementsRequest = new BizFirst.Ai.Process.Domain.WebRequests.ProcessElement.GetByProcessThreadVersionSearchRequest
            {
                ProcessThreadVersionID = processThreadVersionID
            };

            var elementsResponse = await _processElementService.GetByProcessThreadVersionAsync(
                elementsRequest, cancellationToken);

            if (elementsResponse?.Data is IEnumerable<object> elementDataList && elementDataList.Any())
            {
                foreach (var elementData in elementDataList)
                {
                    try
                    {
                        // Cast to ProcessElement
                        if (elementData is ProcessElement element)
                        {
                            _logger.LogDebug("Loading element {ElementKey} of type {TypeID}",
                                element.ProcessElementKey, element.ProcessElementTypeID);

                            // Get element type name
                            string typeName = $"Type_{element.ProcessElementTypeID}";
                            try
                            {
                                var typeRequest = new GetByIdWebRequest
                                {
                                    ID = new IDInfo
                                    {
                                        ID = element.ProcessElementTypeID
                                    }
                                };

                                var typeResponse = await _processElementTypeService.GetByIdAsync(
                                    typeRequest, cancellationToken);

                                if (typeResponse?.Data is ProcessElementType elementType)
                                {
                                    typeName = elementType.Name ?? typeName;
                                }
                            }
                            catch (Exception typeEx)
                            {
                                _logger.LogWarning(typeEx, "Could not load element type {TypeID} for element {ElementKey}",
                                    element.ProcessElementTypeID, element.ProcessElementKey);
                            }

                            elements.Add(new ProcessElementDefinition(element, typeName));
                        }
                    }
                    catch (Exception elementEx)
                    {
                        _logger.LogWarning(elementEx, "Error processing element from service response");
                    }
                }
            }

            _logger.LogDebug("Querying ConnectionService for ProcessThreadVersionID {VersionID}", processThreadVersionID);

            // Load Connections for this version using the service
            var connectionsRequest = new BizFirst.Ai.Process.Domain.WebRequests.Connection.GetByProcessThreadVersionSearchRequest
            {
                ProcessThreadVersionID = processThreadVersionID
            };

            var connectionsResponse = await _connectionService.GetByProcessThreadVersionAsync(
                connectionsRequest, cancellationToken);

            if (connectionsResponse?.Data is IEnumerable<object> connectionDataList && connectionDataList.Any())
            {
                foreach (var connectionData in connectionDataList)
                {
                    try
                    {
                        if (connectionData is Connection connection)
                        {
                            connections.Add(new ConnectionDefinition(connection));
                        }
                    }
                    catch (Exception connectionEx)
                    {
                        _logger.LogWarning(connectionEx, "Error processing connection from service response");
                    }
                }
            }

            _logger.LogInformation(
                "Loaded ProcessThread {ThreadVersionID} with {ElementCount} elements and {ConnectionCount} connections",
                processThreadVersionID, elements.Count, connections.Count);

            // Create thread definition
            var threadDefinition = new ProcessThreadDefinition(null!)
            {
                Elements = elements,
                Connections = connections
            };

            // Cache the definition
            _definitionCache[processThreadVersionID] = threadDefinition;

            return threadDefinition;
        }
        catch (DefinitionLoadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ProcessThread for version {VersionID}", processThreadVersionID);
            throw new DefinitionLoadException(
                $"Failed to load process thread definition for version {processThreadVersionID}", ex);
        }
    }

    /// <summary>Clear the definition cache.</summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing definition cache");
        _definitionCache.Clear();
    }
}
