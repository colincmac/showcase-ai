using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Chat;
using System.ClientModel;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants;

namespace Showcase.Shared.AIExtensions.Realtime.Telemetry;


public class OpenTelemetryScope : IDisposable
{
    private static readonly ActivitySource s_chatSource = new(DefaultSourceName);
    private static readonly Meter s_chatMeter = new(DefaultSourceName);

    // TODO: possibly add Server Histogram? https://github.com/open-telemetry/semantic-conventions/blob/main/docs/gen-ai/gen-ai-metrics.md#generative-ai-model-server-metrics
    private static readonly Histogram<double> _operationDurationHistogram = s_chatMeter.CreateHistogram<double>(
            GenAI.Client.OperationDuration.Name,
            SecondsUnit,
            GenAI.Client.OperationDuration.Description
            //advice: new() { HistogramBucketBoundaries = GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
            );

    private static readonly Histogram<int> _tokenUsageHistogram = s_chatMeter.CreateHistogram<int>(
            GenAI.Client.TokenUsage.Name,
            TokensUnit,
            GenAI.Client.TokenUsage.Description
            //advice: new() { HistogramBucketBoundaries = GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
            );

    private readonly string _operationName;
    private readonly string _serverAddress;
    private readonly int _serverPort;
    private readonly string _requestModelId;

    private readonly string _providerName;

    private Stopwatch? _duration;
    private Activity? _activity;
    private TagList _commonTags;

    private OpenTelemetryScope(
        string operationName,
        string modelId,
        string serverAddress,
        string providerName,
        int serverPort)
    {
        _requestModelId = modelId;
        _operationName = operationName;
        _serverAddress = serverAddress;
        _serverPort = serverPort;
        _providerName = providerName;

        _commonTags = new TagList
        {
            { GenAI.SystemName, _providerName },
            { GenAI.Request.Model, _requestModelId },
            { Server.Address, _serverAddress },
            { Server.Port, _serverPort },
            { GenAI.Operation.Name, _operationName },
        };
    }


    public static OpenTelemetryScope StartRealtimeConversationSessionScope(
        string model, 
        string serverAddress, 
        string providerName, 
        int serverPort)
    {
        var scope = new OpenTelemetryScope(GenAI.RealtimeSessionConversation, model, serverAddress, providerName, serverPort);
        scope.StartConversationSession();
        return scope;
    }

    private void StartConversationSession()
    {
        _duration = Stopwatch.StartNew();
        // Activity: <operationName> <modelId>
        _activity = s_chatSource.StartActivity(string.IsNullOrWhiteSpace(_requestModelId) ? GenAI.RealtimeSessionConversation : $"{GenAI.RealtimeSessionConversation} {_requestModelId}", ActivityKind.Client);
        return;
    }

    public void RecordSessionUpdated(RealtimeSessionOptions options)
    {
        if (_activity?.IsAllDataRequested == true)
        {
            RecordCommonAttributes();
            SetActivityTagIfNotNull(GenAI.Request.MaxTokens, options?.MaxOutputTokens?.NumericValue);
            SetActivityTagIfNotNull(GenAI.Request.Temperature, options?.Temperature);
            SetActivityTagIfNotNull(GenAI.Request.Voice, options?.Voice?.ToString());
            SetActivityTagIfNotNull(GenAI.Request.InputAudioFormat, options?.InputAudioFormat?.ToString());
            SetActivityTagIfNotNull(GenAI.Request.OutputAudioFormat, options?.OutputAudioFormat?.ToString());
            SetActivityTagIfNotNull(GenAI.Request.TurnDetectionKind, options?.TurnDetectionOptions?.Kind.ToString());
        }
    }

    public void RecordConversationCompletion(ChatCompletion completion)
    {
        RecordMetrics(completion.Model, null, completion.Usage?.InputTokenCount, completion.Usage?.OutputTokenCount);

        if (_activity?.IsAllDataRequested == true)
        {
            RecordResponseAttributes(completion.Id, completion.Model, completion.FinishReason, completion.Usage);
        }
    }


    public void RecordException(Exception ex)
    {
        var errorType = GetErrorType(ex);
        RecordMetrics(null, errorType, null, null);
        if (_activity?.IsAllDataRequested == true)
        {
            _activity.SetTag(OpenTelemetryConstants.Error.Type, errorType);
            _activity.SetStatus(ActivityStatusCode.Error, ex?.Message ?? errorType);
        }
    }

    public void Dispose()
    {
        _activity?.Stop();
        _activity?.Dispose();
        s_chatMeter.Dispose();
    }

    private void RecordCommonAttributes()
    {
        _activity?.SetTag(GenAI.SystemName, _providerName);
        _activity?.SetTag(GenAI.Request.Model, _requestModelId);
        _activity?.SetTag(Server.Address, _serverAddress);
        _activity?.SetTag(Server.Port, _serverPort);
        _activity?.SetTag(GenAI.Operation.Name, _operationName);
    }

    private void RecordMetrics(string? responseModel, string? errorType, int? inputTokensUsage, int? outputTokensUsage)
    {
        // tags is a struct, let's copy and modify them
        var tags = _commonTags;

        if (responseModel != null)
        {
            tags.Add(GenAI.Response.Model, responseModel);
        }

        if (inputTokensUsage != null)
        {
            var inputUsageTags = tags;
            inputUsageTags.Add(GenAI.Response.InputTokens, "input");
            _tokenUsageHistogram.Record(inputTokensUsage.Value, inputUsageTags);
        }

        if (outputTokensUsage != null)
        {
            var outputUsageTags = tags;
            outputUsageTags.Add(GenAI.Response.OutputTokens, "output");
            _tokenUsageHistogram.Record(outputTokensUsage.Value, outputUsageTags);
        }

        if (errorType != null)
        {
            tags.Add(OpenTelemetryConstants.Error.Type, errorType);
        }

        _operationDurationHistogram.Record(_duration.Elapsed.TotalSeconds, tags);
    }

    private void RecordResponseAttributes(string? responseId, string? model, ChatFinishReason? finishReason, ChatTokenUsage usage)
    {
        SetActivityTagIfNotNull(GenAI.Response.Id, responseId);
        SetActivityTagIfNotNull(GenAI.Response.Model, model);
        SetActivityTagIfNotNull(GenAI.Response.InputTokens, usage?.InputTokenCount);
        SetActivityTagIfNotNull(GenAI.Response.OutputTokens, usage?.OutputTokenCount);
        SetFinishReasonAttribute(finishReason);
    }

    private void SetFinishReasonAttribute(ChatFinishReason? finishReason)
    {
        if (finishReason == null)
        {
            return;
        }

        var reasonStr = finishReason switch
        {
            ChatFinishReason.ContentFilter => "content_filter",
            ChatFinishReason.FunctionCall => "function_call",
            ChatFinishReason.Length => "length",
            ChatFinishReason.Stop => "stop",
            ChatFinishReason.ToolCalls => "tool_calls",
            _ => finishReason.ToString(),
        };

        // There could be multiple finish reasons, so semantic conventions use array type for the corrresponding attribute.
        // It's likely to change, but for now let's report it as array.
        _activity?.SetTag(GenAI.Response.FinishReasons, new[] { reasonStr });
    }

    private string GetChatMessageRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Function => "function",
            ChatMessageRole.System => "system",
            ChatMessageRole.Tool => "tool",
            ChatMessageRole.User => "user",
            _ => role.ToString(),
        };

    private string? GetErrorType(Exception exception)
    {
        if (exception is ClientResultException requestFailedException)
        {
            // requestFailedException.InnerException.HttpRequestError into error.type
            return requestFailedException.InnerException?.Message;
        }

        return exception?.GetType()?.FullName;
    }

    private void SetActivityTagIfNotNull(string name, object? value)
    {
        if (value != null)
        {
            _activity?.SetTag(name, value);
        }
    }

    private void SetActivityTagIfNotNull(string name, int? value)
    {
        if (value.HasValue)
        {
            _activity?.SetTag(name, value.Value);
        }
    }

    private void SetActivityTagIfNotNull(string name, float? value)
    {
        if (value.HasValue)
        {
            _activity?.SetTag(name, value.Value);
        }
    }
}
