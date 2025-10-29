#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.SemanticKernel.Steps;
public class ConversationStateStep : KernelProcessStep<ConversationStepDefinition>
{
    public string Id { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string SystemMessage { get; set; } = string.Empty;
    public string[] Instructions { get; set; } = [];
    public string[] Examples { get; set; } = [];

    private ConversationStepDefinition? _state;

    public ConversationStateStep(string stepName, string systemMessage) 
    {
        StepName = stepName;
        SystemMessage = systemMessage;
    }

    public override ValueTask ActivateAsync(KernelProcessStepState<ConversationStepDefinition> state)
    {
        _state = state.State;
        return base.ActivateAsync(state);
    }


    //[KernelFunction]
    //public async Task ActivateNextStep(KernelProcessStepContext context, string content)
    //{
    //    int chunkSize = content.Length / Environment.ProcessorCount;
    //}

}

public sealed record ConversationStepDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Instructions { get; set; } = [];
    public string[] Examples { get; set; } = [];
    public ConversationTransition[] Transitions { get; set; } = [];
}

public sealed record ConversationTransition(string NextStep, string SemanticCondition);