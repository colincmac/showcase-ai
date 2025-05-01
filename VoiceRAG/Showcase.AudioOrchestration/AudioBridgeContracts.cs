#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

#region Shared Events


[JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(EventType))]
public abstract record RealtimeEvent()
{
    public abstract string EventType { get; }

    public Guid EventId { get; init; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public string ServiceEventType { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
};

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeAudioDeltaEvent))]
public record RealtimeAudioDeltaEvent(BinaryData AudioData, string ConversationRole, string? TranscriptText = null) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeAudioDeltaEvent);
    public bool IsAudioEmpty => AudioData is null || AudioData.IsEmpty;
    public bool IsTranscriptEmpty => string.IsNullOrEmpty(TranscriptText);
};


[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeMessageEvent))]
public record RealtimeMessageEvent(IEnumerable<string> ChatMessageContent, string ConversationRole) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeMessageEvent);
    public bool IsEmpty => !ChatMessageContent.Any();
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeTranscriptFinishedEvent))]
public record RealtimeTranscriptFinishedEvent(string Transcription, string ConversationRole) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeTranscriptFinishedEvent);
    public bool IsEmpty => string.IsNullOrWhiteSpace(Transcription);
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeMetricDeltaEvent))]
public record RealtimeMetricDeltaEvent(BinaryData Metric) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeMetricDeltaEvent);
    public bool IsEmpty => Metric.IsEmpty;
}


// Similar to Stop Audio from ACS
[JsonDerivedType(typeof(RealtimeEvent), nameof(ParticipantStartedSpeakingEvent))]
public record ParticipantStartedSpeakingEvent(string ConversationRole, string ParticipantId) : RealtimeEvent
{
    public override string EventType => nameof(ParticipantStartedSpeakingEvent);
    public bool IsEmpty => false;
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeVideoDeltaEvent))]
public record RealtimeVideoDeltaEvent(BinaryData VideoData, string ConversationRole) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeVideoDeltaEvent);
    public bool IsEmpty => VideoData.IsEmpty;
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeUserIntentDiscoveredEvent))]
public record RealtimeUserIntentDiscoveredEvent(string ParticipantId, string Intent) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeUserIntentDiscoveredEvent);
    public bool IsEmpty => true;
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeUserIntentFulfilledEvent))]
public record RealtimeUserIntentFulfilledEvent(string ConversationRole, string ParticipantId) : RealtimeEvent
{
    public override string EventType => nameof(RealtimeUserIntentFulfilledEvent);
    public bool IsEmpty => true;
}

#endregion
