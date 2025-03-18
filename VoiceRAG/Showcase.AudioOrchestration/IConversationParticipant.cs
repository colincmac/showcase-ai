using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IConversationParticipant
{
    Task SendDataAsync(BinaryData data, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    void BroadcastTo(IEnumerable<IConversationParticipant> participants);
}
