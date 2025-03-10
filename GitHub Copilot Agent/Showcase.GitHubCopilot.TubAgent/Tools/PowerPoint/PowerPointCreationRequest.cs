namespace Showcase.GitHubCopilot.TubAgent.Tools.PowerPoint;

public class PowerPointCreationRequest : DocCreationRequest<PowerPointContentDetails>
{
    public override DocumentType DocumentType => DocumentType.PowerPoint;

    public PowerPointCreationRequest(PowerPointContentDetails content) : base(content) { }

    public static bool TryParsePowerPointCreationRequest(dynamic content, string title, out DocCreationRequestBase request)
    {
        var contentDict = content as IDictionary<string, object>;
        if (!contentDict.ContainsKey("slides"))
        {
            request = null;
            return false;
        }

        var slides = new List<SlideInfo>();

        foreach (var slide in content.slides as IEnumerable<dynamic>)
        {
            var slideDict = slide as IDictionary<string, object>;
            if (!slideDict.ContainsKey("title") || !slideDict.ContainsKey("keyPoints"))
            {
                continue;
            }

            var slideTitle = slide.title as string;
            var keyPoints = (slide.keyPoints as IEnumerable<dynamic>).Select(kp => kp as string).ToArray();

            slides.Add(new SlideInfo() { Title = slideTitle, KeyPoints = keyPoints });
        }

        request = new PowerPointCreationRequest(new PowerPointContentDetails() { Slides = slides, Title = title });
        return true;
    }

    public override byte[] GenerateDocument()
    {
        return PowerPointCreation.GenerateDocument(this);
    }
}
