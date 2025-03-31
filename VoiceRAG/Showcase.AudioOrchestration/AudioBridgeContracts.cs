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
    IntentDiscovered
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EventType")]
public abstract record RealtimeEvent()
{
    public abstract string EventType { get; }

    public Guid EventId { get; init; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public string ServiceEventType { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
};

[JsonDerivedType(typeof(RealtimeAudioDeltaEvent), "RawAudio")]
public record RealtimeAudioDeltaEvent(BinaryData AudioData, string? TranscriptText = null) : RealtimeEvent
{
    public override string EventType => "RawAudio";
    public bool IsEmpty => AudioData is null || AudioData.IsEmpty;
};

[JsonDerivedType(typeof(RealtimeMessageEvent), "ChatMessage")]
public record RealtimeMessageEvent(IEnumerable<string> ChatMessageContent, string ConversationMessageRole) : RealtimeEvent
{
    public override string EventType => "ChatMessage";
    public bool IsEmpty => !ChatMessageContent.Any();
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "TranscriptText")]
public record RealtimeTranscriptMessageEvent(string Transcription) : RealtimeEvent
{
    public override string EventType => "TranscriptText";
    public bool IsEmpty => string.IsNullOrWhiteSpace(Transcription);
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "MetricData")]
public record RealtimeMetricEvent(BinaryData Metric) : RealtimeEvent
{
    public override string EventType => "MetricData";
    public bool IsEmpty => Metric.IsEmpty;
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "StopAudio")]
public record RealtimeStopAudioEvent() : RealtimeEvent
{
    public override string EventType => "StopAudio";
    public bool IsEmpty => true;
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "RawVideo")]
public record RealtimeVideoDeltaEvent(BinaryData VideoData) : RealtimeEvent
{
    public override string EventType => "RawVideo";
    public bool IsEmpty => VideoData.IsEmpty;
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "UserIntentDiscovered")]
public record RealtimeUserIntentDiscoveredEvent() : RealtimeEvent
{
    public override string EventType => "UserIntentDiscovered";
    public bool IsEmpty => true;
}

[JsonDerivedType(typeof(RealtimeTranscriptMessageEvent), "UserIntentFulfilled")]
public record RealtimeUserIntentFulfilledEvent() : RealtimeEvent
{
    public override string EventType => "UserIntentDiscovered";
    public bool IsEmpty => true;
}

#endregion
