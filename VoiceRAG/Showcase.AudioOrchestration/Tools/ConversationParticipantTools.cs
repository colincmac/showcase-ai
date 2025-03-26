using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Tools;

public class ConversationParticipantTools : IAIToolHandler
{
    private readonly ConversationParticipant _invokingParticipant;
    public ConversationParticipantTools(ConversationParticipant invokingParticipant, IList<ConversationParticipant> knownParticipants)
    {
        _invokingParticipant = invokingParticipant;
    }

    public IEnumerable<AIFunction> GetAITools()
    {
        var tools = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<AIToolAttribute>() is not null)
            .Select(tool =>
            {
                var attribute = tool.GetCustomAttribute<AIToolAttribute>();

                return AIFunctionFactory.Create(tool, this, options: new()
                {
                    Name = attribute?.Name,
                    Description = attribute?.Description
                });
            });
        foreach (var tool in tools)
        {
            yield return tool;
        }
    }

    public enum ConversationTermination
    {
        Continue,
        EndCallGracefully,
        EscalateToLivePerson,
        WaitForResponse,
    }

    [AITool(name: "transferAgent", description: "Triggers a transfer of the user to a more specialized agent. \r\n  Calls escalate to a more specialized LLM agent or to a human agent, with additional context.")]
    public static string TransferToAgent(
        [Description("The reasoning why this transfer is needed.")] string reasonForTransfer,
        [Description("Relevant context from the conversation that will help the recipient perform the correct action.")] string conversationContext,
        [Description("The name of the agent to transfer to.")] string agentName
        ) => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
}
