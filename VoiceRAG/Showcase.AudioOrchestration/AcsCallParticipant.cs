using Azure.Communication.CallAutomation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public class AcsCallParticipant : ConversationParticipant
{
    private readonly WebSocket _socket;
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private readonly SemaphoreSlim _clientSendSemaphore = new(initialCount: 1, maxCount: 1);

    // Buffer size used when reading from a WebSocket.
    private const int BufferSize = 1024 * 2;

    internal Task ParticipantEventProcessing { get; private set; } = Task.CompletedTask;
    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;

    public AcsCallParticipant(
        WebSocket socket,
        string id,
        string name, 
        ILoggerFactory loggerFactory) : base(id, name)
    {
        _socket = socket;
        _logger = loggerFactory.CreateLogger<AcsCallParticipant>();
    }

    public override async Task StartResponseAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger.LogInformation("Starting ACS Call Participant {ParticipantId}", Id);

        ParticipantEventProcessing = Task.Run(() => ProcessParticipantEventsAsync(_cts.Token), _cts.Token);
        InternalEventProcessing = Task.Run(() => ProcessInboundEvents(_cts.Token), _cts.Token);

        _logger.LogInformation("Started ACS Call Participant {ParticipantId}", Id);
        await Task.WhenAll(ParticipantEventProcessing, InternalEventProcessing).ConfigureAwait(false);
    }

    private async Task ProcessParticipantEventsAsync(CancellationToken cancellationToken)
    {
        var buffer = _bufferPool.Rent(BufferSize);

        try
        {
            while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await _socket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Socket {SocketId} was closed: {CloseDescription}", Id, _socket.CloseStatusDescription);
                    break;
                }

                var resultData = buffer.Take(result.Count).ToArray();

                if (TryGetAudioFromResponse(resultData) is RealtimeAudioDeltaEvent audioEvent && !audioEvent.IsEmpty)
                {
                    await _outboundChannel.Writer.WriteAsync(audioEvent, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("WebSocket receive operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error receiving WebSocket messages.");
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }

    private async Task ProcessInboundEvents(CancellationToken cancellationToken)
    {
        await foreach (var internalEvent in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if (internalEvent is RealtimeAudioDeltaEvent audioEvent) await SendAudioAsync(audioEvent, cancellationToken).ConfigureAwait(false);

            if (internalEvent is RealtimeStopAudioEvent stopAudioEvent) await SendStopAudioAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendStopAudioAsync(CancellationToken cancellationToken)
    {
        var input = OutStreamingData.GetStopAudioForOutbound();
        await SendCommandAsync(BinaryData.FromString(input), cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAudioAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        if(audioEvent.AudioData is null) return;
        var input = ConvertInboundAudioToAcsEvent(audioEvent);
        await SendCommandAsync(input, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendCommandAsync(BinaryData data, CancellationToken cancellationToken)
    {
        ArraySegment<byte> messageBytes = new(data.ToArray());
        await _clientSendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _socket.SendAsync(
            messageBytes,
            WebSocketMessageType.Text, // TODO: extensibility for binary messages
            endOfMessage: true,
            cancellationToken);
        }
        finally
        {
            _clientSendSemaphore.Release();
        }
    }

    private BinaryData ConvertInboundAudioToAcsEvent(RealtimeAudioDeltaEvent audioIn)
    {
        var audio = OutStreamingData.GetAudioDataForOutbound(audioIn.AudioData.ToArray());
        return BinaryData.FromString(audio);
    }

    private RealtimeAudioDeltaEvent? TryGetAudioFromResponse(byte[] audioOut)
    {
        string data = Encoding.UTF8.GetString(audioOut).TrimEnd('\0');

        var input = StreamingData.Parse(data);
        if(input is not AudioData audioData || audioData.IsSilent) return null;

        return new RealtimeAudioDeltaEvent(AudioData: new BinaryData(audioData.Data), ServiceEventType: MediaKind.AudioData.ToString(), SourceId: Id);
    }

    public override void Dispose()
    {
        _socket.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
