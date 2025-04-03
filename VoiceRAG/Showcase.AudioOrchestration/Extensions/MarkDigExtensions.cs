using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig;

namespace Showcase.AI.Voice.Extensions;
public static class MarkDigExtensions
{

    public static string? GetTitle(this HeadingBlock current)
    {
        return current.Inline?.FirstChild?.ToString();
    }

    public static Block? FindNextBlock(this MarkdownDocument document, Block current)
    {
        var next = document.FindClosestBlock(current.Line + 1);
        return next;
    }

    public static string[] GetHeaderContent(this MarkdownDocument document, HeadingBlock current)
    {
        var nextBlock = document.FindNextBlock(current);

        if (nextBlock is ParagraphBlock paragraph)
        {
            StringBuilder contentBuilder = new ();

            foreach (var line in paragraph.Lines.Lines)
            {
                contentBuilder.AppendLine(line.ToString());
            }
            return [contentBuilder.ToString()];
        }

        if (nextBlock is ListBlock list)
        {
            List<string> items = new();
            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    StringBuilder contentBuilder = new();
                    foreach (var line in listItem.Lines.Lines)
                    {
                        contentBuilder.AppendLine(line.ToString());
                    }
                    items.Add(contentBuilder.ToString());
                }
            }
            return items.ToArray();
        }
        return [];
    }
}
