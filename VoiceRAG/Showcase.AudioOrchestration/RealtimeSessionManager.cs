#pragma warning disable OPENAI002

using OpenAI;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class RealtimeSessionManager
{
    private readonly ConcurrentDictionary<string, RealtimeConversationSession> _sessions = new();

    public RealtimeConversationSession StartSession(string sessionId, WebSocket socket)
    {
        // Set up agents for the new session
        var agents = new List<IAgent> {

            // Add more agents as needed
        };
        var session = new RealTimeConversationSession(socket, new AzureTranscriber(), new AzureSynthesizer(), agents);
        if (!_sessions.TryAdd(sessionId, session))
            throw new InvalidOperationException("Session already exists.");

        // Run session asynchronously (fire-and-forget or track the task)
        _ = Task.Run(() => session.StartAsync(CancellationToken.None));
        return session;
    }

    public async Task StopSessionAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            // Ideally signal cancellation to stop gracefully
            // For example, cancel the token passed to StartAsync or close the WebSocket
            await session.StopAsync();  // assume we have a method to stop loops
        }
    }
}
