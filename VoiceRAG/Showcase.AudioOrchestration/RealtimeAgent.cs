using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public abstract class RealtimeAgent
{
    private ILogger? _logger;

    /// <summary>
    /// Gets the description of the agent (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the name of the agent (optional).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// A <see cref="ILoggerFactory"/> for this <see cref="Agent"/>.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; init; } = NullLoggerFactory.Instance;

    protected internal abstract Task<Channel<BinaryData>> CreateChannelAsync(CancellationToken cancellationToken);

    protected internal abstract Task<Channel<BinaryData>> RestoreChannelAsync(string channelState, CancellationToken cancellationToken);


}
