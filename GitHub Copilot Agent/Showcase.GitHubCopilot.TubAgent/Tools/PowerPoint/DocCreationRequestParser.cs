namespace Showcase.GitHubCopilot.TubAgent.Tools.PowerPoint;

public static class DocCreationRequestParser
{
    public static bool TryParseDocCreationRequest(dynamic input, out DocCreationRequestBase request)
    {
        var inputDict = input as IDictionary<string, object>;
        if (inputDict.ContainsKey("docType") && inputDict.ContainsKey("content"))
        {
            var title = inputDict.ContainsKey("title") ? input.title as string : null;

            var docType = input.docType as string;
            switch (docType)
            {
                // case "document":
                //     return WordDocCreationRequest.TryParseWordDocCreationRequest(input.content, title, out request);
                case "slidedeck":
                    return PowerPointCreationRequest.TryParsePowerPointCreationRequest(input.content, title, out request);
                    // case "spreadsheet":
                    //     return ExcelWorkbookCreationRequest.TryParseExcelWorkbookCreationRequest(input.content, title, out request);
            }
        }

        request = null;
        return false;
    }
}
