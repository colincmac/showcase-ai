using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation.Realtime;
public interface IStreamingEvaluator : IEvaluator
{
    IAsyncEnumerable<EvaluationResult> EvaluateStreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default);
}
