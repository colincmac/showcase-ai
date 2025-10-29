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
sealed class MarkdownDisplayNameAttribute(string DisplayName) : Attribute
{
    public string DisplayName { get; } = DisplayName;
}

public partial class RealtimeVoiceAgentProfile
{

    [MarkdownDisplayName("Instructions")]
    public string Instructions { get; set; } = string.Empty;

    [MarkdownDisplayName("Guidelines")]
    public string Guidelines { get; set; } = string.Empty;

    [MarkdownDisplayName("Personality and Tone")]
    public PersonalityAndToneSection Personality { get; set; } = new PersonalityAndToneSection();

    [MarkdownDisplayName("Context")]
    public string Context { get; set; } = string.Empty;

    [MarkdownDisplayName("Pronunciation")]
    public string Pronunciation { get; set; } = string.Empty;

    public record PersonalityAndToneSection
    {
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
                    Value = p.GetValue(this)
                });
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                if(prop.Value is not string textValue || string.IsNullOrEmpty(textValue)) continue; // Skip empty values
                sb.AppendLine($"## {prop.DisplayName}");
                sb.AppendLine(textValue);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public string ToMarkdown()
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new
            {
                DisplayName = p.GetCustomAttribute<MarkdownDisplayNameAttribute>()?.DisplayName ?? p.Name,
                Value = p.GetValue(this)
            });

        var sb = new StringBuilder();
        foreach (var prop in properties)
        {
            if (prop.Value is string textValue && !string.IsNullOrEmpty(textValue))
            {
                sb.AppendLine($"## {prop.DisplayName}");
                sb.AppendLine(textValue);
                sb.AppendLine();
            }
            else if (prop.Value is PersonalityAndToneSection personalitySection)
            {
                sb.AppendLine($"## {prop.DisplayName}");
                sb.AppendLine(personalitySection.ToMarkdown());
            }

        }
        return sb.ToString();
    }

}