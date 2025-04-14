using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Showcase.AI.Voice.Evaluation;

// Speaker Recognition https://learn.microsoft.com/en-us/legal/cognitive-services/speech-service/speaker-recognition/transparency-note-speaker-recognition?context=%2Fazure%2Fai-services%2Fspeech-service%2Fcontext%2Fcontext

public class VoiceConversationEvaluator : IEvaluator
{
    public IReadOnlyCollection<string> EvaluationMetricNames => throw new NotImplementedException();

    public ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages, ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null, IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
