using System.Text.Json;
using System.Text.Json.Serialization;

namespace Showcase.Shared.AIExtensions.Realtime;

/// <summary>Source-generated JSON type information.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(Showcase.Shared.AIExtensions.Realtime.OpenAIRealtimeExtensions.ConversationFunctionToolParametersSchema))]
internal sealed partial class OpenAIJsonContext : JsonSerializerContext;