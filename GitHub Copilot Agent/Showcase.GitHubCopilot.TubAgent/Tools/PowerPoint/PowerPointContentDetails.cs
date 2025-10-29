namespace Showcase.GitHubCopilot.TubAgent.Tools.PowerPoint;

public class PowerPointContentDetails : ContentDetails
{
    public IEnumerable<SlideInfo> Slides { get; set; }

    public string Title { get; set; }
}
