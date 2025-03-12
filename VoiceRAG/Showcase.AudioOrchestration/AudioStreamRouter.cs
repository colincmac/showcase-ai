#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
//Azure OpenAI SDK (includes RealtimeConversationSession)
using Azure;
using OpenAI.RealtimeConversation;           // for AzureKeyCredential, etc.

namespace Showcase.AudioOrchestration;



// Data models for ACS JSON messages
public record AudioMetadata(int SampleRate, int Channels, int Length);
public record AudioFrame(byte[] PcmData, bool IsSilent);

/// <summary>
/// Manages buffering of ACS audio and forwarding to OpenAI, and vice versa, for one call session.
/// </summary>
public class AudioStreamRouter //: IAsyncDisposable
{
    //private readonly WebSocket _acsSocket;
    //private RealtimeConversationSession _openAiSession;
    //private readonly Channel<AudioFrame> _audioQueue;
    //private readonly ILogger<AudioStreamRouter> _logger;
    //private readonly string _callId;
    //private readonly IConversationStore _transcriptStore; // e.g., Redis-backed store
    //private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    //private readonly int _pcmFrameSize; // expected bytes per PCM frame (from metadata)
    //private volatile bool _openAiRestarting = false;
    //private CancellationTokenSource _cts = new();

    //public AudioStreamRouter(string callId, WebSocket acsSocket, RealtimeConversationSession openAiSession,
    //                          AudioMetadata audioFormat, ILogger<AudioStreamRouter> logger, IConversationStore transcriptStore)
    //{
    //    _callId = callId;
    //    _acsSocket = acsSocket;
    //    _openAiSession = openAiSession;
    //    _logger = logger;
    //    _transcriptStore = transcriptStore;
    //    _pcmFrameSize = audioFormat.Length;  // e.g., 960 bytes for 24kHz 20ms frame
    //    // Bounded channel to buffer audio frames (capacity can be tuned)
    //    _audioQueue = Channel.CreateBounded<AudioFrame>(new BoundedChannelOptions(5000)
    //    {
    //        SingleWriter = true,
    //        SingleReader = true,
    //        FullMode = BoundedChannelFullMode.Wait // backpressure if OpenAI is slow
    //    });
    //}

    ///// <summary>Starts the bidirectional audio streaming between ACS and OpenAI.</summary>
    //public async Task StartAsync()
    //{
    //    // Begin tasks for receiving ACS audio and processing OpenAI responses
    //    var recvTask = ReceiveFromAcsAsync(_cts.Token);
    //    var openAiTask = ProcessOpenAiAsync(_cts.Token);
    //    _logger.LogInformation("AudioStreamRouter started for Call {CallId}.", _callId);

    //    // Wait for either task to finish (error or normal closure)
    //    var completed = await Task.WhenAny(recvTask, openAiTask);
    //    // If one completed due to error, cancel the other
    //    if (completed.IsFaulted)
    //    {
    //        _logger.LogError(completed.Exception, "Error in audio routing for Call {CallId}", _callId);
    //        _cts.Cancel();
    //    }
    //    // Await both to ensure cleanup
    //    try { await recvTask; } catch { /* handled above */ }
    //    try { await openAiTask; } catch { /* handled above */ }
    //    _logger.LogInformation("AudioStreamRouter ending for Call {CallId}.", _callId);
    //}

    ///// <summary>Receives audio from ACS WebSocket, buffers it, and forwards to OpenAI.</summary>
    //private async Task ReceiveFromAcsAsync(CancellationToken cancelToken)
    //{
    //    // Buffer for receiving WebSocket messages (large enough for one JSON frame)
    //    byte[] receiveBuffer = _bufferPool.Rent(4096);
    //    try
    //    {
    //        // First message should be AudioMetadata JSON
    //        WebSocketReceiveResult result = await _acsSocket.ReceiveAsync(receiveBuffer, cancelToken);
    //        if (result.MessageType == WebSocketMessageType.Text)
    //        {
    //            string metaJson = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
    //            var doc = JsonDocument.Parse(metaJson);
    //            if (doc.RootElement.GetProperty("kind").GetString() == "AudioMetadata")
    //            {
    //                var meta = doc.RootElement.GetProperty("audioMetadata");
    //                int sr = meta.GetProperty("sampleRate").GetInt32();
    //                int ch = meta.GetProperty("channels").GetInt32();
    //                int len = meta.GetProperty("length").GetInt32();
    //                _logger.LogInformation("Received AudioMetadata: {SampleRate} Hz, {Channels} channel(s), frame length {Length} bytes", sr, ch, len);
    //                // (We already set _pcmFrameSize from constructor AudioMetadata)
    //            }
    //        }

    //        // Loop to receive audio frames from ACS
    //        while (!cancelToken.IsCancellationRequested && _acsSocket.State == WebSocketState.Open)
    //        {
    //            result = await _acsSocket.ReceiveAsync(receiveBuffer, cancelToken);
    //            if (result.MessageType == WebSocketMessageType.Close)
    //            {
    //                _logger.LogWarning("ACS WebSocket closed for Call {CallId}. Code {CloseStatus}, Reason: {CloseReason}",
    //                                    _callId, result.CloseStatus, result.CloseStatusDescription);
    //                await _acsSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledged", CancellationToken.None);
    //                break;
    //            }
    //            // Handle possibly fragmented messages by looping until EndOfMessage
    //            int totalBytes = result.Count;
    //            while (!result.EndOfMessage)
    //            {
    //                if (totalBytes >= receiveBuffer.Length)
    //                {
    //                    // resize buffer if needed for exceptionally large message
    //                    byte[] newBuf = _bufferPool.Rent(receiveBuffer.Length * 2);
    //                    Array.Copy(receiveBuffer, newBuf, totalBytes);
    //                    _bufferPool.Return(receiveBuffer);
    //                    receiveBuffer = newBuf;
    //                }
    //                result = await _acsSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, totalBytes, receiveBuffer.Length - totalBytes), cancelToken);
    //                totalBytes += result.Count;
    //            }

    //            if (result.MessageType == WebSocketMessageType.Text)
    //            {
    //                string json = Encoding.UTF8.GetString(receiveBuffer, 0, totalBytes);
    //                // Parse the JSON to extract audio data
    //                using var jsonDoc = JsonDocument.Parse(json);
    //                var root = jsonDoc.RootElement;
    //                if (root.GetProperty("kind").GetString() == "AudioData")
    //                {
    //                    var audioElem = root.GetProperty("audioData");
    //                    bool silent = audioElem.GetProperty("silent").GetBoolean();
    //                    string base64Data = audioElem.GetProperty("data").GetString();
    //                    // If not silent, decode audio data and enqueue for OpenAI
    //                    if (!silent && !string.IsNullOrEmpty(base64Data))
    //                    {
    //                        byte[] pcmBuffer = _bufferPool.Rent(_pcmFrameSize);
    //                        // decode base64 directly into our rented buffer
    //                        if (!Convert.TryFromBase64String(base64Data, pcmBuffer, out int bytesWritten))
    //                        {
    //                            _logger.LogWarning("Failed to decode base64 audio frame on Call {CallId}", _callId);
    //                            _bufferPool.Return(pcmBuffer);
    //                            continue;
    //                        }
    //                        // Enqueue the audio frame (or wait if backpressure)
    //                        var frame = new AudioFrame(pcmBuffer, IsSilent: false);
    //                        await _audioQueue.Writer.WriteAsync(frame, cancelToken);
    //                    }
    //                    else
    //                    {
    //                        _logger.LogDebug("Silent audio frame detected for Call {CallId}, skipping forwarding.", _callId);
    //                        // We could optionally enqueue a marker for silence or just skip.
    //                    }
    //                }
    //            }
    //        } // end while
    //    }
    //    catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
    //    {
    //        // Normal shutdown via cancellation
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error in ReceiveFromAcs for Call {CallId}", _callId);
    //        // Trigger cancellation so the OpenAI processing can stop as well
    //        _cts.Cancel();
    //    }
    //    finally
    //    {
    //        _audioQueue.Writer.TryComplete(); // signal no more input
    //        _bufferPool.Return(receiveBuffer);
    //    }
    //}

    ///// <summary>
    ///// Reads buffered audio frames from ACS and sends them to the OpenAI session. Also handles OpenAI updates (transcripts & audio) and sends audio to ACS.
    ///// </summary>
    //private async Task ProcessOpenAiAsync(CancellationToken cancelToken)
    //{
    //    // Start listening for OpenAI session updates (responses) in background
    //    var openAiUpdatesTask = Task.Run(async () =>
    //    {
    //        try
    //        {
    //            await foreach (var update in _openAiSession.ReceiveUpdatesAsync(cancelToken))
    //            {
    //                // Handle different types of updates from OpenAI
    //                switch (update)
    //                {
    //                    case ConversationTranscriptionUpdate transcriptionUpdate:
    //                        // Partial or final transcription of user speech
    //                        if (transcriptionUpdate is ConversationInputTranscriptionFinishedUpdate inputDone)
    //                        {
    //                            string userText = inputDone.Transcript;
    //                            _logger.LogInformation("🔹 User said (Call {CallId}): {Transcript}", _callId, userText);
    //                            await _transcriptStore.AppendUserTranscriptAsync(_callId, userText);
    //                        }
    //                        else if (transcriptionUpdate is ConversationOutputTranscriptionDeltaUpdate outputDelta)
    //                        {
    //                            // (Optional) Handle incremental transcription of assistant's response (text)
    //                            // Not storing until it's finished for clarity
    //                        }
    //                        else if (transcriptionUpdate is ConversationItemStreamingAudioTranscriptionFinishedUpdate outputDone)
    //                        {
    //                            // Final transcription of the assistant's spoken output
    //                            string assistantText = outputDone.Transcript;
    //                            _logger.LogInformation("🔸 Assistant said (Call {CallId}): {Transcript}", _callId, assistantText);
    //                            await _transcriptStore.AppendAssistantTranscriptAsync(_callId, assistantText);
    //                        }
    //                        break;
    //                    case ConversationAudioDeltaUpdate audioDeltaUpdate:
    //                        // The model produced a chunk of audio (as bytes)
    //                        byte[] audioChunk = audioDeltaUpdate.Delta.ToArray();
    //                        _logger.LogDebug("Received {Bytes} bytes of AI audio for Call {CallId}", audioChunk.Length, _callId);
    //                        // Send this audio chunk into the ACS call via WebSocket
    //                        string json = OutgoingAcsAudio.FromAudioChunk(audioChunk);
    //                        var bytes = Encoding.UTF8.GetBytes(json);
    //                        if (_acsSocket.State == WebSocketState.Open)
    //                        {
    //                            await _acsSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
    //                        }
    //                        break;
    //                    case ConversationResponseFinishedUpdate responseFinished:
    //                        // The assistant's turn (response) finished.
    //                        _logger.LogInformation("Assistant response finished for Call {CallId}.", _callId);
    //                        break;
    //                    case ConversationErrorUpdate errorUpdate:
    //                        _logger.LogError("OpenAI session error for Call {CallId}: {Message}", _callId, errorUpdate.Message);
    //                        throw new ApplicationException($"OpenAI session error: {errorUpdate.Message}");
    //                }
    //            }
    //        }
    //        catch (Exception ex) when (ex is not OperationCanceledException)
    //        {
    //            _logger.LogError(ex, "Error receiving OpenAI updates for Call {CallId}", _callId);
    //            // If the OpenAI session failed unexpectedly, trigger session recovery
    //            await HandleOpenAiSessionFailureAsync();
    //        }
    //    }, cancelToken);

    //    // Now process audio frames from ACS and feed to OpenAI
    //    try
    //    {
    //        // GPT-4o expects a continuous audio stream; we send frames as they arrive
    //        while (await _audioQueue.Reader.WaitToReadAsync(cancelToken))
    //        {
    //            while (_audioQueue.Reader.TryRead(out AudioFrame frame))
    //            {
    //                if (frame.IsSilent)
    //                {
    //                    // We chose to not forward silence frames to reduce traffic
    //                    _bufferPool.Return(frame.PcmData);
    //                    continue;
    //                }
    //                // Write audio to OpenAI session. We can send incrementally.
    //                // The Azure SDK provides SendAudioAsync to stream audio input.
    //                try
    //                {
    //                    await _openAiSession.SendAudioAsync(frame.PcmData, cancelToken);
    //                }
    //                catch (Exception ex)
    //                {
    //                    _logger.LogError(ex, "OpenAI SendAudioAsync failed for Call {CallId}", _callId);
    //                    throw;
    //                }
    //                finally
    //                {
    //                    // Return buffer to pool after sending
    //                    _bufferPool.Return(frame.PcmData);
    //                }
    //            }
    //        }
    //    }
    //    catch (OperationCanceledException) { /* cancelled, likely due to shutdown */ }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error in ProcessOpenAi for Call {CallId}", _callId);
    //        // If OpenAI send failed (session maybe broken), trigger recovery
    //        await HandleOpenAiSessionFailureAsync();
    //    }
    //    finally
    //    {
    //        // Cancel listening for OpenAI updates and wait for it to complete
    //        _cts.Cancel();
    //        try { await openAiUpdatesTask; } catch { /* ignore, handled above */ }
    //    }
    //}

    ///// <summary>
    ///// Handles OpenAI session failure by creating a new session and replaying conversation context.
    ///// </summary>
    //private async Task HandleOpenAiSessionFailureAsync()
    //{
    //    if (_openAiRestarting) return; // prevent re-entry
    //    _openAiRestarting = true;
    //    _logger.LogWarning("Attempting to recover OpenAI session for Call {CallId}...", _callId);
    //    try
    //    {
    //        // Dispose old session
    //        await _openAiSession.DisposeAsync();
    //    }
    //    catch { /* ignore */ }

    //    // Create a new OpenAI conversation session (using a factory or client)
    //    _openAiSession = await OpenAiSessionFactory.CreateNewSessionAsync();  // (pseudo-code; assumes we have access to create new session)
    //    _logger.LogInformation("Created new OpenAI session for Call {CallId}", _callId);

    //    // Hydrate conversation: send saved transcripts to new session as initial context
    //    var history = await _transcriptStore.GetConversationHistoryAsync(_callId);
    //    if (history != null)
    //    {
    //        foreach (var item in history)
    //        {
    //            // Assume item has Role (User/Assistant) and Content (transcript)
    //            ConversationItem convItem;
    //            if (item.Role == "user")
    //                convItem = ConversationItem.FromUserTranscript(item.Content);
    //            else
    //                convItem = ConversationItem.FromAssistantTranscript(item.Content);
    //            await _openAiSession.AddItemAsync(convItem);
    //        }
    //        _logger.LogInformation("Rehydrated OpenAI session with {Count} conversation items for Call {CallId}.", history.Count, _callId);
    //    }

    //    // Resume processing with the new session (spawn a new task to handle its updates)
    //    _openAiRestarting = false;
    //    _ = Task.Run(async () => await ProcessOpenAiAsync(_cts.Token)); // continue processing loop with new session
    //}

    //public async ValueTask DisposeAsync()
    //{
    //    try { if (_acsSocket?.State == WebSocketState.Open) await _acsSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None); }
    //    catch { /* ignore */ }
    //    _cts.Cancel();
    //    _audioQueue.Writer.TryComplete();
    //    if (_openAiSession != null)
    //        await _openAiSession.DisposeAsync();
    //}
}

// Helper for formatting outbound ACS audio JSON
static class OutgoingAcsAudio
{
    public static string FromAudioChunk(byte[] pcmData)
    {
        // Encode PCM bytes to base64 and wrap in ACS AudioData JSON structure for outbound
        string base64 = Convert.ToBase64String(pcmData);
        // Note: ACS expects "kind": "AudioData" with an inner audioData object
        // We include only the base64 data and silent=false. (timestamp/participant not required for outgoing)
        return $"{{\"kind\":\"AudioData\",\"audioData\":{{\"data\":\"{base64}\",\"silent\":false}}}}";
    }
    public static string StopPlayback()
    {
        // JSON message to signal ACS to stop any queued audio playback (using ACS-defined format)
        return $"{{\"kind\":\"AudioData\",\"audioData\":{{\"status\":\"stop\"}}}}";
        // (This format is assumed; ACS docs suggest sending a special message to stop playback)
    }
}