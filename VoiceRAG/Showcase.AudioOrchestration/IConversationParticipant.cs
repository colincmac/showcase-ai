using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IConversationParticipant
{
    Task SendAsync(BinaryData data, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task ReceiveAsync(IEnumerable<IConversationParticipant> participants);
}
