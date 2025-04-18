using Azure.Communication.CallAutomation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.ConversationParticipants.CallQualityAuditor;

/**
 * Call Quality Signals
 * These signals are used to assess the quality of the call and provide feedback to the participants.
 *Offline Eval
 *
 * RealtimeEval?
 ** Voice
 * Pacing (slow down to pronounce certain statements)
 * tone modulation
 * perceived naturalness and fluency of conversation?
 * emotional quotient (iq/eq)
 *
 * emotional inference
 */

public record ScoreCard();
public record ProsodyAndClaritySignal();

public record SpeechEmotionSignal();

public record AudioQualitySignal();
public record StreamQuality();

public record SentimentAnalysis() : RealtimeEvent
{
    public override string EventType => nameof(SentimentAnalysis);
    public bool IsEmpty => false;

    public string SpeakerId { get; init; } = string.Empty;
    public double OffsetInTicks;
};