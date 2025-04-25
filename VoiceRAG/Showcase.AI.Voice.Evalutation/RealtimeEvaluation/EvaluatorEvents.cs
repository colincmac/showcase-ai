using Google.Protobuf;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation.RealtimeEvaluation;


[JsonDerivedType(typeof(RealtimeEvent), nameof(ConversationEvaluationUpdate))]
public record ConversationEvaluationUpdate(EvaluationResult EvaluationResult)
    : RealtimeEvent
{
    public override string EventType => nameof(ConversationEvaluationUpdate);
};

[JsonDerivedType(typeof(RealtimeEvent), nameof(ConversationEvaluationFinishedUpdate))]
public record ConversationEvaluationFinishedUpdate(IEnumerable<EvaluationMetric> EvaluationMetrics)
    : ConversationEvaluationUpdate(new EvaluationResult(EvaluationMetrics))
{
    public override string EventType => nameof(ConversationEvaluationFinishedUpdate);
};

[JsonDerivedType(typeof(RealtimeEvent), nameof(ConversationEvaluationStreamingPartDeltaUpdate))]
public record ConversationEvaluationStreamingPartDeltaUpdate(EvaluationResult EvaluationResult)
    : ConversationEvaluationUpdate(EvaluationResult)
{
    public override string EventType => nameof(ConversationEvaluationStreamingPartDeltaUpdate);
};

[JsonDerivedType(typeof(RealtimeEvent), nameof(SafetyGuardrailFailedEvent))]
public record SafetyGuardrailFailedEvent(string ConversationId, string EvaluationId, EvaluationMetricInterpretation Interpretation )
    : RealtimeEvent
{
    public override string EventType => nameof(SafetyGuardrailFailedEvent);
};

[JsonDerivedType(typeof(RealtimeEvent), nameof(QualityGuardrailFailedEvent))]
public record QualityGuardrailFailedEvent(string ConversationId, string EvaluationId, EvaluationMetricInterpretation Interpretation)
    : RealtimeEvent
{
    public override string EventType => nameof(QualityGuardrailFailedEvent);
    public IList<ChatMessage> Messages { get; init; }

    public ChatResponse ModelResponse { get; init; }
};