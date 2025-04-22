using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation.Realtime.Safety;

/**
 * Keeps track of recent user utterances (text transcripts of the last few turns). After each user turn is completed (and possibly after the assistant responds), it can aggregate the last N user inputs and invoke the
 * Evaluates for:
 * - Direct Jailbreak
 * - Self Harm
 * 
 * References: 
 * - https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Evaluation.Safety
 * - 
 */
// 
public class UserIntentEvaluator : IEvaluator
{
    public IReadOnlyCollection<string> EvaluationMetricNames => throw new NotImplementedException();

    public ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages, ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null, IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
