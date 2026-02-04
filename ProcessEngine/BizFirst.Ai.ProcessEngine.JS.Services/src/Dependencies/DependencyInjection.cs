namespace BizFirst.Ai.ProcessEngine.JS.Services.Dependencies;

using Microsoft.Extensions.DependencyInjection;
using BizFirst.Ai.ProcessEngine.JS.Services.ExpressionEngine;
using BizFirst.Ai.ProcessEngine.JS.Services.Security;

/// <summary>
/// Dependency injection configuration for JavaScript services.
/// Registers expression evaluation and security services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add JavaScript expression services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJavaScriptServices(this IServiceCollection services)
    {
        // Expression evaluation
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        // Node certification and security
        services.AddScoped<INodeCertificationService, NodeCertificationService>();

        return services;
    }
}
