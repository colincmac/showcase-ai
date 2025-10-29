using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.AI.ContentSafety;
using Azure.Identity;

namespace Showcase.AI.Voice.Evaluation.RealtimeEvaluation;

// Possibly allow the use the ChatCompletions API with async filters instead?
// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#sample-response-stream-passes-filters
// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#asynchronous-filter
// https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#content-streaming
public class StreamingCompositeEvaluator: IStreamingEvaluator, IAsyncDisposable
{
    private readonly IReadOnlyList<IEvaluator> _evaluators;
    public IReadOnlyCollection<string> EvaluationMetricNames { get; }

    public StreamingCompositeEvaluator(IEnumerable<IEvaluator> evaluators)
    {
        var t = new ContentSafetyClient(new Uri("https://<your-endpoint>.cognitiveservices.azure.com/"), new DefaultAzureCredential());
        ArgumentNullException.ThrowIfNull(evaluators);

        var metricNames = new HashSet<string>();

        foreach (IEvaluator evaluator in evaluators)
        {
            if (evaluator.EvaluationMetricNames.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The '{nameof(evaluator.EvaluationMetricNames)}' property on '{evaluator.GetType().FullName}' returned an empty collection. An evaluator must advertise the names of the metrics that it supports.");
            }

            foreach (string metricName in evaluator.EvaluationMetricNames)
            {
                if (!metricNames.Add(metricName))
                {
                    throw new ArgumentException($"Cannot add multiple evaluators for '{metricName}'.", nameof(evaluators));
                }
            }
        }

        EvaluationMetricNames = metricNames;
        _evaluators = [.. evaluators];
    }


    /// <summary>
    /// Evaluates the given messages and model response using the provided evaluators, returning the final EvaluationResult result.
    /// See (CompositeEvaluator)[https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.AI.Evaluation/CompositeEvaluator.cs]    
    /// </summary>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<EvaluationResult> resultsStream = 
            EvaluateAndStreamResultsAsync(
                messages,
                modelResponse,
                chatConfiguration,
                additionalContext,
                cancellationToken);
        var results = await resultsStream.ToListAsync(cancellationToken);
        return new EvaluationResult(results.SelectMany(r => r.Metrics.Values));
    }


    /// <summary>
    /// Evaluates the given messages and model response using the provided evaluators, streaming the resulting EvaluationResult items as they become available.
    /// </summary>
    public async IAsyncEnumerable<EvaluationResult> EvaluateAndStreamResultsAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<EvaluationResult>();

        // Start a task to produce results
        _ = Task.Run(async () =>
        {
            var tasks = _evaluators.Select(async evaluator =>
            {
                try
                {
                    // Evaluate using the current evaluator
                    var result = await evaluator.EvaluateAsync(
                        messages,
                        modelResponse,
                        chatConfiguration,
                        additionalContext,
                        cancellationToken).ConfigureAwait(false);

                    // Write the result to the channel
                    await channel.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Handle exceptions and write diagnostic metrics to the channel
                    var result = CreateDiagnosticResult(evaluator, ex);
                    await channel.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                }
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Signal that no more results will be written
            channel.Writer.Complete();
        }, cancellationToken);

        // Stream results from the channel
        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }
    }

    private EvaluationResult CreateDiagnosticResult(IEvaluator evaluator, Exception exception)
    {
        string message = exception.ToString();
        var result = new EvaluationResult();

        foreach (string metricName in evaluator.EvaluationMetricNames)
        {
            var metric = new EvaluationMetric(metricName);
            metric.AddDiagnostics(EvaluationDiagnostic.Error(message));
            result.Metrics.Add(metric.Name, metric);
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var evaluator in _evaluators)
        {
            if (evaluator is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (evaluator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

