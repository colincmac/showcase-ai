namespace Showcase.GitHubCopilot.TubAgent.Tools.PowerPoint;

public abstract class DocCreationRequest<T> : DocCreationRequestBase
    where T : ContentDetails
{
    public abstract DocumentType DocumentType { get; }

    public T Content { get; }

    protected DocCreationRequest(T content)
    {
        Content = content;
    }
}
