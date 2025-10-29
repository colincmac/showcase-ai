using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

/// <summary>
/// Interface for updating an external monitoring system with call quality and other events.
/// </summary>
public interface IMonitoringService
{
    Task UpdateCallQualityAsync(string callId, double qualityMetric, CancellationToken cancellationToken);
    Task LogEventAsync(string callId, string message, CancellationToken cancellationToken);
}

