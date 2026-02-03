namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

/// <summary>
/// Interface for action nodes (nodes that perform operations).
/// Action nodes execute operations and return results.
/// Examples: SendEmail, WriteFile, Delay, etc.
/// </summary>
public interface IActionNodeExecution : IProcessElementExecution
{
    // Inherits all execution behavior from IProcessElementExecution
    // Specific action implementation in Execute method
}
