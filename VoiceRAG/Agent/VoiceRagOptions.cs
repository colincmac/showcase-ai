namespace Showcase.VoiceRagAgent;

public class VoiceRagOptions
{
    public const string SectionName = "VoiceRag";
    public string AcsConnectionString { get; set; } = string.Empty;
    public string AzureOpenAIDeploymentModelName { get; set; } = string.Empty;
    public string AzureOpenAISystemPrompt { get; set; } = string.Empty;
}
