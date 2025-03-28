using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Showcase.AppHost.OpenTelemetryCollector;

var builder = DistributedApplication.CreateBuilder(args);

var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
       .WithBindMount("../prometheus", "/etc/prometheus", isReadOnly: true)
       .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
       .WithHttpEndpoint(targetPort: 9090, name: "http");

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithBindMount("../grafana/config", "/etc/grafana", isReadOnly: true)
                     .WithBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                     .WithEnvironment("PROMETHEUS_ENDPOINT", prometheus.GetEndpoint("http"))
                     .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.AddOpenTelemetryCollector("otelcollector", "../otelcollector/config.yaml")
       .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheus.GetEndpoint("http")}/api/v1/otlp");

//var cache = builder.AddRedis("cache");

//var existingOpenAIName = builder.AddParameter("existingOpenAIName");
//var existingOpenAIResourceGroup = builder.AddParameter("existingOpenAIResourceGroup");

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
    : builder.AddConnectionString("openai");



#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var pythonPlugins = builder.AddPythonApp(
    name: "python-plugins",
    appDirectory: Path.Combine("..", "Python.Plugins"),
    scriptPath: "-m",
    virtualEnvironmentPath: "env",
    scriptArgs: ["uvicorn", "main:app"])
       .WithEndpoint(targetPort: 62394, scheme: "http", env: "UVICORN_PORT");

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    pythonPlugins.WithEnvironment("DEBUG", "True");
}

#pragma warning restore ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

//var gitHubAgent = builder.AddProject<Projects.Showcase_GitHubCopilot_Agent>("GitHubAgent")
//    .WithReference(openai)
//    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

var voiceRagAgent = builder.AddProject<Projects.Showcase_VoiceRagAgent>("VoiceRagAgent")
    .WithReference(openai)
    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true")
    .WithDaprSidecar();

//builder.AddProject<Projects.SecEdgarAgent>("secedgaragent")
//    .WithReference(openai)
//    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

//builder.AddProject<Projects.Showcase_OcelotGateway>("showcase-ocelotgateway")
//    .WithReference(gitHubAgent)
//    .WithReference(pythonPlugins);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.Showcase_GitHubCopilot_TubAgent>("showcase-githubcopilot-tubagent");


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.SecEdgarAgent>("secedgaragent")
//    .WithReference(openai)
//    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

//builder.AddProject<Projects.Showcase_OcelotGateway>("showcase-ocelotgateway")
//    .WithReference(gitHubAgent)
//    .WithReference(pythonPlugins);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.Showcase_GitHubCopilot_TubAgent>("showcase-githubcopilot-tubagent");


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

builder.AddProject<Projects.Showcase_VoiceOrchestrator>("showcase-voiceorchestrator");

//builder.AddProject<Projects.SecEdgarAgent>("secedgaragent")
//    .WithReference(openai)
//    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

//builder.AddProject<Projects.Showcase_OcelotGateway>("showcase-ocelotgateway")
//    .WithReference(gitHubAgent)
//    .WithReference(pythonPlugins);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.Showcase_GitHubCopilot_TubAgent>("showcase-githubcopilot-tubagent");


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.SecEdgarAgent>("secedgaragent")
//    .WithReference(openai)
//    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

//builder.AddProject<Projects.Showcase_OcelotGateway>("showcase-ocelotgateway")
//    .WithReference(gitHubAgent)
//    .WithReference(pythonPlugins);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

//builder.AddProject<Projects.Showcase_GitHubCopilot_TubAgent>("showcase-githubcopilot-tubagent");


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);



//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);


//var apiService = builder.AddProject<Projects.Showcase_ApiService>("ApiService")
//    .WithReference(openai)
//    .WithReference(pythonPlugins);

//builder.AddProject<Projects.Showcase_Web>("WebFrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService);

builder.Build().Run();
