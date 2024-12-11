using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants;

namespace Showcase.Shared.AIExtensions.Realtime.Telemetry;
public class OpenTelemetryConstants
{
    public const string DefaultSourceName = "OpenAI.ConversationSession";

    public const string SecondsUnit = "s";
    public const string TokensUnit = "token";

    public static class Event
    {
        public const string Name = "event.name";
    }

    public static class Error
    {
        public const string Type = "error.type";
    }

    public static class GenAI
    {
        public const string Choice = "gen_ai.choice";
        public const string SystemName = "gen_ai.system";

        public const string RealtimeSessionConversation = "session_conversation";
        public const string Chat = "chat";
        public const string Embeddings = "embeddings";

        public static class Assistant
        {
            public const string Message = "gen_ai.assistant.message";
        }

        public static class Client
        {
            public static class OperationDuration
            {
                public const string Description = "Measures the duration of a GenAI operation";
                public const string Name = "gen_ai.client.operation.duration";
                public static readonly double[] ExplicitBucketBoundaries = [0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1.28, 2.56, 5.12, 10.24, 20.48, 40.96, 81.92];
            }

            public static class TokenUsage
            {
                public const string Description = "Measures number of input and output tokens used";
                public const string Name = "gen_ai.client.token.usage";
                public static readonly int[] ExplicitBucketBoundaries = [1, 4, 16, 64, 256, 1_024, 4_096, 16_384, 65_536, 262_144, 1_048_576, 4_194_304, 16_777_216, 67_108_864];
            }
        }


        public static class Operation
        {
            public const string Name = "gen_ai.operation.name";
        }

        public static class Request
        {
            public const string Model = "gen_ai.request.model";
            public const string MaxTokens = "gen_ai.request.max_tokens";
            public const string Temperature = "gen_ai.request.temperature";
            public const string Voice = "gen_ai.request.voice";
            public const string VoiceActivityDetection = "gen_ai.request.voice_activity_detection";
            public const string InputAudioFormat = "gen_ai.request.input_audio_format";
            public const string OutputAudioFormat = "gen_ai.request.output_audio_format";
            public const string TurnDetectionKind = "gen_ai.request.turn_detection_kind";
            //public const string TurnDetectionSilenceDuration = "gen_ai.request.turn_detection_min_length";
            //public const string TurnDetectionSilencePadding = "gen_ai.request.turn_detection_padding";
            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.request.{parameterName}";
        }
        public static class Response
        {
            public const string FinishReasons = "gen_ai.response.finish_reasons";
            public const string Id = "gen_ai.response.id";
            public const string InputTokens = "gen_ai.response.input_tokens";
            public const string OutputTokens = "gen_ai.response.output_tokens";

            public const string Model = "gen_ai.response.model";
            public static string OpenAIFingerprint = PerProvider("openai", "system_fingerprint");
            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.response.{parameterName}";
        }

        public static class ConversationRequest
        {
            public const string Model = "gen_ai.request.model";
            public const string MaxTokens = "gen_ai.request.max_tokens";
            public const string Temperature = "gen_ai.request.temperature";
            public const string Voice = "gen_ai.request.voice";
            public const string VoiceActivityDetection = "gen_ai.request.voice_activity_detection";
            public const string InputAudioFormat = "gen_ai.request.input_audio_format";
            public const string OutputAudioFormat = "gen_ai.request.output_audio_format";
            public const string TurnDetectionKind = "gen_ai.request.turn_detection_kind";
            //public const string TurnDetectionSilenceDuration = "gen_ai.request.turn_detection_min_length";
            //public const string TurnDetectionSilencePadding = "gen_ai.request.turn_detection_padding";
            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.request.{parameterName}";
        }
        public static class ConversationResponse
        {
            public const string FinishReasons = "gen_ai.response.finish_reasons";
            public const string Id = "gen_ai.response.id";
            public const string InputTokens = "gen_ai.response.input_tokens";
            public const string OutputTokens = "gen_ai.response.output_tokens";

            public const string Model = "gen_ai.response.model";
            public static string OpenAIFingerprint = PerProvider("openai", "system_fingerprint");
            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.response.{parameterName}";
        }
        //A session refers to a single WebSocket connection between a client and the server.
        public static class Session
        {
            public const string Id = "gen_ai.session.id";
            public const string Name = "gen_ai.session.name";
        }

        //A realtime Conversation consists of a list of Items.
        //By default, there is only one Conversation, and it gets created at the beginning of the Session.
        // In the future, there may be support for additional conversations.
        public static class Conversation
        {
            public const string Id = "gen_ai.conversation.id";
            public const string Name = "gen_ai.conversation";
        }



        public static class Usage
        {
            public const string InputTokens = "gen_ai.usage.input_tokens";
            public const string OutputTokens = "gen_ai.usage.output_tokens";
        }

        public static class System
        {
            public const string Message = "gen_ai.system.message";
        }

        public static class Token
        {
            public const string Type = "gen_ai.token.type";
        }

        public static class Tool
        {
            public const string Message = "gen_ai.tool.message";
        }

        public static class User
        {
            public const string Message = "gen_ai.user.message";
        }


    }

    public static class Server
    {
        public const string Address = "server.address";
        public const string Port = "server.port";

        public static class RequestDuration
        {
            public const string Description = "Generative AI server request duration such as time-to-last byte or last output token";
            public const string Name = "gen_ai.server.request.duration";
            public static readonly double[] ExplicitBucketBoundaries = [0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1.28, 2.56, 5.12, 10.24, 20.48, 40.96, 81.92];
        }

        public static class TimePerOutputToken
        {
            public const string Description = "Time per output token generated after the first token for successful responses";
            public const string Name = "gen_ai.server.time_per_output_token";
            public static readonly double[] ExplicitBucketBoundaries = [0.01, 0.025, 0.05, 0.075, 0.1, 0.15, 0.2, 0.3, 0.4, 0.5, 0.75, 1.0, 2.5];
        }

        public static class TimeToFirstToken
        {
            public const string Description = "Time to generate first token for successful responses";
            public const string Name = "gen_ai.server.time_to_first_token";
            public static readonly double[] ExplicitBucketBoundaries = [0.001, 0.005, 0.01, 0.02, 0.04, 0.06, 0.08, 0.1, 0.25, 0.5, 0.75, 1.0, 2.5, 5.0, 7.5, 10.0];
        }
    }
}
