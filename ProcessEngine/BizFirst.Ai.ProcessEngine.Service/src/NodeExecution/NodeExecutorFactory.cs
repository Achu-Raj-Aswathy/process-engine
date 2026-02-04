namespace BizFirst.Ai.ProcessEngine.Service.NodeExecution;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

/// <summary>
/// Implementation of INodeExecutorFactory using Strategy pattern.
/// Creates appropriate executor for each node type dynamically.
/// </summary>
public class NodeExecutorFactory : INodeExecutorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _executorRegistry = new();
    private readonly ILogger<NodeExecutorFactory> _logger;

    /// <summary>Initializes a new instance and registers default executors.</summary>
    public NodeExecutorFactory(IServiceProvider serviceProvider, ILogger<NodeExecutorFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RegisterDefaultExecutors();
    }

    /// <inheritdoc/>
    public IProcessElementExecution GetExecutorForNodeType(string nodeTypeName)
    {
        if (string.IsNullOrWhiteSpace(nodeTypeName))
        {
            throw new ArgumentException("Node type name cannot be null or empty", nameof(nodeTypeName));
        }

        if (!_executorRegistry.TryGetValue(nodeTypeName, out var executorType))
        {
            _logger.LogError("No executor registered for node type: {NodeType}", nodeTypeName);
            throw new InvalidOperationException($"No executor registered for node type: {nodeTypeName}");
        }

        try
        {
            var executor = _serviceProvider.GetService(executorType);
            if (executor == null)
            {
                throw new InvalidOperationException($"Failed to resolve executor for node type: {nodeTypeName}");
            }

            return (IProcessElementExecution)executor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating executor for node type: {NodeType}", nodeTypeName);
            throw;
        }
    }

    /// <inheritdoc/>
    public void RegisterExecutor(string nodeTypeName, Type executorType)
    {
        if (!typeof(IProcessElementExecution).IsAssignableFrom(executorType))
        {
            throw new ArgumentException(
                $"Executor type {executorType.Name} must implement IProcessElementExecution",
                nameof(executorType));
        }

        _executorRegistry[nodeTypeName] = executorType;
        _logger.LogInformation("Registered executor for node type: {NodeType}", nodeTypeName);
    }

    /// <summary>Register default built-in executors.</summary>
    private void RegisterDefaultExecutors()
    {
        // Register trigger executors
        RegisterExecutor("manual-trigger", typeof(Executors.Triggers.ManualTriggerExecutor));
        RegisterExecutor("webhook-trigger", typeof(Executors.Triggers.WebhookTriggerExecutor));
        RegisterExecutor("scheduled-trigger", typeof(Executors.Triggers.ScheduledTriggerExecutor));

        // Register decision/logic executors
        RegisterExecutor("if-condition", typeof(Executors.Logic.IfConditionExecutor));
        RegisterExecutor("loop", typeof(Executors.Logic.LoopExecutor));
        RegisterExecutor("switch", typeof(Executors.Logic.SwitchExecutor));
        RegisterExecutor("break", typeof(Executors.Logic.BreakStatementExecutor));
        RegisterExecutor("continue", typeof(Executors.Logic.ContinueStatementExecutor));
        RegisterExecutor("try-block", typeof(Executors.Logic.TryBlockExecutor));
        RegisterExecutor("catch-block", typeof(Executors.Logic.CatchBlockExecutor));
        RegisterExecutor("finally-block", typeof(Executors.Logic.FinallyBlockExecutor));

        // Register action executors
        RegisterExecutor("send-email", typeof(Executors.Actions.SendEmailExecutor));

        // Register workflow executors
        RegisterExecutor("sub-workflow", typeof(Executors.Workflow.SubWorkflowExecutor));
        RegisterExecutor("parallel-fork", typeof(Executors.Workflow.ParallelForkExecutor));
        RegisterExecutor("parallel-join", typeof(Executors.Workflow.ParallelJoinExecutor));

        // Register data transformation executors
        RegisterExecutor("variable-assignment", typeof(Executors.Data.VariableAssignmentExecutor));
        RegisterExecutor("json-transform", typeof(Executors.Data.JsonTransformExecutor));
        RegisterExecutor("data-mapping", typeof(Executors.Data.DataMappingExecutor));
        RegisterExecutor("collection-operation", typeof(Executors.Data.CollectionOperationExecutor));

        // Register integration executors
        RegisterExecutor("http-request", typeof(Executors.Integrations.HttpRequestExecutor));

        _logger.LogInformation("Default executors registration complete. Registered {Count} executors", _executorRegistry.Count);
    }
}
