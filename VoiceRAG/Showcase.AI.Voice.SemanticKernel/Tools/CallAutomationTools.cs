using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.SemanticKernel.Tools;
public class CallAutomationTools
{
    private readonly CallAutomationClient _callAutomationClient;
    private readonly ILogger _logger;
    public CallAutomationTools(CallAutomationClient callAutomationClient, ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _callAutomationClient = callAutomationClient ?? throw new ArgumentNullException(nameof(callAutomationClient));
    }
    [KernelFunction(name:"GetAllParticipants")]
    [Description("Get all participants in a call")]
    public async Task<List<string>> GetAllParticipantsAsync(string callConnectionId)
    {
        if (string.IsNullOrEmpty(callConnectionId))
        {
            throw new ArgumentNullException(nameof(callConnectionId));
        }
        var participants = await _callAutomationClient.GetCallConnection(callConnectionId).GetParticipantsAsync();

        var participantIds = participants.Value.Select(p =>
            p.Identifier.RawId).ToList();

        return participantIds;
    }

    //[KernelFunction("TransferCall")]
    //[Description("Transfer a call to a live agent")]
    //public async Task<List<string>> TransferCallAsync(string callConnectionId)
    //{
    //    return
    //}
}
