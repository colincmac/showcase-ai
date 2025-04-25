using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI.Evaluation.Safety;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Evaluation;
public class ContentSafetyOptions
{
    public const string SectionName = "ContentSafety";
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    public ContentSafetyServiceConfiguration ToServiceConfiguration(TokenCredential credential, HttpClient? httpClient = default, int timeoutInSecondsForRetries = default)
    {
        return new ContentSafetyServiceConfiguration(credential, SubscriptionId, ResourceGroupName, ProjectName, httpClient, timeoutInSecondsForRetries);
    }
}
