using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Agents.RealtimeVoice;


public class RealtimeVoiceAgentProfile
{

    #region General Metadata
    public string AgentId { get; set; } 
    public string AgentName { get; set; }
    public string AgentDescription { get; set; }
    public string AgentAvatarUrl { get; set; } = string.Empty;
    public string AgentLanguage { get; set; }
    public string AgentVoiceId { get; set; }
    #endregion

    internal class Personality
    {
        public const string Title = "Personality and Tone";
        public string Identity { get; set; }
        public string Task { get; set; }
        public string Demeanor { get; set; }
        public string Tone { get; set; }
        public string EnthusiasmLevel { get; set; }
        public string FormalityLevel { get; set; }
        public string EmotionLevel { get; set; }
        public string FillerWords { get; set; }
        public string Pacing { get; set; }
    }

    internal class Context
    {
        public const string Title = "Context";
        public string[] ContextList { get; set; }
    }

    internal class Pronunciation
    {
        public const string Title = "Pronunciation";
        public string[] PronunciationList { get; set; }
    }



}



