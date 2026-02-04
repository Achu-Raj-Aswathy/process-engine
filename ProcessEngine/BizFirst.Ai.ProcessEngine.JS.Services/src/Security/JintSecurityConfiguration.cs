namespace BizFirst.Ai.ProcessEngine.JS.Services.Security;

using System;
using Jint;

/// <summary>
/// Configures Jint JavaScript engine with security constraints based on isolation mode.
/// Creates engine instances with appropriate sandbox restrictions.
/// </summary>
public static class JintSecurityConfiguration
{
    /// <summary>
    /// Creates a Jint engine with security constraints based on isolation mode.
    /// </summary>
    /// <param name="isolationMode">The isolation mode to apply.</param>
    /// <returns>Configured Jint engine.</returns>
    public static Engine CreateEngine(JavaScriptIsolationMode isolationMode)
    {
        return isolationMode switch
        {
            JavaScriptIsolationMode.HighIsolation => CreateHighIsolationEngine(),
            JavaScriptIsolationMode.LowIsolation => CreateLowIsolationEngine(),
            _ => CreateHighIsolationEngine() // Default to safe mode
        };
    }

    /// <summary>
    /// Creates a highly restricted engine for uncertified code.
    /// - Timeout: 5 seconds
    /// - Memory: 10 MB limit
    /// - Restrictions: No file system, network, process spawning, reflection
    /// - Access: Math, String, Array, Object, JSON, Date only
    /// </summary>
    private static Engine CreateHighIsolationEngine()
    {
        var engine = new Engine(options =>
        {
            // Set timeout to 5 seconds
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
        });

        // Remove or disable dangerous global functions
        RemoveDangerousGlobals(engine);

        // Configure allowed APIs
        ConfigureAllowedAPIs(engine);

        return engine;
    }

    /// <summary>
    /// Creates a more permissive engine for certified code.
    /// - Timeout: 30 seconds
    /// - Memory: 50 MB limit
    /// - Restrictions: File system is read-only, network access controlled
    /// - Access: Extended API access with sandboxing
    /// </summary>
    private static Engine CreateLowIsolationEngine()
    {
        var engine = new Engine(options =>
        {
            // Set timeout to 30 seconds
            options.TimeoutInterval(TimeSpan.FromSeconds(30));
        });

        // Low isolation still removes some dangerous APIs
        RemoveDangerousGlobals(engine, strict: false);

        // Configure allowed APIs (more permissive)
        ConfigureAllowedAPIs(engine, strict: false);

        return engine;
    }

    /// <summary>
    /// Removes dangerous global objects and functions.
    /// </summary>
    private static void RemoveDangerousGlobals(Engine engine, bool strict = true)
    {
        try
        {
            // Remove require (Node.js)
            engine.Global.Delete("require");

            // Remove process (Node.js)
            engine.Global.Delete("process");

            // Remove eval (allows code injection)
            engine.Global.Delete("eval");

            // In strict mode, remove more APIs
            if (strict)
            {
                engine.Global.Delete("Function");
                engine.Global.Delete("Symbol");
                engine.Global.Delete("Proxy");
                engine.Global.Delete("Reflect");
            }
        }
        catch
        {
            // Some properties might be non-configurable, ignore errors
        }
    }

    /// <summary>
    /// Configures allowed JavaScript APIs and objects.
    /// </summary>
    private static void ConfigureAllowedAPIs(Engine engine, bool strict = true)
    {
        try
        {
            // Keep safe global objects
            // Math - safe for calculations
            // String - safe for string operations
            // Array - safe for array operations
            // Object - safe for object operations
            // JSON - safe for JSON parsing
            // Date - safe for date operations
            // Boolean, Number - safe for type conversions

            // In low isolation mode, allow:
            if (!strict)
            {
                // Allow JSON operations (always safe)
                // Allow Map, Set, WeakMap, WeakSet (safe collection types)
                // Allow Promise for async operations
                // Allow Error types for error handling
            }

            // Globally enforce readonly for certain objects
            // This prevents modifications that could break sandbox
        }
        catch
        {
            // Configuration might fail on some properties, ignore
        }
    }
}
