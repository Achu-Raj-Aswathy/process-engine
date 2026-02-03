namespace BizFirst.Ai.ProcessEngine.Service.ExecutionRouting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;

/// <summary>
/// Implementation of IExecutionRouter.
/// Routes execution flow between nodes based on output ports and conditions.
/// </summary>
public class ExecutionRouter : IExecutionRouter
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<ExecutionRouter> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ExecutionRouter(
        IExpressionEvaluator expressionEvaluator,
        ILogger<ExecutionRouter> logger)
    {
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public List<ProcessElementDefinition> GetDownstreamNodesForOutputPort(
        ProcessElementDefinition sourceElement,
        string outputPortKey,
        ProcessThreadDefinition threadDefinition)
    {
        _logger.LogDebug(
            "Routing from element {ElementKey} via port {PortKey}",
            sourceElement.ProcessElementKey,
            outputPortKey);

        // Find all connections from this element on the specified output port
        var connections = threadDefinition.Connections
            .Where(c => c.SourceProcessElementID == sourceElement.ProcessElementID &&
                       c.SourcePortKey == outputPortKey)
            .ToList();

        _logger.LogDebug(
            "Found {ConnectionCount} connections for element {ElementKey} port {PortKey}",
            connections.Count,
            sourceElement.ProcessElementKey,
            outputPortKey);

        // Map connections to target elements (excluding disabled elements)
        var downstreamElements = new List<ProcessElementDefinition>();
        foreach (var connection in connections)
        {
            var targetElement = threadDefinition.Elements
                .FirstOrDefault(e => e.ProcessElementID == connection.TargetProcessElementID);

            if (targetElement != null && !targetElement.IsDisabled)
            {
                downstreamElements.Add(targetElement);
            }
        }

        return downstreamElements;
    }

    /// <inheritdoc/>
    public async Task<bool> EvaluateConditionalRouteAsync(
        string conditionExpression,
        Dictionary<string, object> executionVariables)
    {
        if (string.IsNullOrWhiteSpace(conditionExpression))
        {
            return true;
        }

        _logger.LogDebug("Evaluating condition: {Condition}", conditionExpression);

        try
        {
            var result = await _expressionEvaluator.EvaluateAsync(
                conditionExpression,
                executionVariables);

            var boolResult = result is bool b ? b : bool.TryParse(result?.ToString(), out var parsed) && parsed;

            _logger.LogDebug("Condition result: {Result}", boolResult);
            return boolResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition: {Condition}", conditionExpression);
            return false;
        }
    }
}
