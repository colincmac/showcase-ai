#pragma warning disable OPENAI002

using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

#region Shared Models & Commands

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WellKnownEventDataType
{
    Text,
    Audio,
    Video,
    Metric
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WellKnownEventType
{
    Message,
    TranscriptText,
    RawAudio,
    RawVideo,
    MetricData,
    StopAudio,
}


public record RealtimeEvent(WellKnownEventType EventType, string ServiceEventType, string SourceId)
{
    public Guid EventId { get; init; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
};
public record RealtimeAudioEvent(BinaryData AudioData, string ServiceEventType, string SourceId, string? TranscriptText = null): RealtimeEvent(WellKnownEventType.RawAudio, ServiceEventType, SourceId)
{
    public bool IsEmpty => AudioData.IsEmpty;
};
public record RealtimeMessageEvent(string ChatMessageContent, string ServiceEventType, string SourceId) : RealtimeEvent(WellKnownEventType.Message, ServiceEventType, SourceId);
public record RealtimeTranscriptMessageEvent(string Transcription, string ServiceEventType, string SourceId) : RealtimeEvent(WellKnownEventType.TranscriptText, ServiceEventType, SourceId);

public record RealtimeMetricEvent(BinaryData Metric, string ServiceEventType, string SourceId) : RealtimeEvent(WellKnownEventType.MetricData, ServiceEventType, SourceId);
public record RealtimeStopAudioEvent(string ServiceEventType, string SourceId) : RealtimeEvent(WellKnownEventType.StopAudio, ServiceEventType, SourceId);

public record RealtimeConversationUpdateEvent(ConversationUpdate Update);

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
