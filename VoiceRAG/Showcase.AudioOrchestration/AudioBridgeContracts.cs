using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

#region Shared Models & Commands

/// <summary>
/// Represents one audio frame of PCM 24K Mono data.
/// </summary>
public record AudioFrame(byte[] Buffer, bool IsEmpty);

/// <summary>
/// Represents one data frame.
/// </summary>
public record DataFrame(byte[] Buffer, bool IsEmpty);
/// <summary>
/// Base class for commands sent from auditing agents to the primary AI agent.
/// </summary>
public abstract record AiAgentCommand;

/// <summary>
/// Command instructing the primary agent to override its current behavior.
/// </summary>
public record OverrideInstructionCommand(string Instruction) : AiAgentCommand;

/// <summary>
/// Command instructing the primary agent to update its conversation prompt.
/// </summary>
public record UpdatePromptCommand(string NewPrompt) : AiAgentCommand;

#endregion
