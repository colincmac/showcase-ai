#pragma warning disable OPENAI002

using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class ConversationSession: IAsyncDisposable
{
    private readonly ILogger<ConversationSession> _logger;
    private readonly Channel<AudioFrame> _audioQueue;
    private readonly IConversationStore _transcriptionStore; // e.g., Redis-backed store
    private readonly ConversationSessionOptions _conversationSessionOptions;
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private readonly RealtimeConversationClient _conversationClient;

    private string _conversationSessionId = string.Empty;
    private CancellationTokenSource _cts = new();
    private WebSocket _outboundAudioSocket;

    public ConversationSession(AzureOpenAIClient openAIClient, IOptions<ConversationSessionOptions> sessionOptions, IConversationStore transcriptionStore, ILogger<ConversationSession> logger)
    {
        _conversationSessionOptions = sessionOptions.Value;
        _transcriptionStore = transcriptionStore;
        _logger = logger;
        _conversationClient = openAIClient.GetRealtimeConversationClient(_conversationSessionOptions.ModelId);
        _audioQueue = Channel.CreateBounded<AudioFrame>(new BoundedChannelOptions(5000)
        {
            SingleWriter = true,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait // backpressure if OpenAI is slow
        });
    }

    public async Task StartConversationAsync(
        string conversationSessionId,
        WebSocket outboundAudioSocket,
        CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _outboundAudioSocket = outboundAudioSocket;
        _conversationSessionId = conversationSessionId;

        var history = await _transcriptionStore.GetConversationHistoryAsync(_conversationSessionId);
        if (history == null) return;
        var t = await _conversationClient.StartConversationSessionAsync(_cts.Token);
        foreach (var update in history)
        {
            Mutate(update);
            Version++;
        }
    }



    public int Version { get; private set; }

    private readonly List<ConversationUpdate> _conversationHistory = [];

    public IReadOnlyCollection<ConversationUpdate> ConversationHistory => _conversationHistory.AsReadOnly();

    public void AddConversationEvent(ConversationUpdate eventItem) => _conversationHistory.Add(eventItem);

    public void RemoveConversationEvent(ConversationUpdate eventItem) => _conversationHistory.Remove(eventItem);

    public void ClearConversationEvents() => _conversationHistory.Clear();

    private void Mutate(ConversationUpdate eventItem) =>
    ((dynamic)this).On((dynamic)eventItem);

    protected void Apply(IEnumerable<ConversationUpdate> eventItems)
    {
        foreach (var eventItem in eventItems)
        {
            Apply(eventItem);
        }
    }

    protected void Apply(ConversationUpdate eventItem)
    {
        Mutate(eventItem);
        AddConversationEvent(eventItem);
    }

    #region Event Handlers
    public async Task On(ConversationItemStreamingPartDeltaUpdate eventItem)
    {

    }

    public async Task On(ConversationInputTranscriptionFinishedUpdate eventItem)
    {

    }

    public async Task On(ConversationInputSpeechStartedUpdate eventItem)
    {

    }
    #endregion

    public async ValueTask DisposeAsync()
    {
        try { if (_acsSocket?.State == WebSocketState.Open) await _acsSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None); }
        catch { /* ignore */ }
        _cts.Cancel();
        _audioQueue.Writer.TryComplete();
        if (_openAiSession != null)
            await _openAiSession.DisposeAsync();
    }
}
