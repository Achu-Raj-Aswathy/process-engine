namespace BizFirst.Ai.ProcessEngine.Api.Base.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base controller for process execution endpoints.
/// Provides common functionality for all execution-related API endpoints.
/// </summary>
[ApiController]
[Route("api/v1/process-engine/[controller]")]
public abstract class BaseProcessExecutionController : ControllerBase
{
    /// <summary>
    /// Creates a standardized API response.
    /// </summary>
    protected IActionResult ApiResponse<T>(T data, string? message = null)
    {
        return Ok(new
        {
            success = true,
            data = data,
            message = message
        });
    }

    /// <summary>
    /// Creates a standardized error response.
    /// </summary>
    protected IActionResult ApiError(string errorMessage, int statusCode = 400)
    {
        Response.StatusCode = statusCode;
        return new ObjectResult(new
        {
            success = false,
            error = errorMessage
        });
    }
}
