using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime.AudioDataProvider;
using System;
using System.Buffers;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;

namespace Showcase.Shared.AIExtensions.Realtime;

#pragma warning disable OPENAI002

public class OpenAIVoiceClient : IVoiceClient
{
    private readonly ILogger _logger;
    private readonly RealtimeConversationClient _aiClient;
    private readonly Channel<ReadOnlyMemory<byte>> _audioInboundChannel;
    private readonly CancellationTokenSource _cts = new ();

    private readonly ConcurrentDictionary<Type, List<Func<IOutputWebSocket, ConversationUpdate, Task>>> _eventHandlers
        = new ();

    private static readonly Uri _defaultOpenAIEndpoint = new("https://api.openai.com/v1");

    public ChatClientMetadata Metadata { get; }

    public OpenAIVoiceClient(AzureOpenAIClient aiClient, string modelId, ILogger logger)
    {
        _aiClient = aiClient.GetRealtimeConversationClient(modelId);
        _logger = logger;
        _audioInboundChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
        Uri providerUrl = typeof(AzureOpenAIClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(aiClient) as Uri ?? _defaultOpenAIEndpoint;

        string? model = typeof(AzureOpenAIClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(aiClient) as string;

        Metadata = new("openai", providerUrl, model);
    }

    /// <summary>
    /// Registers a handler for a specific type of ConversationUpdate.
    /// </summary>
    /// <typeparam name="T">The type of ConversationUpdate to handle.</typeparam>
    /// <param name="handler">The handler to invoke when the specified ConversationUpdate is received.</param>
    /// <returns>The current instance of <see cref="IVoiceClient"/>.</returns>
    public IVoiceClient OnConversationUpdate<T>(ConversationUpdateHandler<T> handler) where T : ConversationUpdate
    {
        ArgumentNullException.ThrowIfNull(handler);

        // Wrap the handler to match the Func signature
        Func<IOutputWebSocket, ConversationUpdate, Task> wrappedHandler = async (output, update) =>
        {
            if (update is T typedUpdate)
            {
                await handler(output, typedUpdate).ConfigureAwait(false);
            }
        };

        // Add the wrapped handler to the list for the specific type
        var handlers = _eventHandlers.GetOrAdd(typeof(T), _ => []);
        lock (handlers) // Ensure thread-safe addition
        {
            handlers.Add(wrappedHandler);
        }

        return this;
    }



    public async Task StartConversationAsync(IOutputWebSocket output, RealtimeSessionOptions sessionOptions, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        var aiSession = await _aiClient.StartConversationSessionAsync(cancellationToken).ConfigureAwait(false);

        // Add default handlers for ConversationUpdate types
        OnConversationUpdate<ConversationResponseFinishedUpdate>((_, update) => OnConversationResponseFinishedUpdate(output, update));

        var options = ToOpenAIConversationSessionOptions(sessionOptions);
        await aiSession.ConfigureSessionAsync(options, cancellationToken).ConfigureAwait(false);

        // Start the background audio processing for the input channel
        var processAudioTask = Task.Run(async () => await ProcessInboundAudioChannelAsync(aiSession, cancellationToken).ConfigureAwait(false), cancellationToken);

        // Start the background task to process AI responses
        var getOpenAiStreamResponseTask = Task.Run(async () => await GetOpenAiStreamResponseAsync(aiSession, output, sessionOptions.Tools ?? [], cancellationToken).ConfigureAwait(false), cancellationToken);

        // Start processing the WebSocket and wait for it to complete
        await output.StartProcessingWebSocket(this, cancellationToken).ConfigureAwait(false);

        // Optionally, you can await the other tasks if you need to ensure they complete
        // await Task.WhenAll(processAudioTask, getOpenAiStreamResponseTask).ConfigureAwait(false);
    }
  
    public async Task SendInputAudioAsync(BinaryData audio, CancellationToken cancellationToken = default)
    {
        try
        {
            await _audioInboundChannel.Writer.WriteAsync(audio.ToArray(), cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Attempted to write to a closed channel.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing audio data to the channel.");
            throw;
        }
    }

    private async Task GetOpenAiStreamResponseAsync(RealtimeConversationSession session, IOutputWebSocket mediaStreamingHandler, IList<AITool> tools, CancellationToken cancellationToken = default)
    {
        try
        {
            await session.StartResponseAsync(cancellationToken);
            await foreach (var update in session.ReceiveUpdatesAsync(cancellationToken))
            {
                if (_eventHandlers.TryGetValue(update.GetType(), out var handlers))
                {
                    // Create a copy to prevent issues if handlers are modified during iteration
                    List<Func<IOutputWebSocket, ConversationUpdate, Task>> handlersCopy;
                    lock (handlers)
                    {
                        handlersCopy = [.. handlers];
                    }

                    foreach (var handler in handlersCopy)
                    {
                        try
                        {
                            await handler.Invoke(mediaStreamingHandler, update).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing handler for {UpdateType}", update.GetType().Name);
                        }
                    }
                }

                if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                {
                    _logger.LogDebug("Delta Audio Transcript: {AudioTranscript}", deltaUpdate.AudioTranscript);
                    _logger.LogDebug("Delta TextOnly Update: {Text}", deltaUpdate.Text);
                    _logger.LogDebug("Delta Function Args: {FunctionArguments}", deltaUpdate.FunctionArguments);
                    if (deltaUpdate.AudioBytes is not null)
                        await mediaStreamingHandler.SendInputAudioAsync(deltaUpdate.AudioBytes.ToArray(), cancellationToken).ConfigureAwait(false);
                }

                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    _logger.LogDebug($"Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                    await mediaStreamingHandler.SendStopAudioCommand(cancellationToken).ConfigureAwait(false);
                }

                await session.HandleToolCallsAsync(update, tools.OfType<AIFunction>().ToList(), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AI streaming was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during AI streaming.");
        }
    }


    private async Task ProcessInboundAudioChannelAsync(RealtimeConversationSession session, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var audioChunk in _audioInboundChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                using var audioStream = new MemoryStream(audioChunk.ToArray());
                await session.SendInputAudioAsync(audioStream, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio processing was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data.");
            // Consider implementing retry logic or other recovery mechanisms
        }
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
        foreach(var tool in tools)
        {
            result.Tools.Add(tool);
        }
        return result;
    }

    // Default Handlers

    // Conversation response is done. Always emitted, no matter the final state
    private Task OnConversationResponseFinishedUpdate(IOutputWebSocket output, ConversationResponseFinishedUpdate update)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _audioInboundChannel.Writer.TryComplete();
        _eventHandlers.Clear();
        GC.SuppressFinalize(this);
    }
}
