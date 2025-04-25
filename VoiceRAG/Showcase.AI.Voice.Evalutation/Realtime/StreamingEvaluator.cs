//using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.Evaluation;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace Showcase.AI.Voice.Evaluation.Realtime;

//// Possibly use async filters with annotation only?
//// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#sample-response-stream-passes-filters
//// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#asynchronous-filter
//// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#content-streaming
//public class StreamingEvaluator //: IAsyncEnumerable<IEvaluator>
//{

//    public ValueTask<EvaluationResult> EvaluateAsync(IEnumerable<ChatMessage> messages, ChatResponse modelResponse, ChatConfiguration? chatConfiguration = null, IEnumerable<EvaluationContext>? additionalContext = null, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }
//    private async IAsyncEnumerable<EvaluationResult> EvaluateAndStreamResultsAsync(
//    IEnumerable<ChatMessage> messages,
//    ChatResponse modelResponse,
//    ChatConfiguration? chatConfiguration = null,
//    IEnumerable<EvaluationContext>? additionalContext = null,
//    [EnumeratorCancellation] CancellationToken cancellationToken = default)
//    {
//        var channel = Channel.CreateUnbounded<EvaluationResult>();

//        // Start a task to produce results
//        _ = Task.Run(async () =>
//        {
//            var tasks = _evaluators.Select(async evaluator =>
//            {
//                try
//                {
//                    // Evaluate using the current evaluator
//                    var result = await evaluator.EvaluateAsync(
//                        messages,
//                        modelResponse,
//                        chatConfiguration,
//                        additionalContext,
//                        cancellationToken).ConfigureAwait(false);

//                    // Write the result to the channel
//                    await channel.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
//                }
//                catch (Exception ex)
//                {
//                    // Handle exceptions and write diagnostic metrics to the channel
//                    var result = CreateDiagnosticResult(evaluator, ex);
//                    await channel.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
//                }
//            });

//            // Wait for all tasks to complete
//            await Task.WhenAll(tasks).ConfigureAwait(false);

//            // Signal that no more results will be written
//            channel.Writer.Complete();
//        }, cancellationToken);

//        // Stream results from the channel
//        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
//        {
//            yield return result;
//        }
//    }

//    private EvaluationResult CreateDiagnosticResult(IEvaluator evaluator, Exception exception)
//    {
//        string message = exception.ToString();
//        var result = new EvaluationResult();

//        foreach (string metricName in evaluator.EvaluationMetricNames)
//        {
//            var metric = new EvaluationMetric(metricName);
//            metric.AddDiagnostics(EvaluationDiagnostic.Error(message));
//            result.Metrics.Add(metric.Name, metric);
//        }

//        return result;
//    }

//    public static async IAsyncEnumerable<T> StreamResultsAsync<T>(
//        this IEnumerable<Task<T>> concurrentTasks,
//        bool preserveOrder = false,
//        [EnumeratorCancellation] CancellationToken cancellationToken = default)
//    {
//        if (preserveOrder)
//        {
//            foreach (Task<T> task in concurrentTasks)
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                yield return await task.ConfigureAwait(false);
//            }
//        }
//        else
//        {
//            await foreach (Task<T> task in
//                Task.WhenEach(concurrentTasks).WithCancellation(cancellationToken).ConfigureAwait(false))
//            {
//                yield return await task.ConfigureAwait(false);
//            }
//        }
//    }
//}
