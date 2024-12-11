using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.ServiceDefaults.Observability;
public static partial class GenAILogMessages
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "[{MethodName}] Invoking chat: {AgentType}: {AgentId}")]
    public static partial void LogAgentGroupChatInvokingAgent(
        this ILogger logger,
        string methodName,
        Type agentType,
        string agentId);

}
