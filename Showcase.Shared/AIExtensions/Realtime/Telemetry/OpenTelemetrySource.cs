using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AIExtensions.Realtime.Telemetry;
public class OpenTelemetrySource
{
    private readonly string _serverAddress;
    private readonly int _serverPort;
    private readonly string _model;

    public OpenTelemetrySource(string model, Uri endpoint)
    {
        _serverAddress = endpoint.Host;
        _serverPort = endpoint.Port;
        _model = model;
    }

    //public OpenTelemetryScope StartChatScope(ChatCompletionOptions completionsOptions)
    //{
    //    return  OpenTelemetryScope.StartConversation(_model, ChatOperationName, _serverAddress, _serverPort, completionsOptions);
    //}
}
