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
}
