using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IAuditor
{
    Task ObserveInboundAudioAsync(AudioFrame frame, CancellationToken cancellationToken);
    Task ObserveOutboundAudioAsync(AudioFrame frame, CancellationToken cancellationToken);

    /// <summary>
    /// Optionally generate a command based on analysis.
    /// The provided callback sends the command to the primary agent.
    /// </summary>
    Task GenerateCommandAsync(Func<AiAgentCommand, Task> sendCommand, CancellationToken cancellationToken);
}
