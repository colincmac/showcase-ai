using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.IO;
using System.Linq;
using System.Reflection;
using Path = System.IO.Path;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Text = DocumentFormat.OpenXml.Drawing.Text;
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using A16 = DocumentFormat.OpenXml.Office2013.Drawing;
using System.Text.Json;
using Microsoft.AspNetCore.Routing.Template;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

namespace Showcase.GitHubCopilot.TubAgent.Tools.PowerPoint;

public static class PowerPointCreation
{
    private const string TemplatePath = "template.pptx";
    private const string OutputPath = "output.pptx";

    private static SlideMasterPart GetMasterPart(PresentationPart presentationPart)
    {
        // Assuming there's only one SlideMasterPart
        return presentationPart.SlideMasterParts.First();
    }
    static void CreateTitleSlide(PresentationPart presentationPart, SlideMasterPart masterPart, IEnumerable<SlideLayoutPart> slideLayouts, DateTime currentDate)
    {
        // Find the "title" layout
        SlideLayoutPart titleLayout = slideLayouts.FirstOrDefault(sl => GetLayoutName(sl) == "title");
        if (titleLayout == null)
            throw new Exception("Title layout not found.");

        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));

        slidePart.AddPart(titleLayout);

        // Clone the slide layout content
        slidePart.Slide.CommonSlideData.ShapeTree = (ShapeTree)titleLayout.SlideLayout.CommonSlideData.ShapeTree.CloneNode(true);

        // Replace placeholders
        foreach (var shape in slidePart.Slide.Descendants<Shape>())
        {
            var placeholder = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
            if (placeholder != null && placeholder.Type == PlaceholderValues.Title)
            {
                shape.TextBody = new TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text($"{currentDate:MMMM yyyy} Updates")))
                );
            }
        }

        // Append slide to the presentation
        uint maxSlideId = presentationPart.Presentation.SlideIdList.Elements<SlideId>().Max(s => s.Id.Value);
        SlideId newSlideId = new SlideId()
        {
            Id = maxSlideId + 1,
            RelationshipId = presentationPart.GetIdOfPart(slidePart)
        };
        presentationPart.Presentation.SlideIdList.Append(newSlideId);
    }

    static List<GitHubUpdate> FetchGitHubUpdates()
    {
        // Placeholder for your GitHub updates fetching logic
        // Replace this with actual data fetching
        return new List<GitHubUpdate>
        {
            new GitHubUpdate
            {
                Title = "Feature Added: OpenAI Integration",
                MainText = "We have integrated OpenAI's GPT models to enhance our application's capabilities.",
                ImagePath = "images/feature1.png",
                SourceUrl = "https://github.com/yourrepo/feature1",
                Date = new DateTime(2024, 12, 1)
            },
            // Add more updates as needed
        };
    }

    static void CreateGitHubUpdateSlide(PresentationPart presentationPart, SlideMasterPart masterPart, IEnumerable<SlideLayoutPart> slideLayouts, GitHubUpdate update)
    {
        var updates = FetchGitHubUpdates();

        // Find the "github_blog" layout
        SlideLayoutPart blogLayout = slideLayouts.First(sl => GetLayoutName(sl) == GitHubTubDeckConstants.GitHubBlogLayout);

        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));

        slidePart.AddPart(blogLayout);

        // Clone the slide layout content
        slidePart.Slide.CommonSlideData.ShapeTree = (ShapeTree)blogLayout.SlideLayout.CommonSlideData.ShapeTree.CloneNode(true);

        // Replace placeholders
        foreach (var shape in slidePart.Slide.Descendants<Shape>())
        {
            var placeholder = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>();
            if(placeholder == null) continue;

            switch (placeholder.XName.LocalName)
            {
                case GitHubTubDeckConstants.TitlePlaceHolder:
                    shape.TextBody = new TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(update.Title)))
                    );
                    break;
                case GitHubTubDeckConstants.MainTextPlaceHolder:
                    // Assuming 'main_text' corresponds to Body placeholder
                    shape.TextBody = new TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(update.MainText)))
                    );
                    break;
                    // Add cases for other placeholders like image, source_url, date
            }
            
        }

        // Handle Image
        //if (!string.IsNullOrEmpty(update.ImagePath) && File.Exists(update.ImagePath))
        //{
        //    ImagePart imagePart = slidePart.AddImagePart(ImagePartType.Png);
        //    using (FileStream stream = new FileStream(update.ImagePath, FileMode.Open))
        //    {
        //        imagePart.FeedData(stream);
        //    }

        //    // Add the image to the slide
        //    var picture = new Picture(
        //        new NonVisualPictureProperties(
        //            new NonVisualDrawingProperties() { Id = (UInt32)(slidePart.Slide.Descendants<Picture>().Count() + 1), Name = Path.GetFileName(update.ImagePath) },
        //            new NonVisualPictureDrawingProperties(new A.PictureLocks() { NoChangeAspect = true })
        //        ),
        //        new BlipFill(
        //            new A.Blip() { Embed = slidePart.GetIdOfPart(imagePart) },
        //            new A.Stretch(new A.FillRectangle())
        //        ),
        //        new ShapeProperties(
        //            new A.Transform2D(
        //                new A.Offset() { X = 0, Y = 0 },
        //                new A.Extents() { Cx = 990000L, Cy = 792000L }
        //            ),
        //            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
        //        )
        //    );

        //    slidePart.Slide.CommonSlideData.ShapeTree.AppendChild(picture);
        //}

        // Append slide to the presentation
        uint maxSlideId = presentationPart.Presentation.SlideIdList.Elements<SlideId>().Max(s => s.Id.Value);
        SlideId newSlideId = new SlideId()
        {
            Id = maxSlideId + 1,
            RelationshipId = presentationPart.GetIdOfPart(slidePart)
        };
        presentationPart.Presentation.SlideIdList.Append(newSlideId);
    }

    static string GetLayoutName(SlideLayoutPart slideLayoutPart)
    {
        var name = slideLayoutPart.SlideLayout.CommonSlideData.Name;
        return name;
    }
    internal static byte[] GenerateDocument(PowerPointCreationRequest powerPointSlidedeckCreationRequest)
    {
        File.Copy(TemplatePath, OutputPath, true);
        using (PresentationDocument presentation = PresentationDocument.Open(OutputPath, true))
        {
            if (presentation == null)
            {
                throw new FileNotFoundException("The presentation file could not be found.");
            }

            var presentationPart = presentation.PresentationPart;
            if (presentationPart == null)
            {
                presentationPart = presentation.AddPresentationPart();
                presentationPart.Presentation = new Presentation();
            }


            if (presentationPart.Presentation.SlideIdList == null)
            {
                presentationPart.Presentation.SlideIdList = new SlideIdList();
            }
            SlideMasterPart masterPart = GetMasterPart(presentationPart);

        }


        var dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var blankSlidedeckPath = Path.Combine(dllPath, "PowerPoint", "BlankPresentation.pptx");

        // a stream to hold the document
        using var docStream = new MemoryStream();
        // clone the blank doc from the template file to the stream
        var blankSlidedeck = PresentationDocument.Open(blankSlidedeckPath, false, new OpenSettings() { AutoSave = false });

        using (var powerPointSlidedeck =
            blankSlidedeck.Clone(docStream, true, new OpenSettings() { AutoSave = true }) as PresentationDocument)
        {
            blankSlidedeck.Dispose();

            // if file name was provided, use it as slidedeck title
            // and update the title slide (which is already part of the blank presentation)
            if (!string.IsNullOrEmpty(powerPointSlidedeckCreationRequest.Content.Title))
            {
                var titleSlide = powerPointSlidedeck.PresentationPart.SlideParts.First().Slide;
                UpdateTitleOnSlide(titleSlide, powerPointSlidedeckCreationRequest.Content.Title);
            }

            var slideMasterPart = powerPointSlidedeck.PresentationPart.SlideMasterParts.FirstOrDefault();
            var simpleSlideLayoutPart = slideMasterPart.SlideLayoutParts.SingleOrDefault(sl => sl.SlideLayout.CommonSlideData.Name.Value == GitHubTubDeckConstants.GitHubBlogLayout);

            foreach (var slide in powerPointSlidedeckCreationRequest.Content.Slides)
            {
                InsertSlide(powerPointSlidedeck, simpleSlideLayoutPart, slide.Title, slide.KeyPoints);
            }

            powerPointSlidedeck.PackageProperties.LastModifiedBy = string.Empty;
            powerPointSlidedeck.PackageProperties.Creator = string.Empty;
        }

        return docStream.ToArray();
    }

    private static void InsertSlide(PresentationDocument presentation, SlideLayoutPart layoutPart, string title, string[] keyPoints)
    {
        // create the slide and add it to the presentation
        var slide = new Slide();
        var slidePart = presentation.PresentationPart.AddNewPart<SlidePart>();
        slide.Save(slidePart);

        // add the layout as reference to the newly created slide
        var slideMasterPart = layoutPart.SlideMasterPart;
        var slideLayoutPartId = slideMasterPart.GetIdOfPart(layoutPart);
        slidePart.AddPart(layoutPart, slideLayoutPartId);

        // copy the layout from slide layout to the slide itself
        using (Stream stream = layoutPart.GetStream())
        {
            slidePart.SlideLayoutPart.FeedData(stream);
        }
        slidePart.Slide.CommonSlideData = layoutPart.SlideLayout.CommonSlideData.Clone() as CommonSlideData;

        // get the slide id for the new slide
        SlideIdList slideIdList = presentation.PresentationPart.Presentation.SlideIdList;
        uint newId = slideIdList.ChildElements.Cast<SlideId>().Max(x => x.Id.Value) + 1;

        // update presentation with the new slide Id
        var newSlideId = slideIdList.AppendChild(new SlideId());
        newSlideId.Id = newId;
        newSlideId.RelationshipId = presentation.PresentationPart.GetIdOfPart(slidePart);

        // update the new slide content with the right title and text
        UpdateTitleOnSlide(slidePart.Slide, title);

        // Handle OpenXML corruption of slides with no keyPoints: we create an empty keyPoint instead
        if (keyPoints != null && keyPoints.Length > 0)
        {
            UpdateTextOnSlide(slidePart.Slide, keyPoints);
        }

        // remove footer shapes from the slide that were copied over with the layout
        RemoveFooterShapes(slidePart.Slide);
    }

    private static void UpdateTitleOnSlide(Slide slide, string title)
    {
        var titleShape = GetTitleShape(slide);
        if (titleShape != null)
        {
            var titleParagraph = titleShape.Descendants<Paragraph>().First();
            titleParagraph.RemoveAllChildren();
            var run = titleParagraph.AppendChild(new Run());
            run.AppendChild(new Text(title));
        }
    }

    private static void UpdateTextOnSlide(Slide slide, string[] keyPoints)
    {
        var contentShape = GetContentShape(slide);
        if (contentShape != null)
        {
            var paragraphs = contentShape.Descendants<Paragraph>().ToList();
            paragraphs.ForEach(paragraph => paragraph.Remove());

            foreach (var keyPoint in keyPoints)
            {
                var newParagraph = contentShape.TextBody.AppendChild(new Paragraph());
                var run = newParagraph.AppendChild(new Run());
                run.AppendChild(new Text(keyPoint));
            }
        }
    }

    private static Shape GetContentShape(Slide slide)
    {
        return slide?.Descendants<Shape>().FirstOrDefault(shape =>
        {
            var placeholderShape = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
            return placeholderShape != null && (placeholderShape.Type == null || placeholderShape.Type == PlaceholderValues.Body);
        });
    }

    private static Shape GetTitleShape(Slide slide)
    {
        return slide.Descendants<Shape>().First(shape =>
        {
            //var p = shape.Where(p => p.XName)
            var placeholderShape = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>();
            return placeholderShape != null && placeholderShape.Type != null && (placeholderShape.Type == PlaceholderValues.Title || placeholderShape.Type == PlaceholderValues.CenteredTitle);
        });
    }

    private static void RemoveFooterShapes(Slide slide)
    {
        var shapesToRemove = slide.Descendants<Shape>()
            .Where(shape =>
            {
                var placeholderShape = shape.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.GetFirstChild<PlaceholderShape>();
                return placeholderShape != null && placeholderShape.Type != null && placeholderShape.Type != PlaceholderValues.Title && placeholderShape.Type != PlaceholderValues.Body;
            })
            .ToList();

        shapesToRemove.ForEach(shape => shape.Remove());
    }
}
