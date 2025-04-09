#pragma warning disable OPENAI002

using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;

namespace Showcase.AI.Voice.Agents.RealtimeVoice;

// This should be a Delegating agent that wraps an AI agent that uses raw audio in conversations.
public class RealtimeConversationAgent : OpenAIRealtimeAgent
{
    public RealtimeConversationAgent(
        RealtimeConversationClient aiClient,
        RealtimeSessionOptions sessionOptions,
        ILoggerFactory loggerFactory,
        string id,
        string name) : base(aiClient, sessionOptions, loggerFactory, id, name)
    {
    }


}

