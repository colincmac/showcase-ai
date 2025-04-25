# Problem Statement

# Reference Docs
https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/evaluation-metrics-built-in?tabs=warning
https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/evaluation-approach-gen-ai
https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk
https://github.com/microsoft/teams-ai/tree/main/dotnet/packages/Microsoft.TeamsAI/Microsoft.TeamsAI/AI/Moderator
https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk#data-requirements-for-built-in-evaluators
User Input -> System Prompt -> 
https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cuser-prompt%2Cpython-new#content-streaming
Request
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.Sexual"/>
    public ContentFilterSeverityResult Sexual => InternalResults?.Sexual;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.Violence"/>
    public ContentFilterSeverityResult Violence => InternalResults?.Violence;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.Hate"/>
    public ContentFilterSeverityResult Hate => InternalResults?.Hate;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.SelfHarm"/>
    public ContentFilterSeverityResult SelfHarm => InternalResults?.SelfHarm;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.Profanity"/>
    public ContentFilterDetectionResult Profanity => InternalResults?.Profanity;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.CustomBlocklists"/>
    public ContentFilterBlocklistResult CustomBlocklists => InternalResults?.CustomBlocklists;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.Jailbreak"/>
    public ContentFilterDetectionResult Jailbreak => InternalResults?.Jailbreak;
    /// <inheritdoc cref="InternalAzureContentFilterResultForPromptContentFilterResults.IndirectAttack"/>
    public ContentFilterDetectionResult IndirectAttack => InternalResults?.IndirectAttack;

Response


Risk Category	Prompt/Completion	Severity Threshold
Hate and Fairness	Prompts and Completions	Medium
Violence	Prompts and Completions	Medium
Sexual	Prompts and Completions	Medium
Self-Harm	Prompts and Completions	Medium
User prompt injection attack (Jailbreak)	Prompts	N/A
Protected Material – Text	Completions	N/A
Protected Material – Code	Completions	N/A