namespace BizFirst.Ai.ProcessEngine.Api.Base.Middleware;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.AiSession.Domain.Session.Accessors;
using BizFirst.Ai.AiSession.Domain.Session.Hierarchy;

/// <summary>
/// Middleware that extracts request context and sets up execution context.
/// Must run early in the pipeline to establish session hierarchy.
/// </summary>
public class ExecutionContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExecutionContextMiddleware> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ExecutionContextMiddleware(RequestDelegate next, ILogger<ExecutionContextMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Invoke the middleware.</summary>
    public async Task InvokeAsync(HttpContext context, IAiSessionContextAccessor contextAccessor)
    {
        try
        {
            // Extract headers or claims for context information
            var tenantID = ExtractTenantID(context);
            var userID = ExtractUserID(context);
            var correlationID = ExtractOrCreateCorrelationID(context);
            var requestID = Guid.NewGuid().ToString();

            // Create session hierarchy
            var platformSession = new PlatformAiSession
            {
                PlatformID = 1,
                PlatformCode = "BizFirst",
                PlatformVersion = "3.0.0"
            };

            var accountSession = new AccountAiSession
            {
                AccountID = 1,
                AccountName = "Default Account",
                Platform = platformSession
            };

            var appSession = new AppAiSession
            {
                AppID = 1,
                AppName = "Default App",
                AppCode = "DEFAULT",
                Account = accountSession
            };

            var userSession = new UserAiSession
            {
                UserID = userID,
                Username = $"User{userID}",
                Email = $"user{userID}@example.com",
                App = appSession,
                AvailableAppIDs = new List<int> { 1 }
            };

            var requestSession = new RequestAiSession
            {
                RequestID = requestID,
                CorrelationID = correlationID,
                User = userSession,
                SourceType = "API",
                TenantID = tenantID
            };

            // Set in accessor so it's available throughout request
            contextAccessor.SetRequestSession(requestSession);

            _logger.LogInformation(
                "ExecutionContext established - RequestID: {RequestID}, CorrelationID: {CorrelationID}, User: {UserID}, Tenant: {TenantID}",
                requestID, correlationID, userID, tenantID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up execution context");
            throw;
        }

        await _next(context);
    }

    /// <summary>Extract tenant ID from request headers or claims.</summary>
    private int ExtractTenantID(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-TenantID", out var tenantIDHeader))
        {
            if (int.TryParse(tenantIDHeader, out var tenantID))
                return tenantID;
        }

        // Default tenant
        return 1;
    }

    /// <summary>Extract user ID from request headers or claims.</summary>
    private int ExtractUserID(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-UserID", out var userIDHeader))
        {
            if (int.TryParse(userIDHeader, out var userID))
                return userID;
        }

        // Default user
        return 1;
    }

    /// <summary>Extract correlation ID from headers or create new one.</summary>
    private string ExtractOrCreateCorrelationID(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-CorrelationID", out var correlationID))
        {
            return correlationID.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
