using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class RealtimeConversationOptions
{
    public const string ConfigSectionName = "RealtimeConversationOptions";

    public int ExpectedFrameSize; // e.g., 960 bytes for 24kHz 20ms frame
    public int ChannelCapacity = 5000; // Default channel capacity
}
