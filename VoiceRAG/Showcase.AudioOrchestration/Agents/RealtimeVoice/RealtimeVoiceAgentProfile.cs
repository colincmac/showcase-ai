using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Showcase.AI.Voice.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Agents.RealtimeVoice;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
sealed class MarkdownDisplayNameAttribute : Attribute
{
    public string DisplayName { get; }

    public MarkdownDisplayNameAttribute(string displayName) => DisplayName = displayName;
}

public partial class RealtimeVoiceAgentProfile
{

    [MarkdownDisplayName("Instructions")]
    public string Instructions { get; set; } = string.Empty;

    [MarkdownDisplayName("Guidelines")]
    public string Guidelines { get; set; } = string.Empty;

    [MarkdownDisplayName(PersonalityAndToneSection.Title)]
    public PersonalityAndToneSection Personality { get; set; } = new PersonalityAndToneSection();

    [MarkdownDisplayName(ContextSection.Title)]
    public ContextSection Context { get; set; } = new ContextSection();

    [MarkdownDisplayName(PronunciationSection.Title)]
    public PronunciationSection Pronunciation { get; set; } = new PronunciationSection();

    public record ContextSection
    {
        public const string Title = "Context";
        public string[] ContextList { get; set; } = [];
    }

    public record PronunciationSection
    {
        public const string Title = "Pronunciation";
        public string[] PronunciationList { get; set; } = [];
    }

    public record PersonalityAndToneSection
    {
        public const string Title = "Personality and Tone";

        [MarkdownDisplayName("Identity")]
        public string Identity { get; set; } = string.Empty;

        [MarkdownDisplayName("Task")]
        public string Task { get; set; } = string.Empty;

        [MarkdownDisplayName("Demeanor")]
        public string Demeanor { get; set; } = string.Empty;

        [MarkdownDisplayName("Tone")]
        public string Tone { get; set; } = string.Empty;

        [MarkdownDisplayName("Level of Enthusiasm")]
        public string EnthusiasmLevel { get; set; } = string.Empty;

        [MarkdownDisplayName("Level of Formality")]
        public string FormalityLevel { get; set; } = string.Empty;

        [MarkdownDisplayName("Level of Emotion")]
        public string EmotionLevel { get; set; } = string.Empty;

        [MarkdownDisplayName("Filler Words")]
        public string FillerWords { get; set; } = string.Empty;

        [MarkdownDisplayName("Pacing")]
        public string Pacing { get; set; } = string.Empty;

        public string ToMarkdown()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    DisplayName = p.GetCustomAttribute<MarkdownDisplayNameAttribute>()?.DisplayName ?? p.Name,
                    Value = p.GetValue(this)?.ToString() ?? string.Empty
                });
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                if(string.IsNullOrEmpty(prop.Value)) continue; // Skip empty values
                sb.AppendLine($"## {prop.DisplayName}");
                sb.AppendLine(prop.Value);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public PersonalityAndToneSection FromMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var document = Markdown.Parse(markdown, pipeline);
            var headings = document.Descendants<HeadingBlock>().ToList();
            foreach (var heading in headings)
            {
                if(heading.Level != 2) continue; // Only process level 2 headings
                var displayName = heading.GetTitle();

                var content = heading.Get;
                if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(content)) continue;
                var property = GetType().GetProperty(displayName);
                if (property != null)
                {
                    property.SetValue(this, content);
                }
            }
            return this;
        }
    }

}