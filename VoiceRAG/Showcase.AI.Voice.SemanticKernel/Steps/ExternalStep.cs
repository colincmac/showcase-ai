#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.SemanticKernel.Steps;
public class ExternalStep(string externalEventName) : KernelProcessStep
{
    private readonly string _externalEventName = externalEventName;

    [KernelFunction]
    public async Task EmitExternalEventAsync(KernelProcessStepContext context, object data)
    {
        await context.EmitEventAsync(new() { Id = _externalEventName, Data = data, Visibility = KernelProcessEventVisibility.Public });
    }
}