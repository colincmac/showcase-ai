namespace Showcase.AI.Voice.McpServer.Domain;

public class Conversation
{
    public string ConversationId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public string ReferenceGrammarId { get; set; } = string.Empty;
}
