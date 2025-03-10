#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class ConversationSessionOptions
{
    public string? ModelId { get; set; }
    public string? Instructions { get; set; }
    public ConversationVoice? Voice { get; set; }
    public ConversationAudioFormat? InputAudioFormat { get; set; }
    public ConversationAudioFormat? OutputAudioFormat { get; set; }

    [JsonIgnore]
    public IList<AITool>? Tools { get; set; }
    public float? Temperature { get; set; }
    public ConversationToolChoice? ToolChoice { get; set; }

    public ConversationMaxTokensChoice? MaxOutputTokens { get; set; }

    public ConversationTurnDetectionOptions? TurnDetectionOptions { get; set; }
    public ConversationInputTranscriptionOptions? InputTranscriptionOptions { get; set; }
    public ConversationContentModalities ContentModalities { get; set; }
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    
    public int PCMFrameSize { get; set; } = 0;
}
