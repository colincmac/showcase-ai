//using Azure.Communication.CallAutomation;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.WebSockets;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace Showcase.AudioOrchestration;

///// <summary>
///// The AudioMultiplexer is responsible for reading bidirectional audio from a single WebSocket and distributing
///// inbound audio to all agents, and outbound audio (from the primary agent) both to auditing agents and back to the WebSocket.
///// </summary>
//public class AudioMultiplexer : IAsyncDisposable
//{
//    // Shared cancellation token source for all tasks.
//    private CancellationTokenSource _cts = new();

//    private readonly WebSocket _socket;
//    private readonly int _expectedFrameSize;
//    private readonly ILogger<AudioMultiplexer> _logger;
//    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

//    // Channels for inbound (from ACS) and outbound (to ACS) audio.
//    private readonly Channel<AudioFrame> _inboundChannel;
//    private readonly Channel<AudioFrame> _outboundChannel;

//    // Registered agents.
//    private readonly List<IConversationAgent> _conversationAgents;
//    private readonly List<IAuditor> _auditingAgents;

//    public AudioMultiplexer(WebSocket socket, int expectedFrameSize, ILogger<AudioMultiplexer> logger)
//    {
//        _socket = socket;
//        _expectedFrameSize = expectedFrameSize;
//        _logger = logger;

//        // Create bounded channels to keep memory usage in check.
//        _inboundChannel = Channel.CreateBounded<AudioFrame>(new BoundedChannelOptions(10000)
//        {
//            SingleReader = true,
//            SingleWriter = true,
//            FullMode = BoundedChannelFullMode.Wait // backpressure if the channel is full
//        });

//        _outboundChannel = Channel.CreateBounded<AudioFrame>(new BoundedChannelOptions(10000)
//        {
//            SingleReader = true,
//            SingleWriter = true,
//            FullMode = BoundedChannelFullMode.Wait // backpressure if the channel is full
//        });

//        _conversationAgents = [];
//        _auditingAgents = [];
//    }

//    /// <summary>
//    /// Registers a primary agent (usually only one exists).
//    /// </summary>
//    public void RegisterPrimaryAgent(IConversationAgent agent) => _conversationAgents.Add(agent);

//    /// <summary>
//    /// Registers an auditing agent (e.g. quality or compliance).
//    /// </summary>
//    public void RegisterAuditingAgent(IAuditor agent) => _auditingAgents.Add(agent);

//    /// <summary>
//    /// Starts reading from the ACS WebSocket and dispatching audio frames.
//    /// </summary>
//    public async Task StartAsync(CancellationToken cancellationToken)
//    {
//        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

//        // Start tasks to read from the socket and distribute both inbound and outbound audio.
//        var socketReaderTask = Task.Run(() => ReadFromSocketAsync(_cts.Token), _cts.Token);
//        var inboundDistributorTask = Task.Run(() => DistributeInboundAsync(_cts.Token), _cts.Token);
//        var outboundDistributorTask = Task.Run(() => DistributeOutboundAsync(_cts.Token), _cts.Token);

//        await Task.WhenAll(socketReaderTask, inboundDistributorTask, outboundDistributorTask);
//    }


//    /// <summary>
//    /// Reads audio packets from WebSocket and adds them to the inbound channel.
//    /// </summary>
//    private async Task ReadFromSocketAsync(CancellationToken cancellationToken)
//    {
//        byte[] receiveBuffer = _bufferPool.Rent(4096);
//        try
//        {
//            while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
//            {
//                var result = await _socket.ReceiveAsync(receiveBuffer, cancellationToken);

//                if (result.MessageType == WebSocketMessageType.Close)
//                {
//                    _logger.LogWarning("WebSocket closed by remote.");
//                    break;
//                }

//                if (result.MessageType == WebSocketMessageType.Text)
//                {
//                    int totalBytes = result.Count;
//                    while (!result.EndOfMessage)
//                    {
//                        if (totalBytes >= receiveBuffer.Length)
//                        {
//                            byte[] newBuffer = _bufferPool.Rent(receiveBuffer.Length * 2);
//                            Array.Copy(receiveBuffer, newBuffer, totalBytes);
//                            _bufferPool.Return(receiveBuffer);
//                            receiveBuffer = newBuffer;
//                        }
//                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, totalBytes, receiveBuffer.Length - totalBytes), cancellationToken);
//                        totalBytes += result.Count;
//                    }
//                    string json = Encoding.UTF8.GetString(receiveBuffer, 0, totalBytes);
//                    var input = StreamingData.Parse(json);
//                    if (input is AudioData audioData)
//                    {
//                        byte[] audioBuffer = _bufferPool.Rent(_expectedFrameSize);
//                        await _inboundChannel.Writer.WriteAsync(new AudioFrame(audioData.Data, audioData.IsSilent), cancellationToken);
//                    }
//                }
//            }
//        }
//        catch (OperationCanceledException) { }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error reading from ACS WebSocket.");
//        }
//        finally
//        {
//            _bufferPool.Return(receiveBuffer);
//            _inboundChannel.Writer.Complete();
//        }
//    }

//    /// <summary>
//    /// Distributes inbound audio frames to all registered agents.
//    /// </summary>
//    private async Task DistributeInboundAsync(CancellationToken cancellationToken)
//    {
//        await foreach (var frame in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
//        {
//            // Deliver inbound audio to primary agents.
//            foreach (var agent in _conversationAgents)
//            {
//                _ = agent.ProcessInboundAudioAsync(frame, cancellationToken);
//            }
//            // Let auditing agents observe inbound audio.
//            foreach (var auditor in _auditingAgents)
//            {
//                _ = auditor.ObserveInboundAudioAsync(frame, cancellationToken);
//            }
//            // Return the buffer to the pool.
//            _bufferPool.Return(frame.Buffer);
//        }
//    }

//    /// <summary>
//    /// Distributes outbound audio frames (produced by the primary agent) to auditing agents and sends them back to ACS.
//    /// </summary>
//    private async Task DistributeOutboundAsync(CancellationToken cancellationToken)
//    {
//        await foreach (var frame in _outboundChannel.Reader.ReadAllAsync(cancellationToken))
//        {
//            // Let auditing agents observe outbound audio.
//            foreach (var auditor in _auditingAgents)
//            {
//                _ = auditor.ObserveOutboundAudioAsync(frame, cancellationToken);
//            }
//            // Forward outbound audio back to ACS.
//            if (_socket.State == WebSocketState.Open)
//            {
//                string json = OutgoingAcsAudio.FromAudioChunk(frame.Buffer);
//                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
//                await _socket.SendAsync(jsonBytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
//            }
//            _bufferPool.Return(frame.Buffer);
//        }
//    }

//    /// <summary>
//    /// Called by the primary agent to publish outbound audio for distribution.
//    /// </summary>
//    public async Task PublishOutboundAudioAsync(AudioFrame frame, CancellationToken cancellationToken)
//    {
//        await _outboundChannel.Writer.WriteAsync(frame, cancellationToken);
//    }

//    public async ValueTask DisposeAsync()
//    {
//        _cts.Cancel();
//        _inboundChannel.Writer.Complete();
//        _outboundChannel.Writer.Complete();
//        if (_socket != null && _socket.State == WebSocketState.Open)
//        {
//            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
//        }
//    }

//}
