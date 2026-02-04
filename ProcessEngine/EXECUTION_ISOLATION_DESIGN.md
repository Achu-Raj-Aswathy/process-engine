# JavaScript Execution Isolation Design Document

**Status**: Design Document (For Future Implementation)
**Priority**: CRITICAL - Security Feature
**Created**: 2026-02-03

---

## Executive Summary

This document outlines the design for implementing **JavaScript Expression Isolation** in the ProcessEngine. The goal is to prevent malicious JavaScript code from accessing sensitive system resources unless the node is certified by the provider.

Two execution modes are proposed:
- **High Isolation** (default): Sandboxed, restricted access - for uncertified nodes
- **Low Isolation** (certified only): More permissive access - for provider-certified nodes

---

## Problem Statement

**Current Security Vulnerability**: `ExpressionEvaluator.cs:38`

```csharp
var engine = new Engine();  // ← NO RESTRICTIONS!
```

JavaScript expressions are evaluated with unrestricted access, allowing:
- File system read/write access
- Network connectivity
- Process spawning
- Reflection and CLR type access

This creates a security risk when uncertified connectors/nodes execute JavaScript expressions.

---

## Solution Architecture

### 1. Isolation Modes

**High Isolation Mode** (Uncertified nodes - DEFAULT)
| Feature | Status |
|---------|--------|
| Timeout | 5 seconds |
| Memory Limit | 10 MB |
| File System | Blocked |
| Network Access | Blocked |
| Process Spawning | Blocked |
| Reflection | Blocked |
| CLR Type Access | Blocked |
| Whitelisted APIs | Math, String, Array, Object, JSON, Date |

**Low Isolation Mode** (Certified nodes only)
| Feature | Status |
|---------|--------|
| Timeout | 30 seconds |
| Memory Limit | 50 MB |
| File System | Read-only workspace |
| Network Access | To approved endpoints |
| Process Spawning | Blocked |
| Reflection | Limited |
| CLR Type Access | Limited |
| Extended APIs | Available with sandboxing |

### 2. Certificate Validation

Certificates are stored in `ProcessElement.Configuration` JSON field.

**Expected Schema**:
```json
{
  "certificate": {
    "issued": true,
    "issuedBy": "ProviderName",
    "certificateType": "Trusted|Verified|Certified",
    "certificateHash": "sha256_hash",
    "issuedAt": "2026-01-01T00:00:00Z",
    "expiresAt": "2026-12-31T00:00:00Z"
  },
  "other_config": "..."
}
```

**Validation Criteria** (ALL must be true for Low Isolation):
1. `certificate.issued` = true
2. `certificate.issuedBy` is not empty
3. `certificate.certificateType` in ["Trusted", "Verified", "Certified"]
4. `certificate.expiresAt` is in the future (not expired)
5. Optional: `certificate.certificateHash` validates against known certificates

### 3. Component Architecture

```
ProcessElementExecutor
    ↓
IfConditionExecutor / SwitchExecutor / LoopExecutor / ExecutionRouter
    ↓
ExpressionEvaluator
    ├── NodeCertificationService
    │   └── Parses ProcessElement.Configuration JSON
    │       └── Returns JavaScriptIsolationMode
    │
    └── JintSecurityConfiguration
        ├── CreateEngine(HighIsolation)
        │   └── Configure Jint with sandbox constraints
        │
        └── CreateEngine(LowIsolation)
            └── Configure Jint with extended permissions
```

---

## Implementation Components

### Component 1: JavaScriptIsolationMode Enum

**File**: `BizFirst.Ai.ProcessEngine.Service/src/Security/JavaScriptIsolationMode.cs`

```csharp
namespace BizFirst.Ai.ProcessEngine.Service.Security;

/// <summary>
/// JavaScript isolation modes for expression execution.
/// Based on node certification by provider.
/// </summary>
public enum JavaScriptIsolationMode
{
    /// <summary>High isolation mode for uncertified nodes (default, sandboxed)</summary>
    HighIsolation = 0,

    /// <summary>Low isolation mode for certified nodes only (more permissive)</summary>
    LowIsolation = 1
}
```

### Component 2: JintSecurityConfiguration

**File**: `BizFirst.Ai.ProcessEngine.Service/src/Security/JintSecurityConfiguration.cs`

Creates and configures Jint Engine with appropriate security constraints.

**Key Methods**:
- `CreateEngine(JavaScriptIsolationMode mode)` - Factory method
- `ConfigureHighIsolation(Engine engine)` - Apply sandbox constraints
- `ConfigureLowIsolation(Engine engine)` - Apply extended constraints

**Implementation Approach**:
- Use Jint `Engine.Options` to configure constraints
- Disable global functions (process, require, import, eval, etc.)
- Set memory limits using Jint's memory management
- Implement timeout enforcement
- Whitelist only safe API objects

### Component 3: NodeCertificationService

**File**: `BizFirst.Ai.ProcessEngine.Service/src/Security/INodeCertificationService.cs`
**File**: `BizFirst.Ai.ProcessEngine.Service/src/Security/NodeCertificationService.cs`

Determines isolation mode by checking node certificate.

**Interface**:
```csharp
public interface INodeCertificationService
{
    Task<JavaScriptIsolationMode> GetIsolationModeAsync(
        ProcessElementDefinition elementDefinition,
        CancellationToken cancellationToken = default);
}
```

**Implementation Logic**:
1. Parse `elementDefinition.Configuration` as JSON
2. Extract `certificate` object
3. Validate all criteria (issued, issuedBy, certificateType, expiresAt)
4. Return LowIsolation if ALL criteria met, else HighIsolation

### Component 4: Updated ExpressionEvaluator

**File**: `BizFirst.Ai.ProcessEngine.Service/src/ExpressionEngine/ExpressionEvaluator.cs`

**Changes**:
1. Inject `INodeCertificationService`
2. Add `ProcessElementDefinition elementDefinition` parameter to `EvaluateAsync`
3. Determine isolation mode via service
4. Create Engine with appropriate configuration
5. Execute expression in configured environment

**Updated Signature**:
```csharp
Task<object?> EvaluateAsync(
    string expression,
    Dictionary<string, object> executionVariables,
    ProcessElementDefinition elementDefinition = null)
```

**Implementation Pattern**:
```csharp
// Default to High Isolation if no element definition
var isolationMode = JavaScriptIsolationMode.HighIsolation;

if (elementDefinition != null)
{
    isolationMode = await _certificationService.GetIsolationModeAsync(elementDefinition);
}

var engine = JintSecurityConfiguration.CreateEngine(isolationMode);
// ... execute expression
```

### Component 5: Executor Updates

Update all executors that call ExpressionEvaluator:

**Files**:
- `IfConditionExecutor.cs`
- `SwitchExecutor.cs`
- `LoopExecutor.cs`
- `ExecutionRouter.cs`
- `JsonTransformExecutor.cs`

**Pattern**:
```csharp
var result = await _expressionEvaluator.EvaluateAsync(
    expression,
    executionVariables,
    elementDefinition: executionContext.ElementDefinition
);
```

---

## Data Flow

### Node Execution with JavaScript Expression

```
1. Node Executor calls ExpressionEvaluator.EvaluateAsync()
   ↓
2. ExpressionEvaluator.EvaluateAsync(expression, variables, elementDefinition)
   ↓
3. Call NodeCertificationService.GetIsolationModeAsync(elementDefinition)
   ↓
4. Parse ProcessElement.Configuration JSON
   ↓
5. Validate certificate (issued, issuedBy, certificateType, expiresAt)
   ↓
6. Return LowIsolation (if certified) or HighIsolation (default)
   ↓
7. Call JintSecurityConfiguration.CreateEngine(isolationMode)
   ↓
8. Configure Jint Engine with appropriate constraints
   ↓
9. Execute expression in sandboxed environment
   ↓
10. Return result to calling executor
```

---

## Test Cases

### Security Tests

1. **Node without certificate** → Should execute in High Isolation
2. **Node with valid certificate** → Should execute in Low Isolation
3. **Node with expired certificate** → Should fall back to High Isolation
4. **Node with invalid certificateType** → Should use High Isolation
5. **File system access in High Isolation** → Should be blocked
6. **Network access in High Isolation** → Should be blocked
7. **Timeout in High Isolation** → Should enforce 5 second limit
8. **Timeout in Low Isolation** → Should enforce 30 second limit
9. **Memory limit in High Isolation** → Should enforce 10 MB limit
10. **CLR type access in High Isolation** → Should be blocked

### Integration Tests

1. **End-to-end workflow** with uncertified node executing JavaScript
2. **End-to-end workflow** with certified node executing JavaScript
3. **Sub-workflow** with nodes in different isolation modes
4. **Error scenarios** (timeout, memory exceeded, security violation)
5. **Certificate expiration** during workflow execution

---

## Configuration Schema

Create documentation:
**File**: `BizFirst.Ai.ProcessEngine.Service/src/Security/NODE_CERTIFICATION_SCHEMA.md`

Detailed specification of expected JSON structure in `ProcessElement.Configuration`:

```json
{
  "certificate": {
    "issued": true,
    "issuedBy": "TrustedProvider",
    "certificateType": "Certified",
    "certificateHash": "sha256:abc123...",
    "issuedAt": "2026-01-01T00:00:00Z",
    "expiresAt": "2027-01-01T00:00:00Z"
  }
}
```

---

## Security Considerations

### Attack Vectors Mitigated

| Vector | Mitigation |
|--------|-----------|
| File system access | Blocked in High Isolation, read-only in Low |
| Network access | Blocked in High Isolation, whitelisted in Low |
| Process spawning | Blocked in both modes |
| Infinite loops | 5s timeout (High), 30s timeout (Low) |
| Memory exhaustion | 10MB limit (High), 50MB limit (Low) |
| Reflection/CLR access | Blocked in both modes |
| Code injection | Jint sandbox prevents access to dangerous APIs |

### Assumptions

1. Jint security options can effectively sandbox JavaScript
2. Certificate information is trustworthy (stored securely)
3. ProcessElement.Configuration is immutable during execution
4. Low Isolation nodes are pre-vetted by provider

### Limitations

1. Jint may not support all desired restrictions (needs testing)
2. Certificate structure defined in code, not database schema
3. No certificate revocation checking (CRL)
4. No audit logging of security violations
5. Low Isolation mode still has some restrictions (intentional)

---

## Performance Implications

- **High Isolation**: 5 second timeout limit may be restrictive for complex computations
- **Low Isolation**: 30 second timeout allows longer-running scripts
- **Memory Limits**: May cause issues with large data processing (10MB vs 50MB)
- **Security Overhead**: Jint configuration adds minimal overhead (~1ms per execution)

---

## Future Enhancements

1. **Certificate Revocation**: Check certificate revocation lists (CRL)
2. **Audit Logging**: Log all JavaScript executions with isolation mode
3. **Custom Policies**: Per-tenant isolation policy configuration
4. **API Whitelisting**: Allow specific HTTP endpoints in Low Isolation
5. **Performance Metrics**: Track timeout/memory violations per node
6. **Certificate Management**: Centralized certificate service

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Jint security gaps | High | Extensive testing, security audit |
| Certificate parsing errors | Medium | Strict validation, error logging |
| Performance degradation | Medium | Timeout tuning, memory optimization |
| Unauthorized elevation | Low | Immutable configuration, audit trail |

---

## Approval & Sign-off

**Document Status**: Design Review
**Created By**: Claude Code
**Date**: 2026-02-03
**Next Step**: Await approval for implementation

---

## References

- Jint Documentation: https://github.com/sebastienros/jint
- JavaScript Security Best Practices: https://owasp.org/www-community/attacks/JavaScript_Execution
- OWASP Sandbox Escapes: https://owasp.org/www-community/Sandbox_Escape

---

**End of Document**
