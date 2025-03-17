//using System;
//using System.Buffers;
//using System.Net.WebSockets;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Threading.Channels;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Logging;
//namespace Showcase.AudioOrchestration;


///// <summary>
///// Handles a single call session by bridging the ACS WebSocket with the Azure OpenAI realtime WebSocket.
///// </summary>
//public class CallSession
//{
//    private readonly WebSocket _acsSocket;
//    private ClientWebSocket _openAiSocket;
//    private readonly IDistributedCache _redis;
//    private readonly ILogger _logger;
//    private readonly CancellationToken _externalToken;
//    private readonly CancellationTokenSource _cts = new();

//    // Bounded channels provide FIFO ordering and backpressure.
//    private readonly Channel<byte[]> _acsToOpenAiChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
//    {
//        FullMode = BoundedChannelFullMode.Wait
//    });
//    private readonly Channel<byte[]> _openAiToAcsChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
//    {
//        FullMode = BoundedChannelFullMode.Wait
//    });

//    // The Azure OpenAI Realtime endpoint (set via configuration in a real solution).
//    private readonly Uri _openAiUri = new("wss://your-openai-endpoint");

//    // Buffer size used when reading from a WebSocket.
//    private const int BufferSize = 2048;

//    public CallSession(WebSocket acsSocket, IDistributedCache redis, ILogger logger, CancellationToken externalToken)
//    {
//        _acsSocket = acsSocket;
//        _redis = redis;
//        _logger = logger;
//        _externalToken = externalToken;
//    }

//    /// <summary>
//    /// Starts the call session: connects to OpenAI, then launches tasks to forward audio in both directions.
//    /// </summary>
//    public async Task StartAsync()
//    {
//        // Create a linked cancellation token.
//        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_externalToken, _cts.Token);

//        // Optionally, update call state in Redis.
//        await SaveCallStateAsync("Started", linkedCts.Token);

//        // Connect to Azure OpenAI with automatic reconnection.
//        await ConnectToOpenAiWithRetriesAsync(linkedCts.Token);

//        // Start tasks for bidirectional forwarding.
//        var acsReceiveTask = ReceiveFromWebSocketAsync(_acsSocket, _acsToOpenAiChannel.Writer, "ACS", linkedCts.Token);
//        var openAiSendTask = SendToWebSocketAsync(_openAiSocket, _acsToOpenAiChannel.Reader, "OpenAI", linkedCts.Token);

//        var openAiReceiveTask = ReceiveFromWebSocketAsync(_openAiSocket, _openAiToAcsChannel.Writer, "OpenAI", linkedCts.Token);
//        var acsSendTask = SendToWebSocketAsync(_acsSocket, _openAiToAcsChannel.Reader, "ACS", linkedCts.Token);

//        // Wait until any of the tasks complete or fail.
//        await Task.WhenAny(acsReceiveTask, openAiSendTask, openAiReceiveTask, acsSendTask);

//        // Cancel all tasks if one ends.
//        _cts.Cancel();

//        // Update call state to "Ended".
//        await SaveCallStateAsync("Ended", linkedCts.Token);

//        // Close both WebSockets gracefully.
//        await CloseSocketAsync(_acsSocket, "ACS", CancellationToken.None);
//        await CloseSocketAsync(_openAiSocket, "OpenAI", CancellationToken.None);
//    }

//    /// <summary>
//    /// Attempts to connect to the OpenAI endpoint using exponential backoff (up to 3 attempts).
//    /// </summary>
//    private async Task ConnectToOpenAiWithRetriesAsync(CancellationToken token)
//    {
//        const int maxRetries = 3;
//        int attempt = 0;
//        Exception lastException = null;
//        while (attempt < maxRetries && !token.IsCancellationRequested)
//        {
//            try
//            {
//                _openAiSocket = new ClientWebSocket();
//                // Optionally add headers for authentication here:
//                // _openAiSocket.Options.SetRequestHeader("Authorization", "Bearer YOUR_TOKEN");

//                _logger.LogInformation("Connecting to OpenAI (attempt {Attempt})...", attempt + 1);
//                await _openAiSocket.ConnectAsync(_openAiUri, token);
//                _logger.LogInformation("Connected to OpenAI.");
//                return;
//            }
//            catch (Exception ex)
//            {
//                lastException = ex;
//                _logger.LogError(ex, "Failed to connect to OpenAI on attempt {Attempt}.", attempt + 1);
//                attempt++;
//                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff.
//                await Task.Delay(delay, token);
//            }
//        }
//        throw new Exception("Could not connect to OpenAI after multiple attempts.", lastException);
//    }

//    /// <summary>
//    /// Receives data from a WebSocket and writes complete audio messages into a channel.
//    /// </summary>
//    private async Task ReceiveFromWebSocketAsync(WebSocket socket, ChannelWriter<byte[]> writer, string socketName, CancellationToken token)
//    {
//        try
//        {
//            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
//            try
//            {
//                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
//                {
//                    var segment = new ArraySegment<byte>(buffer);
//                    var result = await socket.ReceiveAsync(segment, token);
//                    if (result.MessageType == WebSocketMessageType.Close)
//                    {
//                        _logger.LogInformation("{SocketName} initiated close.", socketName);
//                        break;
//                    }
//                    // Note: For simplicity, we assume each ReceiveAsync call returns a complete message.
//                    var data = new byte[result.Count];
//                    Array.Copy(buffer, data, result.Count);
//                    // Write the received audio data to the channel (backpressure applies if the channel is full).
//                    await writer.WriteAsync(data, token);
//                }
//            }
//            finally
//            {
//                ArrayPool<byte>.Shared.Return(buffer);
//                writer.Complete();
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Receive loop for {SocketName} canceled.", socketName);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error in ReceiveFromWebSocketAsync for {SocketName}.", socketName);
//        }
//    }

//    /// <summary>
//    /// Reads audio messages from a channel and sends them over the specified WebSocket.
//    /// </summary>
//    private async Task SendToWebSocketAsync(WebSocket socket, ChannelReader<byte[]> reader, string socketName, CancellationToken token)
//    {
//        try
//        {
//            await foreach (var message in reader.ReadAllAsync(token))
//            {
//                if (socket.State != WebSocketState.Open)
//                {
//                    _logger.LogWarning("Socket {SocketName} is not open for sending.", socketName);
//                    break;
//                }
//                var segment = new ArraySegment<byte>(message);
//                await socket.SendAsync(segment, WebSocketMessageType.Binary, endOfMessage: true, token);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Send loop for {SocketName} canceled.", socketName);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error in SendToWebSocketAsync for {SocketName}.", socketName);
//        }
//    }

//    /// <summary>
//    /// Closes the given WebSocket gracefully.
//    /// </summary>
//    private async Task CloseSocketAsync(WebSocket socket, string socketName, CancellationToken token)
//    {
//        if (socket?.State == WebSocketState.Open)
//        {
//            try
//            {
//                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
//                _logger.LogInformation("{SocketName} closed gracefully.", socketName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error while closing {SocketName}.", socketName);
//            }
//        }
//    }

//    /// <summary>
//    /// Saves a simple call state to Redis (for example, to help with reconnection and context hydration).
//    /// </summary>
//    private async Task SaveCallStateAsync(string state, CancellationToken token)
//    {
//        // In a real implementation, use a unique call identifier.
//        string callKey = "call:12345";
//        await _redis.SetStringAsync(callKey, state, token);
//        _logger.LogInformation("Call state updated to '{State}' in Redis.", state);
//    }
//}
