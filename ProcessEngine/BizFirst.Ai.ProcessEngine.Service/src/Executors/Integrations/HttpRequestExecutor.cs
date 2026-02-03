namespace BizFirst.Ai.ProcessEngine.Service.Executors.Integrations;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for HTTP request integration nodes.
/// Makes HTTP requests to external APIs and returns the response.
/// </summary>
public class HttpRequestExecutor : IIntegrationNodeExecution
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpRequestExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public HttpRequestExecutor(IHttpClientFactory httpClientFactory, ILogger<HttpRequestExecutor> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing HTTP request integration {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var result = await InvokeExternalServiceAsync(executionContext, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HTTP request integration {ElementKey}", executionContext.ProcessElementKey);
            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> InvokeExternalServiceAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);

        // Extract HTTP request parameters
        var url = config.TryGetValue("url", out var u) ? u.ToString() : null;
        var method = config.TryGetValue("method", out var m) ? m.ToString()?.ToUpper() : "GET";
        var headers = config.TryGetValue("headers", out var h) ? (Dictionary<string, object>?)h : null;
        var body = config.TryGetValue("body", out var b) ? b : null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = "URL is required for HTTP request",
                OutputPortKey = "error"
            };
        }

        _logger.LogDebug("Making HTTP {Method} request to {Url}", method, url);

        using var httpClient = _httpClientFactory.CreateClient();

        try
        {
            HttpRequestMessage request = method switch
            {
                "GET" => new HttpRequestMessage(HttpMethod.Get, url),
                "POST" => new HttpRequestMessage(HttpMethod.Post, url),
                "PUT" => new HttpRequestMessage(HttpMethod.Put, url),
                "DELETE" => new HttpRequestMessage(HttpMethod.Delete, url),
                "PATCH" => new HttpRequestMessage(HttpMethod.Patch, url),
                _ => new HttpRequestMessage(HttpMethod.Get, url)
            };

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value?.ToString() ?? "");
                }
            }

            // Add request body for non-GET requests
            if ((method == "POST" || method == "PUT" || method == "PATCH") && body != null)
            {
                var bodyContent = body is string str ? str : JsonSerializer.Serialize(body);
                request.Content = new StringContent(bodyContent, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var outputData = new Dictionary<string, object>
            {
                { "status_code", (int)response.StatusCode },
                { "success", response.IsSuccessStatusCode },
                { "body", responseContent },
                { "headers", new Dictionary<string, string>() }
            };

            // Extract response headers
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            outputData["headers"] = responseHeaders;

            _logger.LogInformation("HTTP request completed with status code {StatusCode}", response.StatusCode);

            return new NodeExecutionResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                OutputData = outputData,
                OutputPortKey = response.IsSuccessStatusCode ? "success" : "error"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"HTTP request failed: {ex.Message}",
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfiguration(validationContext.Definition.Configuration);

        // Validate URL is provided
        if (!config.ContainsKey("url") || string.IsNullOrWhiteSpace(config["url"]?.ToString()))
        {
            return await Task.FromResult(ValidationResult.Failure("URL is required for HTTP request"));
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in HTTP request integration {ElementKey}", executionContext.ProcessElementKey);

        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = error.Message,
            OutputPortKey = "error"
        });
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        // No cleanup needed for HTTP request
        await Task.CompletedTask;
    }

    /// <summary>Parse configuration JSON string to dictionary.</summary>
    private Dictionary<string, object> ParseConfiguration(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            // TODO: Use proper JSON parser (System.Text.Json)
            // For now, return empty dict - configuration parsing should be implemented properly
            return new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
