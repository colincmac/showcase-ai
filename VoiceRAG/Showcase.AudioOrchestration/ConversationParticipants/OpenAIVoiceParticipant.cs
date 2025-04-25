#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;


namespace Showcase.AI.Voice.ConversationParticipants;

// TODO: Allow override for event handling

public class OpenAIVoiceParticipant : ConversationParticipant
{

    // The Session object, which controls the parameters of the interaction, like the model being used, the voice used to generate output, and other configuration.
    // A Conversation, which represents user input Items and model output Items generated during the current session.
    // Responses, which are model-generated audio or text Items that are added to the Conversation.
    private readonly RealtimeConversationClient _aiClient;
    private readonly RealtimeSessionOptions _sessionOptions;

    internal Task ParticipantEventProcessing { get; private set; } = Task.CompletedTask;
    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;

    public OpenAIVoiceParticipant(
        RealtimeConversationClient aiClient,
        RealtimeSessionOptions sessionOptions,
        ILoggerFactory loggerFactory,
        string id,
        string name) : base(id, name)
    {
        _aiClient = aiClient;
        _sessionOptions = sessionOptions;
        _logger = loggerFactory.CreateLogger<OpenAIVoiceParticipant>();
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger.LogInformation("Starting OpenAI Realtime Agent {AgentId} with session options: {SessionOptions}", Id, _sessionOptions);
        var session = await _aiClient.StartConversationSessionAsync(cancellationToken);
        var options = ToOpenAIConversationSessionOptions(_sessionOptions);
        await session.ConfigureSessionAsync(options, cancellationToken);

        _logger.LogInformation("Session started with options: {SessionOptions}", _sessionOptions.ToString());

        ParticipantEventProcessing = Task.Run(() => ProcessParticipantEventsAsync(session, _cts.Token), _cts.Token);
        InternalEventProcessing = Task.Run(() => ProcessInboundEvents(session, _cts.Token), _cts.Token);

        _logger.LogInformation("Started OpenAI Realtime Agent {AgentId} with session options: {SessionOptions}", Id, _sessionOptions);

        await Task.WhenAll(ParticipantEventProcessing, InternalEventProcessing).ConfigureAwait(false);
    }

    private async Task ProcessParticipantEventsAsync(RealtimeConversationSession session, CancellationToken cancellationToken)
    {
        try
        {
            var tools = _sessionOptions.Tools?.OfType<AIFunction>().ToList();
            await foreach (var update in session.ReceiveUpdatesAsync(cancellationToken))
            {
                //_logger.LogDebug("Received update: {Update}", update);

                if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                {
                    _logger.LogDebug("Delta Output Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
                    _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);

                    var evt = new RealtimeAudioDeltaEvent(AudioData: deltaUpdate.AudioBytes, TranscriptText: deltaUpdate.AudioTranscript)
                    {
                        ServiceEventType = deltaUpdate.Kind.ToString(),
                        SourceId = Id
                    };
                    await _outboundChannel.Writer.WriteAsync(evt, cancellationToken);
                }
                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    _logger.LogDebug("Incoming audio to AI Agent. Barge-in by stopping all in-transit outgoing audio");

                    var evt = new ParticipantStartedSpeakingEvent()
                    {
                        ServiceEventType = speechStartedUpdate.Kind.ToString(),
                        SourceId = Id
                    };
                    await _outboundChannel.Writer.WriteAsync(evt, cancellationToken);
                }
                if (update is ConversationInputTranscriptionFinishedUpdate inputTranscriptionFinished)
                {
                    _logger.LogDebug("Delta Input Transcript: {AudioTranscript}", inputTranscriptionFinished.Transcript);

                    var evt = new RealtimeTranscriptFinishedEvent(Transcription: inputTranscriptionFinished.Transcript)
                    {
                        ServiceEventType = inputTranscriptionFinished.Kind.ToString(),
                        SourceId = Id
                    };
                    await _outboundChannel.Writer.WriteAsync(evt, cancellationToken);
                }

                if (update is ConversationSessionConfiguredUpdate sessionConfigured)
                {
                    _logger.LogDebug("Session configured with options: {SessionOptions}", sessionConfigured.ToString());
                    await session.StartResponseAsync(cancellationToken);
                }
                if (update is ConversationResponseFinishedUpdate turnFinished)
                {
                    _logger.LogDebug("ConversationResponseFinishedUpdate: {SessionOptions}", JsonSerializer.Serialize(turnFinished));
                }
                if (tools is not null)
                    await Shared.AIExtensions.Realtime.OpenAIRealtimeExtensions.HandleToolCallsAsync(session, update, tools, cancellationToken: cancellationToken);
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
            //await _isReadyTcs.Task.ConfigureAwait(false);

            await foreach (var internalEvent in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (internalEvent is RealtimeAudioDeltaEvent audioEvent) 
                    await HandleAudioAsync(session, audioEvent, cancellationToken);

                if (internalEvent is RealtimeMessageEvent chatEvent) 
                    await session.AddItemAsync(ConversationItem.CreateSystemMessage(chatEvent.ChatMessageContent.Select(t => (ConversationContentPart)t)), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound events: {Message}", ex.Message);
        }

    }

    private async Task HandleAudioAsync(RealtimeConversationSession session, RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        using var audioStream = new MemoryStream(audioEvent.AudioData.ToArray());

        await session.SendInputAudioAsync(audioStream, _cts.Token);
    }


    private static ConversationSessionOptions ToOpenAIConversationSessionOptions(RealtimeSessionOptions? options)
    {
        if (options is null) return new ConversationSessionOptions();

        var tools = options.Tools?.OfType<AIFunction>()
            .Select(t => Shared.AIExtensions.Realtime.OpenAIRealtimeExtensions.ToConversationFunctionTool(t)) ?? Enumerable.Empty<ConversationFunctionTool>();

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
        //_currentSession?.Dispose();
        //_currentSession = null;

        base.Dispose();
    }
}
