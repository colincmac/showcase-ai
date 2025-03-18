#pragma warning disable OPENAI002

using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.RealtimeConversation;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class AgentDataMultiplexer
{

    private readonly ILogger<AgentDataMultiplexer> _logger;
    private readonly int _expectedFrameSize; // e.g., 960 bytes for 24kHz 20ms frame
    private readonly int _channelCapacity = 5000; // Default channel capacity
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private readonly IConversationStore _conversationStore; // e.g., Redis-backed store
    private readonly RealtimeConversationClient _realtimeConversationClient;

    private CancellationTokenSource _cts = new();
    private WebSocket? _socket;
    private string _conversationId;

    // Channels for inbound (from ACS) and outbound (to ACS) audio.
    private Channel<DataFrame> _inboundDataChannel;
    private Channel<DataFrame> _outboundDataChannel;

    private readonly List<IAgentChannel> _aiAgents = [];

    public AgentDataMultiplexer(RealtimeConversationClient realtimeConversationClient, IConversationStore conversationStore, IOptions<RealtimeConversationOptions> options, ILogger<AgentDataMultiplexer> logger)
    {
        _logger = logger;
        _expectedFrameSize = options.Value.ExpectedFrameSize;
        _channelCapacity = options.Value.ChannelCapacity;
        _conversationStore = conversationStore;
        _realtimeConversationClient = realtimeConversationClient;

        // Create bounded channels to keep memory usage in check.
        _inboundDataChannel = Channel.CreateBounded<DataFrame>(new BoundedChannelOptions(_channelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        _outboundDataChannel = Channel.CreateBounded<DataFrame>(new BoundedChannelOptions(_channelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task StartAsync(string conversationId, WebSocket socket, CancellationToken cancellationToken = default)
    {
        _conversationId = conversationId;
        _socket = socket;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        var session = await _realtimeConversationClient.StartConversationSessionAsync(_cts.Token);

        // Start tasks to read from the socket and distribute both inbound and outbound audio.
        var socketReaderTask = Task.Run(() => ReadFromSocketAsync(_cts.Token), _cts.Token);
        var inboundDistributorTask = Task.Run(() => DistributeInboundAIDataAsync(_cts.Token), _cts.Token);
        var outboundDistributorTask = Task.Run(() => DistributeAIGeneratedOutboundDataAsync(_cts.Token), _cts.Token);

        await Task.WhenAll(socketReaderTask, inboundDistributorTask, outboundDistributorTask);
    }

    public virtual DataFrame? ParseInboundAudioToListeners(byte[] buffer, int totalBytes)
    {
        string json = Encoding.UTF8.GetString(buffer, 0, totalBytes);
        var input = StreamingData.Parse(json);
        if(input is AudioData audioData)
        {
            return new DataFrame(audioData.Data, audioData.IsSilent);
        }
        else
        {
            _logger.LogWarning("Failed to parse inbound data.");
            return null;
        }
    }

    public virtual BinaryData ParseOutboundDataToListeners(byte[] buffer)
    {
        var audio = OutStreamingData.GetAudioDataForOutbound(buffer);
        return BinaryData.FromString(audio);
    }

    /// <summary>
    /// Registers an AI agent.
    /// </summary>
    public void RegisterAgent(IAgentChannel agent) => _aiAgents.Add(agent);

    /// <summary>
    /// Reads messages from the ACS WebSocket. Expects JSON messages with a base64‐encoded audio frame.
    /// </summary>
    private async Task ReadFromSocketAsync(CancellationToken cancellationToken)
    {
        byte[] receiveBuffer = _bufferPool.Rent(4096);
        try
        {
            while (!cancellationToken.IsCancellationRequested && _socket?.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(receiveBuffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by remote.");
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    int totalBytes = result.Count;
                    while (!result.EndOfMessage)
                    {
                        if (totalBytes >= receiveBuffer.Length)
                        {
                            byte[] newBuffer = _bufferPool.Rent(receiveBuffer.Length * 2);
                            Array.Copy(receiveBuffer, newBuffer, totalBytes);
                            _bufferPool.Return(receiveBuffer);
                            receiveBuffer = newBuffer;
                        }
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, totalBytes, receiveBuffer.Length - totalBytes), cancellationToken);
                        totalBytes += result.Count;
                    }
                    var dataFrame = ParseInboundAudioToListeners(receiveBuffer, totalBytes);
                    if (dataFrame != null)  await _inboundDataChannel.Writer.WriteAsync(dataFrame, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from WebSocket.");
        }
        finally
        {
            _inboundDataChannel.Writer.Complete();
            _bufferPool.Return(receiveBuffer);
        }
    }

    /// <summary>
    /// Distributes inbound audio frames to all registered agents.
    /// </summary>
    private async Task DistributeInboundAIDataAsync(CancellationToken cancellationToken)
    {
        await foreach (var frame in _inboundDataChannel.Reader.ReadAllAsync(cancellationToken))
        {
            // Deliver inbound to agents.
            foreach (var agent in _aiAgents)
            {
                _ = agent.ProcessInboundDataAsync(frame, cancellationToken);
            }
            // Return the buffer to the pool.
            _bufferPool.Return(frame.Buffer);
        }
    }

    /// <summary>
    /// Distributes outbound frames (produced by the primary agent) to auditing agents and sends them back to ACS.
    /// </summary>
    private async Task DistributeAIGeneratedOutboundDataAsync(CancellationToken cancellationToken)
    {
        await foreach (var frame in _outboundDataChannel.Reader.ReadAllAsync(cancellationToken))
        {
            // Let auditing agents observe outbound audio.
            foreach (var agent in _aiAgents)
            {
                _ = agent.ProcessOutboundDataAsync(frame, cancellationToken);
            }
            // Forward outbound audio back to ACS.
            if (_socket?.State == WebSocketState.Open)
            {
                var data = ParseOutboundDataToListeners(frame.Buffer);
                await _socket.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
            _bufferPool.Return(frame.Buffer);
        }
    }

    /// <summary>
    /// Called by the primary agent to publish outbound audio for distribution.
    /// </summary>
    public async Task PublishOutboundAsync(DataFrame frame, CancellationToken cancellationToken)
    {
        await _outboundDataChannel.Writer.WriteAsync(frame, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _inboundDataChannel.Writer.Complete();
        _outboundDataChannel.Writer.Complete();
        if (_socket != null && _socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
        }
    }
}
