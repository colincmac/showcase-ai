using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation.RealtimeEvaluation.Quality;
public class ProsodyAndClarityEvaluator : SingleNumericMetricEvaluator
{
    protected override string MetricName => throw new NotImplementedException();

    protected override bool IgnoresHistory => throw new NotImplementedException();

    protected override ValueTask<string> RenderEvaluationPromptAsync(ChatMessage? userRequest, ChatResponse modelResponse, IEnumerable<ChatMessage>? includedHistory, IEnumerable<EvaluationContext>? additionalContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
