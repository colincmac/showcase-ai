#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.SemanticKernel;
public class ProcessEventBroadcaster : IExternalKernelProcessMessageChannel
{
    public Task EmitExternalEventAsync(string externalTopicEvent, KernelProcessProxyMessage eventData)
    {
        throw new NotImplementedException();
    }

    public ValueTask Initialize()
    {
        throw new NotImplementedException();
    }

    public ValueTask Uninitialize()
    {
        throw new NotImplementedException();
    }
}
