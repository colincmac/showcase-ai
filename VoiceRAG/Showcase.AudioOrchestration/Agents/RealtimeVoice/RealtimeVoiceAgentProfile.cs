using Markdig;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Agents.RealtimeVoice
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class MarkdownDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public MarkdownDisplayNameAttribute(string displayName) => DisplayName = displayName;
    }

    public class RealtimeVoiceAgentProfile
    {
        public static RealtimeVoiceAgentProfile TryFromMarkdown(string markdownText)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAutoIdentifiers()
                .UseAdvancedExtensions()
                .Build();

            var document = Markdown.Parse(markdownText, pipeline);
            var profile = new RealtimeVoiceAgentProfile();
            var properties = typeof(RealtimeVoiceAgentProfile)
                .GetProperties(BindingFlags.Public);

            foreach (var heading in document.Descendants<HeadingBlock>())
            {
                heading.
                var headingText = heading.Inline?.FirstChild?.ToString();
                switch (headingText)
                {
                    case PersonalityAndToneSection.Title:
                        profile.Personality = ParsePersonality(document, heading);
                        break;
                    case "Instructions":
                        profile.Instructions = ParseSectionContent(document, heading);
                        break;
                    case "Important Guidelines":
                        profile.Guidelines = ParseSectionContent(document, heading);
                        break;
                }
                
            }

            return profile;
        }

        private static string ParseSectionContent(MarkdownDocument document, HeadingBlock heading)
        {
            var content = new StringBuilder();
            var startLine = heading.Line + 1;
            foreach (var node in document.Descendants().SkipWhile(n => n.Line < startLine))
            {
                if (node is HeadingBlock) break;
                content.AppendLine(node.ToString());
            }
            return content.ToString().Trim();
        }

        private static PersonalityAndToneSection ParsePersonality(MarkdownDocument document, HeadingBlock heading)
        {
            var personality = new PersonalityAndToneSection();
            var startLine = heading.Line + 1;
            foreach (var node in document.Descendants().SkipWhile(n => n.Line < startLine))
            {
                if (node is HeadingBlock) break;
                if (node is ParagraphBlock paragraph)
                {
                    var text = paragraph?.Inline?.FirstChild?.ToString();
                    var properties = typeof(PersonalityAndToneSection).GetProperties();
                    foreach (var property in properties)
                    {
                        var attribute = property.GetCustomAttribute<MarkdownDisplayNameAttribute>();
                        if (attribute != null && !string.IsNullOrEmpty(text) && text.StartsWith($"## {attribute.DisplayName}"))
                        {
                            property.SetValue(personality, text.Substring(attribute.DisplayName.Length + 3).Trim());
                            break;
                        }
                    }
                }
            }
            return personality;
        }

        [MarkdownDisplayName("Instructions")]
        public string Instructions { get; set; } = string.Empty;

        [MarkdownDisplayName("Guidelines")]
        public string Guidelines { get; set; } = string.Empty;

        [MarkdownDisplayName(PersonalityAndToneSection.Title)]
        public PersonalityAndToneSection Personality { get; set; } = new PersonalityAndToneSection();

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
        }

        public record Metadata
        {
            public string AgentId { get; set; } = string.Empty;
            public string AgentName { get; set; } = string.Empty;
            public string AgentDescription { get; set; } = string.Empty;
            public string AgentAvatarUrl { get; set; } = string.Empty;
            public string AgentLanguage { get; set; } = string.Empty;
            public string AgentVoiceId { get; set; } = string.Empty;
        }

        public record ContextSection
        {
            public const string Title = "Context";
            public string[] ContextList { get; set; } = Array.Empty<string>();
        }

        public record PronunciationSection
        {
            public const string Title = "Pronunciation";
            public string[] PronunciationList { get; set; } = Array.Empty<string>();
        }
    }
}