using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Safety;
using Showcase.AI.Voice.Evaluation.RealtimeEvaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation.RealtimeEvaluation.Safety;

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
public class UserIntentEvaluator
{
    private readonly ContentSafetyServiceConfiguration? _safetyServiceConfiguration;
    private readonly IStreamingEvaluator _streamingCompositeEvaluator;
    private readonly IEnumerable<IEvaluator> _evaluators;
    public UserIntentEvaluator(ContentSafetyServiceConfiguration contentSafetyServiceConfiguration, IEnumerable<IEvaluator> customEvaluators)
    {
        _safetyServiceConfiguration = contentSafetyServiceConfiguration;
        _evaluators = customEvaluators ?? new List<IEvaluator>
        {
           new ViolenceEvaluator(contentSafetyServiceConfiguration),
           new HateAndUnfairnessEvaluator(contentSafetyServiceConfiguration),
           new ProtectedMaterialEvaluator(contentSafetyServiceConfiguration),
           new IndirectAttackEvaluator(contentSafetyServiceConfiguration)
        };
        _streamingCompositeEvaluator = new StreamingCompositeEvaluator(_evaluators);
    }

}
