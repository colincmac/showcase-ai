#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public sealed class OpenAIRealtimeAgent : RealtimeAgent
{
    private readonly RealtimeConversationClient _aiClient;
    private readonly RealtimeSessionOptions _sessionOptions;
    private RealtimeConversationSession? _currentSession;

    internal Task AIEventProcessing { get; private set; } = Task.CompletedTask;
    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;


    public OpenAIRealtimeAgent(
        RealtimeConversationClient aiClient,
        RealtimeSessionOptions sessionOptions,
        string? instructions = null,
        string? description = null,
        string? id = null,
        string? name = null)
    {
        _aiClient = aiClient;
        _sessionOptions = sessionOptions;

        Instructions = instructions;
        Description = description;
        Id = id ?? Guid.NewGuid().ToString();
        Name = name;
    }

    public override async IAsyncEnumerable<RealtimeEvent> StartResponseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AIEventProcessing = Task.Run(() => ProcessOpenAIResponses(cancellationToken), cancellationToken);
        InternalEventProcessing = Task.Run(() => ProcessInternalResponses(cancellationToken), cancellationToken);
        await foreach (var update in _outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return update;
        }
    }

    private async Task<RealtimeConversationSession> GetOrStartSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession == null)
        {
            _currentSession = await _aiClient.StartConversationSessionAsync(cancellationToken);
            var options = ToOpenAIConversationSessionOptions(_sessionOptions);
            await _currentSession.ConfigureSessionAsync(options, cancellationToken);
        }
        return _currentSession;
    }

    private async Task ProcessOpenAIResponses(CancellationToken cancellationToken)
    {
        var session = await GetOrStartSessionAsync(cancellationToken);
        var tools = _sessionOptions.Tools?.OfType<AIFunction>().ToList();
        await foreach (var update in session.ReceiveUpdatesAsync(cancellationToken))
        {
            var realtimeEvent = ConvertToExternalEvent(update);
            if(realtimeEvent is not null) await _outputChannel.Writer.WriteAsync(realtimeEvent, cancellationToken);

            if(tools is not null)
                await session.HandleToolCallsAsync(update, tools, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessInternalResponses(CancellationToken cancellationToken)
    {
        var session = await GetOrStartSessionAsync(cancellationToken);
        await foreach (var internalEvent in _inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if(internalEvent is RealtimeAudioEvent audioEvent) await session.SendInputAudioAsync(audioEvent.AudioData, cancellationToken);

            if(internalEvent is RealtimeMessageEvent chatEvent) await session.AddItemAsync(ConversationItem.CreateAssistantMessage([chatEvent.ChatMessageContent]), cancellationToken);
        }
    }

    private RealtimeEvent? ConvertToExternalEvent(ConversationUpdate update)
    {
        // Handle the conversation update here
        // For example, you can log the update or process it further
        _logger.LogInformation("Received conversation update: {Update}", update);

        // Make sure to process audio first
        if(update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
        {
            _logger.LogDebug("Delta Output Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
            _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);

            return new RealtimeAudioEvent(AudioData: deltaUpdate.AudioBytes, TranscriptText: deltaUpdate.AudioTranscript, ServiceEventType: deltaUpdate.Kind.ToString(), SourceId: Id);
        }
        //if(update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
        //{
        //    _logger.LogDebug("Delta Output Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
        //    _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);

        //    return new RealtimeAudioEvent(AudioData: deltaUpdate.AudioBytes, TranscriptText: deltaUpdate.AudioTranscript, ServiceEventType: deltaUpdate.Kind.ToString(), SourceId: Id);
        //}
        if (update is ConversationInputTranscriptionFinishedUpdate inputTranscriptionFinished)
        {
            _logger.LogDebug("Delta Input Transcript: {AudioTranscript}", inputTranscriptionFinished.Transcript);

            return new RealtimeTranscriptMessageEvent(Transcription: inputTranscriptionFinished.Transcript,  ServiceEventType: inputTranscriptionFinished.Kind.ToString(), SourceId: Id);
        }
        return null;
    }

    private static ConversationSessionOptions ToOpenAIConversationSessionOptions(RealtimeSessionOptions? options)
    {
        if (options is null) return new ConversationSessionOptions();

        var tools = options.Tools?.OfType<AIFunction>()
            .Select(t => t.ToConversationFunctionTool()) ?? [];

        ConversationSessionOptions result = new()
        {
            Instructions = options.Instructions,
            Voice = options.Voice,
            InputAudioFormat = options.InputAudioFormat,
            OutputAudioFormat = options.OutputAudioFormat,
            TurnDetectionOptions = options.TurnDetectionOptions,
            InputTranscriptionOptions = options.InputTranscriptionOptions,
        };
        foreach (var tool in tools)
        {
            result.Tools.Add(tool);
        }
        return result;
    }
}
