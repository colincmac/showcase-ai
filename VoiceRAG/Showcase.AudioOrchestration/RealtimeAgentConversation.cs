//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using OpenAI.Chat;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics.Tracing;
//using System.Linq;
//using System.Net.WebSockets;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace Showcase.AudioOrchestration;

//public abstract class RealtimeAgentConversation
//{
//    internal ILogger _logger;
//    internal List<ConversationParticipant> _agents = new();

//    internal Channel<RealtimeEvent> _outputChannel;
//    internal Channel<RealtimeEvent> _inputChannel;

//    public RealtimeAgentConversation(ILogger? logger = null)
//    {
//        _logger = logger ?? NullLogger.Instance;

//    }

//    public virtual void AddAgent(ConversationParticipant agent)
//    {
//        _agents.Add(agent);
//    }

//    public virtual void RemoveAgent(ConversationParticipant agent)
//    {
//        _agents.Remove(agent);
//    }

//    public virtual async ValueTask StartConversationAsync(
//        WebSocket webSocket,
//        CancellationToken cancellationToken = default)
//    {
//        await _inputChannel.Writer.WriteAsync(incomingEvent, cancellationToken);
//    }

//    public virtual IAsyncEnumerable<RealtimeEvent> ReceiveAsync(
//        Stream stream,
//        CancellationToken cancellationToken = default)
//    {
//        // This method should be overridden in derived classes to handle incoming events.
//        throw new NotImplementedException("ReceiveAsync must be implemented in derived classes.");
//    }


//}
