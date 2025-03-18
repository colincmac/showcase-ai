#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public class VoiceConversationParticipant : IConversationParticipant, IDisposable
{
    public string ParticipantId { get; set; }
    public string? ParticipantName { get; init; }

    public ChannelReader<BinaryData> Reader { get; init; }

    private bool _gracefulClose;

    private readonly Channel<BinaryData> _participantFeed;
    private CancellationTokenSource _cts = new();
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

    private readonly RealtimeConversationClient _aiClient;
    private readonly ConversationSessionOptions _sessionOptions;
    private readonly ILogger _logger;
    private RealtimeConversationSession? _currentSession;
    private readonly List<IConversationParticipant> conversationParticipants = new();
    internal Task OutboundTask { get; private set; } = Task.CompletedTask;
    internal Task InboundTask { get; private set; } = Task.CompletedTask;
    private readonly ConversationHistory _conversationTranscriptionHistory = new();

    public VoiceConversationParticipant( RealtimeConversationClient aiClient, ConversationSessionOptions sessionOptions, ILogger logger, string participantId, string? participantName = default)
    {
        ParticipantId = participantId;
        ParticipantName = participantName;
        _participantFeed = Channel.CreateBounded<BinaryData>(new BoundedChannelOptions(channelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public bool AcceptsDataType(WellKnownDataType modality)
    {
        // Check if the modality is supported by the Participant channel.
        return modality switch
        {
            WellKnownDataType.Text => true,
            WellKnownDataType.Audio => true,
            WellKnownDataType.Video => false,
            WellKnownDataType.SensorData => false,
            _ => throw new NotSupportedException($"Data type {modality} is not supported by this AI.")
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var isReconnect = false;

        if (_currentSession == null)
        {
            // Start the conversation session with the AI client and configure with options.
            _currentSession = await _aiClient.StartConversationSessionAsync(_cts.Token);
            await _currentSession.ConfigureSessionAsync(_sessionOptions, _cts.Token);
        }
        else
        {
            isReconnect = true;
        }
        OutboundTask = Task.Run(() => ProcessInbound(_cts.Token), _cts.Token);
        InboundTask = Task.Run(() => ProcessOutboundAsync(_cts.Token), _cts.Token);
    }

    public async Task SendDataAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        await _participantFeed.Writer.WriteAsync(data, cancellationToken);
    }

    public void BroadcastTo(IEnumerable<IConversationParticipant> participants)
    {
        conversationParticipants.AddRange(participants);
    }

    private async Task ProcessInbound(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var update in _participantFeed.Reader.ReadAllAsync(cancellationToken))
                {
                    //Todo:
                }

            }
        }
        finally
        {
        }
    }

    private async Task ProcessOutboundAsync(CancellationToken cancellationToken)
    {
        byte[] receiveBuffer = _bufferPool.Rent(4096);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var update in _currentSession.ReceiveUpdatesAsync(cancellationToken))
                {
                    // Write to participants
                    _participantFeed.Writer.WriteAsync(update.GetRawContent(), cancellationToken);
                    
                    //// Handle the updates
                    //if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                    //{
                    //    _logger.LogDebug("Delta Audio Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
                    //    _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);
                    //    _logger.LogDebug("Delta Function Args: {FunctionArguments}", deltaUpdate.FunctionArguments);
                    //    if (deltaUpdate.AudioBytes is not null)
                    //        // Send data to web socket and other AI agents
                    //        if (deltaUpdate.AudioTranscript is not null)
                    //            continue; // Need to store the transcript to rehydrate the 
                    //}


                    //if (update is ConversationItemStreamingAudioTranscriptionFinishedUpdate transcriptionFinished)
                    //{

                    //    // {
                    //    //  "event_id": "event_2122",
                    //    //  "type": "conversation.item.input_audio_transcription.completed",
                    //    //  "item_id": "msg_003",
                    //    //  "content_index": 0,
                    //    //  "transcript": "Hello, how are you?"
                    //    // }
                    //    _conversationTranscriptionHistory.Add(transcriptionFinished);
                    //}
                    //if (update is ConversationInputTranscriptionFinishedUpdate inputTranscriptionFinished)
                    //{
                    //    _conversationTranscriptionHistory.Add(inputTranscriptionFinished);
                    //}

                    //if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                    //{
                    //    _logger.LogDebug($"Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                    //    //await mediaStreamingHandler.SendStopAudioCommand(cancellationToken).ConfigureAwait(false);
                    //}
                    //await _currentSession.HandleToolCallsAsync(update, tools.OfType<AIFunction>().ToList(), cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
        }
    }
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _participantFeed.Writer.Complete();
    }
}
