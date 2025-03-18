#pragma warning disable OPENAI002

using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public class AIRealtimeConversationAgent : IAgentChannel
{
    private readonly RealtimeConversationClient _aiClient;
    private readonly ConversationSessionOptions _sessionOptions;
    private readonly ILogger _logger;
    private RealtimeConversationSession? _currentSession;
    private readonly Channel<ConversationItem> _internalChannel = Channel.CreateUnbounded<ConversationItem>();

    private readonly ConversationHistory _conversationTranscriptionHistory = new();

    public readonly string ConversationId;

    internal Task Running { get; private set; } = Task.CompletedTask;

    public AIRealtimeConversationAgent(RealtimeConversationClient aiClient, ConversationSessionOptions sessionOptions, ILogger logger)
    {
        _logger = logger;
        _aiClient = aiClient;
        _sessionOptions = sessionOptions;
        ConversationId = Guid.NewGuid().ToString();
    }

    public bool AcceptsDataType(WellKnownAIDataType modality)
    {
        // Check if the modality is supported by the AI client.
        return modality switch
        {
            WellKnownAIDataType.Text => true,
            WellKnownAIDataType.Audio => true,
            WellKnownAIDataType.Video => false,
            WellKnownAIDataType.SensorData => false,
            _ => throw new NotSupportedException($"Data type {modality} is not supported by this AI.")
        };
    }

    public async Task StartConversationSessionAsync(CancellationToken cancellationToken = default)
    {
        // Start the conversation session with the AI client and configure with options.
        _currentSession = await _aiClient.StartConversationSessionAsync(cancellationToken);
        await _currentSession.ConfigureSessionAsync(_sessionOptions, cancellationToken);
        var isReconnect = false;

        if(_currentSession == null)
        {

        }
        else
        {
            isReconnect = true;
        }
        Running = ProcessRealtimeSessionAsync(_currentSession, isReconnect);
    }

    public async Task ProcessRealtimeSessionAsync(RealtimeConversationSession conversationSession, bool isReconnect = false)
    {
        if (isReconnect)
        {
            // 1. Hydrate the conversation history if it is a reconnect.

            //var history = await _currentSession.GetConversationHistoryAsync(cancellationToken);
            //foreach (var item in history)
            //{
            //    _conversationTranscriptionHistory.Add(item);
            //}
        }
    }

    public Task ProcessInboundDataAsync(DataFrame frame, CancellationToken cancellationToken)
    {
        // TODO: Implement the logic to process inbound data frames.
        throw new NotImplementedException();
    }

    public async Task ProcessOutboundDataAsync(DataFrame frame, CancellationToken cancellationToken)
    {
        await _currentSession.StartResponseAsync(cancellationToken);
        await foreach (var update in _currentSession.ReceiveUpdatesAsync(cancellationToken))
        {
            if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
            {
                _logger.LogDebug("Delta Audio Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
                _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);
                _logger.LogDebug("Delta Function Args: {FunctionArguments}", deltaUpdate.FunctionArguments);
                if (deltaUpdate.AudioBytes is not null)
                    // Send data to web socket and other AI agents
                if (deltaUpdate.AudioTranscript is not null)
                    continue; // Need to store the transcript to rehydrate the 
            }


            if (update is ConversationItemStreamingAudioTranscriptionFinishedUpdate transcriptionFinished)
            {

                // {
                //  "event_id": "event_2122",
                //  "type": "conversation.item.input_audio_transcription.completed",
                //  "item_id": "msg_003",
                //  "content_index": 0,
                //  "transcript": "Hello, how are you?"
                // }
                _conversationTranscriptionHistory.Add(transcriptionFinished);
            }
            if (update is ConversationInputTranscriptionFinishedUpdate inputTranscriptionFinished)
            {
                _conversationTranscriptionHistory.Add(inputTranscriptionFinished);
            }

            if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
            {
                _logger.LogDebug($"Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                await mediaStreamingHandler.SendStopAudioCommand(cancellationToken).ConfigureAwait(false);
            }
            await session.HandleToolCallsAsync(update, tools.OfType<AIFunction>().ToList(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
