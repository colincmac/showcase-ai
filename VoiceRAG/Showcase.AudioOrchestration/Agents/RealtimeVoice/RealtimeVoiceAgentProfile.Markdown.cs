using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Agents.RealtimeVoice;
public partial class RealtimeVoiceAgentProfile
{
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        var type = typeof(RealtimeVoiceAgentProfile);

        foreach (var prop in type.GetProperties())
        {
            var displayName = prop.GetCustomAttribute<MarkdownDisplayNameAttribute>()?.DisplayName ?? prop.Name;

            var value = prop.GetValue(this);
            if (value == null) continue;

            if (value is PersonalityAndToneSection personality)
            {
                sb.AppendLine($"## {displayName}");
                foreach (var subProp in typeof(PersonalityAndToneSection).GetProperties())
                {
                    var subAttr = subProp.GetCustomAttribute<MarkdownDisplayNameAttribute>();
                    if (subAttr == null) continue;

                    var subValue = subProp.GetValue(personality)?.ToString();
                    if (!string.IsNullOrEmpty(subValue))
                    {
                        sb.AppendLine($"### {subAttr.DisplayName}");
                        sb.AppendLine(subValue);
                        sb.AppendLine();
                    }
                }
            }
            else if (value is ContextSection context)
            {
                sb.AppendLine($"## {displayName}");
                foreach (var item in context.ContextList)
                {
                    sb.AppendLine($"- {item}");
                }
                sb.AppendLine();
            }
            else if (value is PronunciationSection pronunciation)
            {
                sb.AppendLine($"## {displayName}");
                foreach (var item in pronunciation.PronunciationList)
                {
                    sb.AppendLine($"- {item}");
                }
                sb.AppendLine();
            }
            else
            {
                var stringValue = value.ToString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    sb.AppendLine($"## {displayName}");
                    sb.AppendLine(stringValue);
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static RealtimeVoiceAgentProfile FromMarkdown(string markdown)
    {
        var profile = new RealtimeVoiceAgentProfile();
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        HeadingBlock? currentSection = null;
        var currentContent = new StringBuilder();
        var contextItems = new List<string>();
        var pronunciationItems = new List<string>();

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                ProcessCurrentSection(profile, currentSection, currentContent.ToString(), contextItems, pronunciationItems);
                currentSection = heading;
                currentContent.Clear();
            }
            else
            {
                if (block is ListBlock listBlock && currentSection != null)
                {
                    foreach (var item in listBlock.Where(x => x is ListItemBlock).Cast<ListItemBlock>())
                    {
                        var text = item.ToString()?.TrimStart('-', ' ') ?? string.Empty;
                        if (GetHeadingText(currentSection) == ContextSection.Title)
                        {
                            contextItems.Add(text);
                        }
                        else if (GetHeadingText(currentSection) == PronunciationSection.Title)
                        {
                            pronunciationItems.Add(text);
                        }
                    }
                }
                else
                {
                    currentContent.AppendLine(block.ToString());
                }
            }
        }

        ProcessCurrentSection(profile, currentSection, currentContent.ToString(), contextItems, pronunciationItems);
        return profile;
    }

    private static void ProcessCurrentSection(
        RealtimeVoiceAgentProfile profile,
        HeadingBlock? section,
        string content,
        List<string> contextItems,
        List<string> pronunciationItems)
    {
        if (section == null) return;

        var sectionTitle = GetHeadingText(section);
        var trimmedContent = content.Trim();

        switch (sectionTitle)
        {
            case "Instructions":
                profile.Instructions = trimmedContent;
                break;
            case "Guidelines":
                profile.Guidelines = trimmedContent;
                break;
            case "Context":
                profile.Context.ContextList = contextItems.ToArray();
                break;
            case "Pronunciation":
                profile.Pronunciation.PronunciationList = pronunciationItems.ToArray();
                break;
            case "Personality and Tone":
                ProcessPersonalitySection(profile.Personality, content);
                break;
        }
    }

    private static void ProcessPersonalitySection(PersonalityAndToneSection personality, string content)
    {
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(content, pipeline);

        HeadingBlock? currentSubSection = null;
        var currentContent = new StringBuilder();

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                ProcessPersonalitySubSection(personality, currentSubSection, currentContent.ToString());
                currentSubSection = heading;
                currentContent.Clear();
            }
            else
            {
                currentContent.AppendLine(block.ToString());
            }
        }

        ProcessPersonalitySubSection(personality, currentSubSection, currentContent.ToString());
    }

    private static void ProcessPersonalitySubSection(PersonalityAndToneSection personality, HeadingBlock? section, string content)
    {
        if (section == null) return;

        var sectionTitle = GetHeadingText(section);
        var trimmedContent = content.Trim();

        var property = typeof(PersonalityAndToneSection)
            .GetProperties()
            .FirstOrDefault(p =>
                p.GetCustomAttribute<MarkdownDisplayNameAttribute>()?.DisplayName == sectionTitle);

        if (property != null)
        {
            property.SetValue(personality, trimmedContent);
        }
    }

    private static string GetHeadingText(HeadingBlock heading)
    {
        return heading.Inline?.FirstChild is LiteralInline literal ? literal.Content.ToString() : string.Empty;
    }
}
