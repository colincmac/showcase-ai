#pragma warning disable OPENAI002

using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.AI.Voice.ConversationParticipants;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Voice.Demo;
public class TestAIAgent : OpenAIVoiceParticipant
{
    public TestAIAgent(RealtimeConversationClient aiClient, RealtimeSessionOptions sessionOptions, ILoggerFactory loggerFactory, string id, string name) : base(aiClient, sessionOptions, loggerFactory, id, name)
    {
    }
}
