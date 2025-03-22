#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public sealed class OpenAIRealtimeAgent : ConversationParticipant
{
    private readonly RealtimeConversationClient _aiClient;
    private readonly RealtimeSessionOptions _sessionOptions;
    private RealtimeConversationSession? _currentSession;
    //private readonly IAIToolRegistry _aiToolRegistry;
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

    internal Task ParticipantEventProcessing { get; private set; } = Task.CompletedTask;
    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;

    public string? Instructions { get; init; }
    public string? Description { get; init; }

    public OpenAIRealtimeAgent(
        RealtimeConversationClient aiClient,
        RealtimeSessionOptions sessionOptions,
        //IAIToolRegistry aiToolRegistry,
        string? instructions = null,
        string? description = null,
        string? id = null,
        string? name = null) : base(id, name)
    {
        _aiClient = aiClient;
        _sessionOptions = sessionOptions;
        //_aiToolRegistry = aiToolRegistry;

        Instructions = instructions;
        Description = description;
        _inboundChannel = Channel.CreateBounded<RealtimeEvent>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public override async Task StartResponseAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var session = await GetOrStartSessionAsync(_cts.Token);
        ParticipantEventProcessing = Task.Run(async () => await ProcessParticipantEventsAsync(session, _cts.Token), _cts.Token);
        InternalEventProcessing = Task.Run(async () => await ProcessInboundEvents(session, _cts.Token), _cts.Token);
        await Task.WhenAll(ParticipantEventProcessing, InternalEventProcessing);
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

    private async Task ProcessParticipantEventsAsync(RealtimeConversationSession session, CancellationToken cancellationToken)
    {
        try
        {

            var tools = _sessionOptions.Tools?.OfType<AIFunction>().ToList();
            await foreach (var update in session.ReceiveUpdatesAsync(cancellationToken))
            {
                var realtimeEvent = ConvertToExternalEvent(update);
                if (realtimeEvent is not null) await _outboundChannel.Writer.WriteAsync(realtimeEvent, cancellationToken);

                if (tools is not null)
                    await session.HandleToolCallsAsync(update, tools, cancellationToken: cancellationToken);
            }
            
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session {SessionId} was cancelled.", Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing participant events: {Message}", ex.Message);
        }

    }

    private async Task ProcessInboundEvents(RealtimeConversationSession session, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var internalEvent in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (internalEvent is RealtimeAudioEvent audioEvent) await HandleAudioAsync(session, audioEvent, cancellationToken);

                if (internalEvent is RealtimeMessageEvent chatEvent) await session.AddItemAsync(ConversationItem.CreateAssistantMessage([chatEvent.ChatMessageContent]), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound events: {Message}", ex.Message);
        }

    }

    private async ValueTask HandleAudioAsync(RealtimeConversationSession session, RealtimeAudioEvent audioEvent, CancellationToken cancellationToken)
    {
        await session.SendInputAudioAsync(audioEvent.AudioData, _cts.Token);
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
        if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
        {
            _logger.LogDebug("Incomming audio to AI Agent. Stopping all outgoing audio");

            return new RealtimeStopAudioEvent(ServiceEventType: speechStartedUpdate.Kind.ToString(), SourceId: Id);
        }
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

    public override void Dispose()
    {
        _currentSession?.Dispose();
        _currentSession = null;

        base.Dispose();
    }
}
