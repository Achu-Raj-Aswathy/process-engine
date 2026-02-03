namespace BizFirst.Ai.ProcessEngine.Service.Dependencies;

using Microsoft.Extensions.DependencyInjection;
using BizFirst.Ai.AiSession.Service.Dependencies;
using BizFirst.Ai.ProcessEngine.Service.Orchestration;
using BizFirst.Ai.ProcessEngine.Service.NodeExecution;
using BizFirst.Ai.ProcessEngine.Service.ExecutionRouting;
using BizFirst.Ai.ProcessEngine.Service.Definition;
using BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;
using BizFirst.Ai.ProcessEngine.Service.ContextManagement;
using BizFirst.Ai.ProcessEngine.Service.ErrorHandling;
using BizFirst.Ai.ProcessEngine.Service.Persistence;
using BizFirst.Ai.Process.Service;
using BizFirst.Ai.Process.Domain.Interfaces.Repositories;

/// <summary>
/// Dependency injection configuration for ProcessEngine services.
/// Registers all services, repositories, and executors.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add ProcessEngine services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProcessEngineServices(this IServiceCollection services)
    {
        // Add AiSession services first
        services.AddAiSessionServices();

        // Add Process module services (needed for definition loading)
        services.AddProcessServices();

        // Context management
        services.AddScoped<IExecutionContextManager, ExecutionContextManager>();

        // Orchestration
        services.AddScoped<IOrchestrationProcessor, OrchestrationProcessor>();
        services.AddScoped<IProcessElementExecutor, ProcessElementExecutor>();
        services.AddScoped<INodeExecutorFactory, NodeExecutorFactory>();

        // Routing
        services.AddScoped<IExecutionRouter, ExecutionRouter>();

        // Definition loading (uses Process services for data)
        services.AddScoped<IProcessThreadLoader, ProcessThreadLoader>();

        // Expression evaluation
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        // Error handling and retry logic
        services.AddScoped<IExecutionErrorHandler, ExecutionErrorHandler>();

        // Persistence layer (pause/resume/cancel state management)
        services.AddScoped<IExecutionStateService, ExecutionStateService>();
        // TODO: Register repositories when database persistence is implemented
        // services.AddScoped<IExecutionStackStateRepository, ExecutionStackStateRepository>();
        // services.AddScoped<IExecutionMemoryStateRepository, ExecutionMemoryStateRepository>();

        // Future: Add repositories, other services
        // services.AddScoped<IExecutionRepository, ExecutionRepository>();
        // services.AddScoped<IRetryExecutor, RetryExecutor>();
        // etc.

        return services;
    }
}
